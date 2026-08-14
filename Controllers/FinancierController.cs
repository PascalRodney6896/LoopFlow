using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Services;

namespace LoopFlow.Controllers
{
    public class FinancierController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: Financier (Bank Financing Dashboard - Bank-Financed POs ONLY)
        public async Task<ActionResult> Index(string search, string status, string fundingPath, DateTime? startDate, DateTime? endDate)
        {
            var buyers = await _db.Buyers
                .Include(b => b.User)
                .Include(b => b.CreditLimit)
                .Include(b => b.LoanTransactions)
                .ToListAsync();

            var ordersQuery = _db.PurchaseOrders
                .Where(p => p.FundingPath == null || p.FundingPath == "" || p.FundingPath == "BANK_FINANCED" || p.FundingPath != "MERCHANT_FUNDED")
                .Include(p => p.Buyer.User)
                .Include(p => p.SupplierSplits)
                .Include(p => p.Invoices)
                .Include(p => p.FinancingRequest)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                ordersQuery = ordersQuery.Where(p =>
                    p.OrderNumber.Contains(search) ||
                    (p.Buyer != null && p.Buyer.User != null && p.Buyer.User.FullName.Contains(search)) ||
                    p.SupplierSplits.Any(s => s.SupplierName.Contains(search)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(p => p.Status == status || p.FinancingStatus == status || p.FulfilmentStatus == status);
            }

            if (!string.IsNullOrEmpty(fundingPath))
            {
                ordersQuery = ordersQuery.Where(p => p.FundingPath == fundingPath);
            }

            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(p => p.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(p => p.OrderDate <= endDate.Value);
            }

            var orders = await ordersQuery.OrderByDescending(p => p.Id).ToListAsync();

            var invoices = await _db.SupplierInvoices
                .Include(i => i.Order)
                .Include(i => i.Supplier.User)
                .Where(i => i.Order == null || i.Order.FundingPath == null || i.Order.FundingPath == "" || i.Order.FundingPath != "MERCHANT_FUNDED")
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            var financingRequests = await _db.FinancingRequests
                .Include(f => f.Order)
                .Include(f => f.Buyer.User)
                .Where(f => f.Order == null || f.Order.FundingPath == null || f.Order.FundingPath == "" || f.Order.FundingPath != "MERCHANT_FUNDED")
                .OrderByDescending(f => f.Id)
                .ToListAsync();

            var splits = await _db.SupplierSplits
                .Include(s => s.Order)
                .Where(s => s.Order == null || s.Order.FundingPath == null || s.Order.FundingPath == "" || s.Order.FundingPath != "MERCHANT_FUNDED")
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            var loans = await _db.LoanTransactions
                .Include(l => l.Buyer.User)
                .OrderByDescending(l => l.Id)
                .ToListAsync();

            // Bank Suspense Account & Double-Entry Ledger Orchestrator
            var suspenseOrchestrator = new BankSuspenseOrchestrator(_db);
            var suspenseAccount = await suspenseOrchestrator.GetOrCreateSuspenseAccountAsync();
            await suspenseOrchestrator.ReconcileSuspenseAccountAsync();

            var suspenseEntries = await _db.BankSuspenseLedgerEntries
                .Include(e => e.Buyer.User)
                .Include(e => e.Supplier.User)
                .Include(e => e.Order)
                .Include(e => e.Invoice)
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            // Top-Level Portfolio KPIs
            decimal totalApprovedLimit = buyers.Sum(b => b.CreditLimit?.TotalCreditLimit ?? 0m);
            decimal totalUtilised = buyers.Sum(b => b.CreditLimit?.UsedCredit ?? 0m);
            decimal totalOutstanding = totalUtilised;

            var pendingReqs = financingRequests.Where(f => f.Status == "Pending" || f.Status == "UtilisationRequested" || f.Status == "FacilityReserved" || f.Status == "SubmittedToBank").ToList();
            decimal pendingFinancingAmount = pendingReqs.Sum(f => f.TotalAmount);
            int pendingFinancingCount = pendingReqs.Count;

            var pendingPayouts = orders.Where(p => p.PaymentStatus == "PROCESSING" || (p.FulfilmentStatus == "Dispatched" && p.PaymentStatus != "PAID")).ToList();
            decimal pendingSupplierPaymentsAmount = pendingPayouts.Sum(p => p.TotalAmount);
            int pendingSupplierPaymentsCount = pendingPayouts.Count;

            var overdueLoans = loans.Where(l => l.TransactionType == "Disbursement" && l.CreatedAt < DateTime.UtcNow.AddDays(-30)).ToList();
            decimal overdueFinancingAmount = overdueLoans.Sum(l => l.Amount);
            int overdueFinancingCount = overdueLoans.Count;

            ViewBag.TotalApprovedLimit = totalApprovedLimit;
            ViewBag.TotalUtilised = totalUtilised;
            ViewBag.TotalOutstanding = totalOutstanding;
            ViewBag.PendingFinancingAmount = pendingFinancingAmount;
            ViewBag.PendingFinancingCount = pendingFinancingCount;
            ViewBag.PendingSupplierPaymentsAmount = pendingSupplierPaymentsAmount;
            ViewBag.PendingSupplierPaymentsCount = pendingSupplierPaymentsCount;
            ViewBag.OverdueFinancingAmount = overdueFinancingAmount;
            ViewBag.OverdueFinancingCount = overdueFinancingCount;

            ViewBag.SuspenseAccount = suspenseAccount;
            ViewBag.SuspenseLedgerEntries = suspenseEntries;

            var notifications = await _db.Notifications
                .OrderByDescending(n => n.SentAt)
                .Take(10)
                .ToListAsync();
            ViewBag.Notifications = notifications;

            ViewBag.FinancingTransactions = orders;
            ViewBag.Invoices = invoices;
            ViewBag.FinancingRequests = financingRequests;
            ViewBag.SupplierPayments = splits;
            ViewBag.Repayments = loans;

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FundingPath = fundingPath;

            return View(buyers);
        }

        // GET: Financier/Loans (Central View for All Financing Applications)
        public async Task<ActionResult> Loans(string search, string statusFilter)
        {
            var query = _db.FinancingRequests
                .Include(f => f.Order)
                .Include(f => f.Order.Buyer)
                .Include(f => f.Order.Buyer.User)
                .Include(f => f.Order.Buyer.CreditLimit)
                .Include(f => f.Order.SupplierSplits)
                .Include(f => f.Order.Invoices)
                .Where(f => f.Order == null || f.Order.FundingPath == null || f.Order.FundingPath == "" || f.Order.FundingPath != "MERCHANT_FUNDED");

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(f => f.Order.OrderNumber.ToLower().Contains(search)
                    || f.Order.Buyer.User.FullName.ToLower().Contains(search)
                    || f.Order.Invoices.Any(i => i.InvoiceNumber.ToLower().Contains(search)));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(f => f.Status == statusFilter || f.Order.FinancingStatus == statusFilter);
            }

            var applications = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();

            var suspenseOrchestrator = new BankSuspenseOrchestrator(_db);
            var suspenseAccount = await suspenseOrchestrator.GetOrCreateSuspenseAccountAsync();
            await suspenseOrchestrator.ReconcileSuspenseAccountAsync();

            var suspenseEntries = await _db.BankSuspenseLedgerEntries
                .Include(e => e.Buyer.User)
                .Include(e => e.Supplier.User)
                .Include(e => e.Order)
                .Include(e => e.Invoice)
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.StatusFilter = statusFilter;

            ViewBag.PendingApprovalCount = applications.Count(a => a.Status == "SubmittedToBank" || a.Status == "Pending" || a.Status == "UtilisationRequested" || a.Status == "FacilityReserved" || a.Order?.FinancingStatus == "SUBMITTED_TO_BANK" || a.Order?.FinancingStatus == "FACILITY_RESERVED");
            ViewBag.ApprovedCount = applications.Count(a => a.Status == "Approved" || a.Order?.FinancingStatus == "BANK_APPROVED");
            ViewBag.DisbursedCount = applications.Count(a => a.Status == "Disbursed" || a.Order?.FinancingStatus == "DISBURSED");
            ViewBag.RejectedCount = applications.Count(a => a.Status == "Rejected" || a.Order?.FinancingStatus == "REJECTED");

            ViewBag.SuspenseAccount = suspenseAccount;
            ViewBag.SuspenseLedgerEntries = suspenseEntries;
            ViewBag.Notifications = await _db.Notifications.OrderByDescending(n => n.SentAt).Take(10).ToListAsync();

            return View(applications);
        }

        // POST: Financier/ApproveLoanApplication
        [HttpPost]
        public async Task<ActionResult> ApproveLoanApplication(int id)
        {
            var finReq = await _db.FinancingRequests
                .Include(f => f.Order)
                .Include(f => f.Order.Buyer)
                .Include(f => f.Order.Buyer.User)
                .Include(f => f.Order.SupplierSplits)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (finReq == null) return HttpNotFound("Financing application not found.");

            finReq.Status = "Approved";
            finReq.Notes = "Bank loan approved & facility reserved for order #" + (finReq.Order?.OrderNumber ?? "");

            if (finReq.Order != null)
            {
                finReq.Order.FinancingStatus = "BANK_APPROVED";
                finReq.Order.UpdatedAt = DateTime.UtcNow;

                // Post Merchant Obligation Hold into Bank Suspense Ledger
                var suspense = new BankSuspenseOrchestrator(_db);
                await suspense.RecordMerchantObligationHoldAsync(finReq.Order.Id, "NCBA Bank Underwriter");

                // Audit Log
                _db.AuditLogs.Add(new AuditLog
                {
                    ActionType = "APPROVE_LOAN_APPLICATION",
                    EntityType = "FinancingRequest",
                    EntityId = finReq.Id,
                    ReferenceNumber = finReq.Order.OrderNumber,
                    ActorName = "NCBA Bank Underwriter",
                    ActorRole = "NCBA Bank System",
                    NewValue = "Loan application approved for PO #" + finReq.Order.OrderNumber + " (KES " + finReq.TotalAmount.ToString("N0") + ")"
                });

                // Notify Suppliers
                foreach (var split in finReq.Order.SupplierSplits)
                {
                    var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == split.SupplierId);
                    if (supplier != null)
                    {
                        _db.Notifications.Add(new Notification
                        {
                            UserId = supplier.UserId,
                            Type = "Financing Approval",
                            Title = "Bank Loan Approved",
                            Message = "NCBA Bank has approved financing for Order #" + finReq.Order.OrderNumber + ". Pending disbursement.",
                            Priority = "High",
                            Link = "/Supplier/OrderDetails/" + finReq.Order.Id,
                            IsRead = false,
                            SentAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Financing application #" + finReq.Id + " approved & funds placed in Bank Suspense Holding!";
            return RedirectToAction("Loans");
        }

        // POST: Financier/DisburseLoanApplication
        [HttpPost]
        public async Task<ActionResult> DisburseLoanApplication(int id)
        {
            var finReq = await _db.FinancingRequests
                .Include(f => f.Order)
                .Include(f => f.Order.Buyer)
                .Include(f => f.Order.Buyer.User)
                .Include(f => f.Order.Buyer.CreditLimit)
                .Include(f => f.Order.SupplierSplits)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (finReq == null) return HttpNotFound("Financing application not found.");

            finReq.Status = "Disbursed";
            finReq.Notes = "Bank loan facility funds disbursed for PO #" + (finReq.Order?.OrderNumber ?? "");

            if (finReq.Order != null)
            {
                finReq.Order.FinancingStatus = "DISBURSED";
                finReq.Order.UpdatedAt = DateTime.UtcNow;

                // Create LoanTransaction Disbursement record
                var loanTxn = new LoanTransaction
                {
                    BuyerId = finReq.BuyerId,
                    TransactionType = "Disbursement",
                    Amount = finReq.TotalAmount,
                    BalanceBefore = finReq.Order.Buyer?.CreditLimit?.UsedCredit ?? 0m,
                    BalanceAfter = (finReq.Order.Buyer?.CreditLimit?.UsedCredit ?? 0m) + finReq.TotalAmount,
                    TransactionReference = "DISB-NCBA-" + new Random().Next(10000, 99999),
                    Notes = "Loan disbursement for PO #" + finReq.Order.OrderNumber,
                    CreatedAt = DateTime.UtcNow
                };
                _db.LoanTransactions.Add(loanTxn);

                // Release Supplier Disbursement from Bank Suspense Holding
                var suspense = new BankSuspenseOrchestrator(_db);
                await suspense.ReleaseSupplierDisbursementAsync(finReq.Order.Id, "NCBA Bank Underwriter");

                // Audit Log
                _db.AuditLogs.Add(new AuditLog
                {
                    ActionType = "DISBURSE_LOAN_FUNDS",
                    EntityType = "LoanTransaction",
                    EntityId = finReq.Id,
                    ReferenceNumber = finReq.Order.OrderNumber,
                    ActorName = "NCBA Bank System",
                    ActorRole = "NCBA Bank System",
                    NewValue = "Loan disbursed for PO #" + finReq.Order.OrderNumber + " (KES " + finReq.TotalAmount.ToString("N0") + ")"
                });

                // Notify Suppliers
                foreach (var split in finReq.Order.SupplierSplits)
                {
                    var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == split.SupplierId);
                    if (supplier != null)
                    {
                        _db.Notifications.Add(new Notification
                        {
                            UserId = supplier.UserId,
                            Type = "Loan Disbursement",
                            Title = "Bank Loan Disbursed - Ready for Dispatch",
                            Message = "NCBA Bank loan funds for Order #" + finReq.Order.OrderNumber + " have been disbursed. Dispatch unlocked!",
                            Priority = "High",
                            Link = "/Supplier/OrderDetails/" + finReq.Order.Id,
                            IsRead = false,
                            SentAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Financing application #" + finReq.Id + " disbursed from Bank Suspense Ledger!";
            return RedirectToAction("Loans");
        }

        // POST: Financier/ApproveLoanOrder (Dashboard Table Action)
        [HttpPost]
        public async Task<ActionResult> ApproveLoanOrder(int orderId)
        {
            var order = await _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Include(p => p.SupplierSplits)
                .FirstOrDefaultAsync(p => p.Id == orderId);

            if (order == null) return HttpNotFound("Order not found.");

            order.FinancingStatus = "BANK_APPROVED";
            order.UpdatedAt = DateTime.UtcNow;

            var finReq = await _db.FinancingRequests.FirstOrDefaultAsync(f => f.OrderId == orderId);
            if (finReq != null)
            {
                finReq.Status = "Approved";
                finReq.Notes = "Bank underwriter approved loan application for PO #" + order.OrderNumber;
            }

            // Post Merchant Obligation Hold into Bank Suspense Ledger
            var suspense = new BankSuspenseOrchestrator(_db);
            await suspense.RecordMerchantObligationHoldAsync(order.Id, "NCBA Bank Underwriter");

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Loan application for Order #" + order.OrderNumber + " approved & placed in Bank Suspense!";
            return RedirectToAction("Index");
        }

        // POST: Financier/DisburseLoanOrder (Dashboard Table Action)
        [HttpPost]
        public async Task<ActionResult> DisburseLoanOrder(int orderId)
        {
            var order = await _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Include(p => p.Buyer.CreditLimit)
                .Include(p => p.SupplierSplits)
                .FirstOrDefaultAsync(p => p.Id == orderId);

            if (order == null) return HttpNotFound("Order not found.");

            order.FinancingStatus = "DISBURSED";
            order.UpdatedAt = DateTime.UtcNow;

            var finReq = await _db.FinancingRequests.FirstOrDefaultAsync(f => f.OrderId == orderId);
            if (finReq != null)
            {
                finReq.Status = "Disbursed";
                finReq.Notes = "Bank loan facility funds disbursed for PO #" + order.OrderNumber;
            }

            // Create LoanTransaction Disbursement record
            var loanTxn = new LoanTransaction
            {
                BuyerId = order.BuyerId,
                TransactionType = "Disbursement",
                Amount = order.TotalAmount,
                BalanceBefore = order.Buyer?.CreditLimit?.UsedCredit ?? 0m,
                BalanceAfter = (order.Buyer?.CreditLimit?.UsedCredit ?? 0m) + order.TotalAmount,
                TransactionReference = "DISB-NCBA-" + new Random().Next(10000, 99999),
                Notes = "Loan disbursement for PO #" + order.OrderNumber,
                CreatedAt = DateTime.UtcNow
            };
            _db.LoanTransactions.Add(loanTxn);

            // Release Supplier Disbursement from Bank Suspense Holding
            var suspense = new BankSuspenseOrchestrator(_db);
            await suspense.ReleaseSupplierDisbursementAsync(order.Id, "NCBA Bank System");

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Loan facility for Order #" + order.OrderNumber + " disbursed from Bank Suspense Account!";
            return RedirectToAction("Index");
        }

        // POST: Financier/ApproveAndDisburseLoan (One-click Approval & Disbursement)
        [HttpPost]
        public async Task<ActionResult> ApproveAndDisburseLoan(int orderId)
        {
            await ApproveLoanOrder(orderId);
            await DisburseLoanOrder(orderId);
            TempData["SuccessMessage"] = "Loan application for Order #" + orderId + " approved & disbursed via Bank Suspense!";
            return RedirectToAction("Index");
        }

        // POST: Financier/RejectLoanApplication
        [HttpPost]
        public async Task<ActionResult> RejectLoanApplication(int id, string rejectionReason)
        {
            var finReq = await _db.FinancingRequests
                .Include(f => f.Order)
                .Include(f => f.Order.Buyer)
                .Include(f => f.Order.Buyer.CreditLimit)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (finReq == null) return HttpNotFound("Financing application not found.");

            finReq.Status = "Rejected";
            finReq.Notes = "Rejected by Bank Underwriter: " + rejectionReason;

            if (finReq.Order != null)
            {
                finReq.Order.FinancingStatus = "REJECTED";
                finReq.Order.RejectionReason = rejectionReason;

                if (finReq.Order.Buyer?.CreditLimit != null)
                {
                    finReq.Order.Buyer.CreditLimit.UsedCredit = Math.Max(0, finReq.Order.Buyer.CreditLimit.UsedCredit - finReq.TotalAmount);
                    finReq.Order.Buyer.CreditLimit.AvailableCredit += finReq.TotalAmount;
                }
            }

            await _db.SaveChangesAsync();
            TempData["ErrorMessage"] = "Financing application #" + finReq.Id + " rejected. Reserved credit line restored.";
            return RedirectToAction("Loans");
        }

        // GET: Financier/Suspense (Dedicated Bank Suspense Account & Double-Entry Ledger View)
        public async Task<ActionResult> Suspense()
        {
            var suspenseOrchestrator = new BankSuspenseOrchestrator(_db);
            var suspenseAccount = await suspenseOrchestrator.GetOrCreateSuspenseAccountAsync();
            await suspenseOrchestrator.ReconcileSuspenseAccountAsync();

            var entries = await _db.BankSuspenseLedgerEntries
                .Include(e => e.Buyer.User)
                .Include(e => e.Supplier.User)
                .Include(e => e.Order)
                .Include(e => e.Invoice)
                .OrderByDescending(e => e.Id)
                .ToListAsync();

            ViewBag.SuspenseAccount = suspenseAccount;
            ViewBag.Notifications = await _db.Notifications.OrderByDescending(n => n.SentAt).Take(10).ToListAsync();

            return View(entries);
        }

        // GET: Financier/Reports (Bank Portfolio, Suspense, Loans & Sweeps Reports)
        public async Task<ActionResult> Reports(string reportType)
        {
            var buyers = await _db.Buyers.Include(b => b.User).Include(b => b.CreditLimit).ToListAsync();
            var orders = await _db.PurchaseOrders.Include(p => p.Buyer.User).Include(p => p.SupplierSplits).ToListAsync();
            var loans = await _db.LoanTransactions.Include(l => l.Buyer.User).OrderByDescending(l => l.CreatedAt).ToListAsync();

            var suspenseOrchestrator = new BankSuspenseOrchestrator(_db);
            var suspenseAccount = await suspenseOrchestrator.GetOrCreateSuspenseAccountAsync();
            var suspenseEntries = await _db.BankSuspenseLedgerEntries.Include(e => e.Buyer.User).Include(e => e.Supplier.User).OrderByDescending(e => e.Id).ToListAsync();

            ViewBag.ReportType = reportType;
            ViewBag.Buyers = buyers;
            ViewBag.Orders = orders;
            ViewBag.Loans = loans;
            ViewBag.SuspenseAccount = suspenseAccount;
            ViewBag.SuspenseLedgerEntries = suspenseEntries;
            ViewBag.Notifications = await _db.Notifications.OrderByDescending(n => n.SentAt).Take(10).ToListAsync();

            return View();
        }

        // GET: Financier/Buyers
        public async Task<ActionResult> Buyers()
        {
            var buyers = await _db.Buyers
                .Include(b => b.User)
                .Include(b => b.CreditLimit)
                .Include(b => b.LoanTransactions)
                .ToListAsync();

            ViewBag.Notifications = await _db.Notifications.OrderByDescending(n => n.SentAt).Take(10).ToListAsync();
            return View(buyers);
        }

        // GET: Financier/Risk
        public async Task<ActionResult> Risk()
        {
            var buyers = await _db.Buyers
                .Include(b => b.User)
                .Include(b => b.CreditLimit)
                .Include(b => b.LoanTransactions)
                .ToListAsync();

            ViewBag.Notifications = await _db.Notifications.OrderByDescending(n => n.SentAt).Take(10).ToListAsync();
            return View(buyers);
        }

        // GET: Financier/TrustChain (Bank Platform Cryptographic Audit Ledger - Bank Financed ONLY)
        public async Task<ActionResult> TrustChain(string eventFilter)
        {
            var query = _db.TrustChainRecords
                .Include(t => t.Order)
                .Include(t => t.Order.Buyer.User)
                .Where(t => t.Order == null || t.Order.FundingPath == null || t.Order.FundingPath == "" || t.Order.FundingPath != "MERCHANT_FUNDED")
                .AsQueryable();

            var bankEventTypes = new[] {
                "Settlement", "Disbursement", "FinancingApproved", "FacilityReserved",
                "DailySweepRepayment", "InvoicePostedToBank", "CreditLineApproved", "FacilitySettled"
            };

            if (!string.IsNullOrEmpty(eventFilter))
            {
                query = query.Where(t => t.EventType == eventFilter);
            }
            else
            {
                query = query.Where(t => bankEventTypes.Contains(t.EventType) || t.EventType.Contains("Bank") || t.EventType.Contains("Settlement") || t.EventType.Contains("Sweep") || t.EventType.Contains("Financing") || t.EventType.Contains("Disbursement"));
            }

            var records = await query.OrderByDescending(t => t.Id).ToListAsync();

            ViewBag.EventFilter = eventFilter;
            ViewBag.TotalBankBlocks = records.Count;
            ViewBag.DisbursementsHashedCount = records.Count(r => r.EventType == "Disbursement" || r.EventType.Contains("Disbursement") || r.EventType == "Settlement");
            ViewBag.SweepsHashedCount = records.Count(r => r.EventType == "DailySweepRepayment" || r.EventType.Contains("Sweep"));
            ViewBag.FacilityApprovedCount = records.Count(r => r.EventType == "FinancingApproved" || r.EventType == "FacilityReserved" || r.EventType.Contains("Approved"));
            ViewBag.Notifications = await _db.Notifications.OrderByDescending(n => n.SentAt).Take(10).ToListAsync();

            return View(records);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
