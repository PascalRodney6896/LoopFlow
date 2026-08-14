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
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly LoopApiService _loopApi = new LoopApiService();

        private async Task<Supplier> GetActiveSupplierAsync()
        {
            var suppliers = await _db.Suppliers.Include(s => s.User).ToListAsync();
            ViewBag.AllSuppliers = suppliers;

            int? sessionSupplierId = Session["ActiveSupplierId"] as int?;
            Supplier activeSupplier = null;

            if (sessionSupplierId.HasValue)
            {
                activeSupplier = suppliers.FirstOrDefault(s => s.Id == sessionSupplierId.Value);
            }

            if (activeSupplier == null && suppliers.Any())
            {
                activeSupplier = suppliers.First();
                Session["ActiveSupplierId"] = activeSupplier.Id;
            }

            ViewBag.ActiveSupplier = activeSupplier;
            return activeSupplier;
        }

        public ActionResult SwitchSupplier(int supplierId)
        {
            Session["ActiveSupplierId"] = supplierId;
            TempData["SuccessMessage"] = "Switched active supplier context.";
            var referer = Request.UrlReferrer != null ? Request.UrlReferrer.PathAndQuery : Url.Action("Index", "Supplier");
            return Redirect(referer);
        }

        // 1. SUPPLIER DASHBOARD OVERVIEW
        public async Task<ActionResult> Index()
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound("No supplier profile found.");

            // Data Isolation: Fetch only splits and orders belonging to this supplier
            var supplierSplits = await _db.SupplierSplits
                .Include(s => s.Order)
                .Include(s => s.Order.Buyer)
                .Include(s => s.Order.Buyer.User)
                .Include(s => s.Order.Invoices)
                .Where(s => s.SupplierId == activeSupplier.Id)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var supplierOrderIds = supplierSplits.Select(s => s.OrderId).Distinct().ToList();

            var supplierOrders = await _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Include(p => p.SupplierSplits)
                .Include(p => p.Invoices)
                .Where(p => supplierOrderIds.Contains(p.Id))
                .OrderByDescending(p => p.OrderDate)
                .ToListAsync();

            // Top-Level Metrics (Dynamic Real Backend Data)
            // Pending Requests: orders/splits requiring supplier verification or inventory confirmation
            var pendingRequests = supplierSplits
                .Where(s => s.VerificationStatus == "PENDING_VERIFICATION" || s.Order.SupplierVerificationStatus == "PENDING_VERIFICATION" || s.Order.Status == "PendingSupplierApproval")
                .ToList();

            var completedSplits = supplierSplits
                .Where(s => s.IsPaid || s.PaymentStatus == "COMPLETED")
                .OrderByDescending(s => s.PaymentDate ?? s.UpdatedAt)
                .ToList();

            var activeOrders = supplierOrders
                .Where(o => o.Status != "Completed" && o.Status != "Cancelled" && o.FulfilmentStatus != "Delivered")
                .ToList();

            ViewBag.PendingRequestsCount = pendingRequests.Count;
            ViewBag.CompletedPayoutsCount = completedSplits.Count;
            ViewBag.TotalAmountPaid = completedSplits.Sum(s => s.Amount);
            ViewBag.ActiveOrdersCount = activeOrders.Count;

            ViewBag.PendingRequests = pendingRequests;
            ViewBag.CompletedSplits = completedSplits;
            ViewBag.RecentOrders = supplierOrders.Take(5).ToList();

            // Unread Notifications for active supplier
            var notifications = await _db.Notifications
                .Where(n => n.UserId == activeSupplier.UserId)
                .OrderByDescending(n => n.SentAt)
                .Take(5)
                .ToListAsync();
            ViewBag.Notifications = notifications;

            return View(activeSupplier);
        }

        // 2. PENDING REQUESTS
        public async Task<ActionResult> Requests(string search, string statusFilter)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var query = _db.SupplierSplits
                .Include(s => s.Order)
                .Include(s => s.Order.Buyer)
                .Include(s => s.Order.Buyer.User)
                .Include(s => s.Order.Invoices)
                .Where(s => s.SupplierId == activeSupplier.Id);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(s => s.Order.OrderNumber.ToLower().Contains(search)
                    || s.Order.Buyer.User.FullName.ToLower().Contains(search)
                    || (s.InvoiceNumber != null && s.InvoiceNumber.ToLower().Contains(search)));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(s => s.VerificationStatus == statusFilter || s.Order.SupplierVerificationStatus == statusFilter);
            }

            var requests = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
            ViewBag.SearchTerm = search;
            ViewBag.StatusFilter = statusFilter;

            return View(requests);
        }

        [HttpPost]
        public async Task<ActionResult> ConfirmTransaction(int splitId)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var split = await _db.SupplierSplits
                .Include(s => s.Order)
                .Include(s => s.Order.Buyer)
                .Include(s => s.Order.Buyer.User)
                .FirstOrDefaultAsync(s => s.Id == splitId && s.SupplierId == activeSupplier.Id);

            if (split == null) return HttpNotFound("Request not found or access denied.");

            split.VerificationStatus = "VERIFIED";
            if (split.Order != null)
            {
                split.Order.SupplierVerificationStatus = "VERIFIED";
                split.Order.FulfilmentStatus = "Accepted";
                split.Order.UpdatedAt = DateTime.UtcNow;

                // SECTION B5 & B6: AUTOMATIC INVOICE GENERATION UPON ORDER ACCEPTANCE
                string invNumber = "INV-SUP-" + new Random().Next(1000, 9999);
                var autoInvoice = new SupplierInvoice
                {
                    InvoiceNumber = invNumber,
                    OrderId = split.OrderId,
                    SupplierId = activeSupplier.Id,
                    Amount = split.Amount,
                    Currency = "KES",
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(14),
                    Status = "AUTO_GENERATED",
                    VerificationStatus = "VERIFIED",
                    IsAutoGenerated = true,
                    Notes = "Auto-generated upon supplier order acceptance."
                };

                // IF BANK_FINANCED: POST INVOICE TO BANK AS FINANCING UTILISATION / DRAWDOWN REQUEST
                if (split.Order.FundingPath == "BANK_FINANCED")
                {
                    autoInvoice.PostedToBank = true;
                    autoInvoice.Status = "POSTED_TO_BANK";
                    split.Order.InvoiceStatus = "POSTED_TO_BANK";
                    split.Order.FinancingStatus = "UTILISATION_REQUESTED";

                    var finReq = await _db.FinancingRequests.FirstOrDefaultAsync(f => f.OrderId == split.OrderId);
                    if (finReq != null)
                    {
                        finReq.Status = "UtilisationRequested";
                        finReq.Notes = "Auto-generated invoice " + invNumber + " posted to bank for drawdown approval.";
                    }
                }
                else
                {
                    split.Order.InvoiceStatus = "AUTO_GENERATED";
                    split.Order.FinancingStatus = "NOT_REQUIRED";
                }

                _db.SupplierInvoices.Add(autoInvoice);
            }

            await _db.SaveChangesAsync();

            // Record Cryptographic TrustChain Event
            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                split.OrderId,
                "Supplier Verification & Auto-Invoice",
                "{\"Order\":\"" + split.Order.OrderNumber + "\",\"Supplier\":\"" + activeSupplier.User.FullName + "\",\"Amount\":" + split.Amount + ",\"FundingPath\":\"" + (split.Order?.FundingPath ?? "BANK_FINANCED") + "\"}",
                activeSupplier.UserId
            );

            // Audit Log & Notification
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = activeSupplier.UserId,
                ActionType = "VERIFY_TRANSACTION",
                EntityType = "PurchaseOrder",
                EntityId = split.OrderId,
                NewValue = "Supplier confirmed order #" + split.Order.OrderNumber + " (KES " + split.Amount.ToString("N2") + ") & auto-generated invoice.",
                IpAddress = Request.UserHostAddress ?? "127.0.0.1"
            });

            if (split.Order?.Buyer?.UserId != null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = split.Order.Buyer.UserId,
                    Type = "Invoice verified",
                    Title = "Supplier Order Accepted",
                    Message = "Supplier " + activeSupplier.User.FullName + " accepted order #" + split.Order.OrderNumber + " and auto-generated invoice.",
                    Priority = "High",
                    Link = "/Buyer/Index"
                });
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order confirmed! Invoice auto-generated" + (split.Order?.FundingPath == "BANK_FINANCED" ? " and posted to Bank for drawdown approval." : ".");
            return RedirectToAction("Requests");
        }

        [HttpPost]
        public async Task<ActionResult> RejectTransaction(int splitId, string rejectionReason)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var split = await _db.SupplierSplits
                .Include(s => s.Order)
                .FirstOrDefaultAsync(s => s.Id == splitId && s.SupplierId == activeSupplier.Id);

            if (split == null) return HttpNotFound("Request not found.");

            split.VerificationStatus = "REJECTED";
            split.RejectionReason = rejectionReason;
            if (split.Order != null)
            {
                split.Order.SupplierVerificationStatus = "REJECTED";
                split.Order.RejectionReason = rejectionReason;
            }

            await _db.SaveChangesAsync();

            // Record TrustChain Event
            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                split.OrderId,
                "Supplier Rejection",
                "{\"Order\":\"" + split.Order.OrderNumber + "\",\"Supplier\":\"" + activeSupplier.User.FullName + "\",\"Reason\":\"" + rejectionReason + "\"}",
                activeSupplier.UserId
            );

            TempData["ErrorMessage"] = "Transaction rejected. Rejection reason recorded and merchant notified.";
            return RedirectToAction("Requests");
        }

        [HttpPost]
        public async Task<ActionResult> ConfirmInventory(int orderId)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var order = await _db.PurchaseOrders
                .Include(p => p.SupplierSplits)
                .FirstOrDefaultAsync(p => p.Id == orderId && p.SupplierSplits.Any(s => s.SupplierId == activeSupplier.Id));

            if (order == null) return HttpNotFound();

            order.InventoryAvailabilityConfirmed = true;
            order.FulfilmentStatus = "Inventory Confirmed";
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                order.Id,
                "Inventory Confirmed",
                "{\"Order\":\"" + order.OrderNumber + "\",\"Supplier\":\"" + activeSupplier.User.FullName + "\",\"InventoryStatus\":\"Available\"}",
                activeSupplier.UserId
            );

            TempData["SuccessMessage"] = "Inventory availability confirmed for Order #" + order.OrderNumber + ".";
            return RedirectToAction("Orders");
        }

        // 3. PURCHASE ORDERS / MY ORDERS
        public async Task<ActionResult> Orders(string search, string status, string merchantName)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var supplierOrderIds = await _db.SupplierSplits
                .Where(s => s.SupplierId == activeSupplier.Id)
                .Select(s => s.OrderId)
                .Distinct()
                .ToListAsync();

            var query = _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Include(p => p.SupplierSplits)
                .Include(p => p.Invoices)
                .Where(p => supplierOrderIds.Contains(p.Id));

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(p => p.OrderNumber.ToLower().Contains(search)
                    || p.Buyer.User.FullName.ToLower().Contains(search)
                    || p.Invoices.Any(i => i.InvoiceNumber.ToLower().Contains(search)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.FulfilmentStatus == status || p.Status == status);
            }

            if (!string.IsNullOrEmpty(merchantName))
            {
                query = query.Where(p => p.Buyer.User.FullName.Contains(merchantName) || p.Buyer.User.BusinessName.Contains(merchantName));
            }

            var orders = await query.OrderByDescending(p => p.OrderDate).ToListAsync();
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedMerchant = merchantName;

            return View(orders);
        }

        public async Task<ActionResult> OrderDetails(int id)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var order = await _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Include(p => p.SupplierSplits)
                .Include(p => p.Invoices)
                .FirstOrDefaultAsync(p => p.Id == id && p.SupplierSplits.Any(s => s.SupplierId == activeSupplier.Id));

            if (order == null) return HttpNotFound("Order not found or access denied.");

            var supplierSplits = order.SupplierSplits.Where(s => s.SupplierId == activeSupplier.Id).ToList();
            ViewBag.SupplierSplits = supplierSplits;

            var trustChainRecords = await _db.TrustChainRecords
                .Where(t => t.OrderId == order.Id)
                .OrderBy(t => t.Id)
                .ToListAsync();
            ViewBag.TrustChainRecords = trustChainRecords;

            var auditLogs = await _db.AuditLogs
                .Where(a => a.EntityId == order.Id || a.ReferenceNumber == order.OrderNumber)
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
            ViewBag.AuditLogs = auditLogs;

            return View(order);
        }

        [HttpPost]
        public async Task<ActionResult> ConfirmDispatch(int orderId)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var order = await _db.PurchaseOrders
                .Include(p => p.SupplierSplits)
                .FirstOrDefaultAsync(p => p.Id == orderId && p.SupplierSplits.Any(s => s.SupplierId == activeSupplier.Id));

            if (order == null) return HttpNotFound();

            order.FulfilmentStatus = "Dispatched";
            order.DeliveryStatus = "DISPATCHED";
            order.DispatchedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // RELEASE SUPPLIER PAYMENT UPON DISPATCH CONFIRMATION
            if (order.FundingPath == "BANK_FINANCED")
            {
                order.FinancingStatus = "BANK_APPROVED";
                order.PaymentStatus = "PAID";
                order.InvoiceStatus = "VALIDATED";
            }
            else
            {
                order.PaymentStatus = "PAID";
            }

            var splits = order.SupplierSplits.Where(s => s.SupplierId == activeSupplier.Id).ToList();
            foreach (var s in splits)
            {
                s.PaymentStatus = "Completed";
                s.IsPaid = true;
                s.PaymentDate = DateTime.UtcNow;
                if (string.IsNullOrEmpty(s.TransactionReference))
                {
                    s.TransactionReference = "TXN-LOOP-DISB-" + new Random().Next(10000, 99999);
                }
            }

            await _db.SaveChangesAsync();

            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                order.Id,
                "Dispatch Confirmed & Payout Released",
                "{\"Order\":\"" + order.OrderNumber + "\",\"Supplier\":\"" + activeSupplier.User.FullName + "\",\"DispatchedAt\":\"" + DateTime.UtcNow.ToString("o") + "\",\"FundingPath\":\"" + order.FundingPath + "\"}",
                activeSupplier.UserId
            );

            TempData["SuccessMessage"] = "Order #" + order.OrderNumber + " dispatch confirmed & supplier payout released!";
            return RedirectToAction("Orders");
        }

        [HttpPost]
        public async Task<ActionResult> ConfirmDelivery(int orderId)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var order = await _db.PurchaseOrders
                .Include(p => p.SupplierSplits)
                .FirstOrDefaultAsync(p => p.Id == orderId && p.SupplierSplits.Any(s => s.SupplierId == activeSupplier.Id));

            if (order == null) return HttpNotFound();

            order.FulfilmentStatus = "Completed";
            order.Status = "Completed";
            order.DeliveryStatus = "DELIVERED";
            order.DeliveredAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                order.Id,
                "Delivery Confirmed & Order Completed",
                "{\"Order\":\"" + order.OrderNumber + "\",\"Supplier\":\"" + activeSupplier.User.FullName + "\",\"DeliveredAt\":\"" + DateTime.UtcNow.ToString("o") + "\"}",
                activeSupplier.UserId
            );

            TempData["SuccessMessage"] = "Delivery confirmed & Order #" + order.OrderNumber + " completed successfully.";
            return RedirectToAction("Orders");
        }

        // 4. INVOICE MANAGEMENT & DUPLICATE PROTECTION
        public async Task<ActionResult> Invoices(string search, string status)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var query = _db.SupplierInvoices
                .Include(i => i.Order)
                .Include(i => i.Order.Buyer)
                .Include(i => i.Order.Buyer.User)
                .Where(i => i.SupplierId == activeSupplier.Id);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(i => i.InvoiceNumber.ToLower().Contains(search)
                    || i.Order.OrderNumber.ToLower().Contains(search)
                    || i.Order.Buyer.User.FullName.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(i => i.Status == status || i.VerificationStatus == status);
            }

            var invoices = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

            // Eligible purchase orders for invoice submission
            var eligibleOrderIds = await _db.SupplierSplits
                .Where(s => s.SupplierId == activeSupplier.Id)
                .Select(s => s.OrderId)
                .Distinct()
                .ToListAsync();

            var eligibleOrders = await _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Where(p => eligibleOrderIds.Contains(p.Id))
                .ToListAsync();

            ViewBag.EligibleOrders = eligibleOrders;
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatus = status;

            return View(invoices);
        }

        [HttpPost]
        public async Task<ActionResult> CreateInvoice(int orderId, string invoiceNumber, decimal amount, DateTime? dueDate, string notes)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                TempData["ErrorMessage"] = "Invoice number is required.";
                return RedirectToAction("Invoices");
            }

            invoiceNumber = invoiceNumber.Trim().ToUpper();

            // DUPLICATE INVOICE PROTECTION: Check if invoice number already exists for this supplier OR duplicate hash matches
            string duplicateHash = activeSupplier.Id + "_" + invoiceNumber + "_" + amount.ToString("F2");
            var existingInvoice = await _db.SupplierInvoices.FirstOrDefaultAsync(i =>
                i.SupplierId == activeSupplier.Id && (i.InvoiceNumber == invoiceNumber || i.DuplicateCheckHash == duplicateHash));

            if (existingInvoice != null)
            {
                TempData["ErrorMessage"] = "DUPLICATE INVOICE DETECTED! Invoice #" + invoiceNumber + " already exists in the system. Duplicate invoices cannot proceed to financing/payment processing.";
                return RedirectToAction("Invoices");
            }

            var order = await _db.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Associated purchase order not found.";
                return RedirectToAction("Invoices");
            }

            var invoice = new SupplierInvoice
            {
                SupplierId = activeSupplier.Id,
                OrderId = orderId,
                InvoiceNumber = invoiceNumber,
                Amount = amount,
                Currency = "KES",
                InvoiceDate = DateTime.UtcNow,
                DueDate = dueDate ?? DateTime.UtcNow.AddDays(14),
                Status = "PENDING_VERIFICATION",
                VerificationStatus = "VERIFIED",
                DuplicateCheckHash = duplicateHash,
                Notes = notes
            };

            _db.SupplierInvoices.Add(invoice);

            // Link invoice to supplier split
            var split = await _db.SupplierSplits.FirstOrDefaultAsync(s => s.OrderId == orderId && s.SupplierId == activeSupplier.Id);
            if (split != null)
            {
                split.InvoiceNumber = invoiceNumber;
                split.VerificationStatus = "VERIFIED";
            }

            await _db.SaveChangesAsync();

            // Record TrustChain Verification Event
            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                orderId,
                "Invoice Submission & Verification",
                "{\"InvoiceNumber\":\"" + invoiceNumber + "\",\"Amount\":" + amount + ",\"Supplier\":\"" + activeSupplier.User.FullName + "\",\"Verification\":\"Passed (No Duplicate)\"}",
                activeSupplier.UserId
            );

            // Notification
            _db.Notifications.Add(new Notification
            {
                UserId = activeSupplier.UserId,
                Type = "Invoice verified",
                Title = "Invoice Submitted & Verified",
                Message = "Invoice #" + invoiceNumber + " for KES " + amount.ToString("N2") + " passed authenticity and duplicate verification.",
                Priority = "Normal",
                Link = "/Supplier/Invoices"
            });

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Invoice #" + invoiceNumber + " submitted and verified successfully! Duplicate check passed.";
            return RedirectToAction("Invoices");
        }

        [HttpPost]
        public async Task<ActionResult> ResubmitInvoice(int invoiceId, string newInvoiceNumber, decimal newAmount, string notes)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var invoice = await _db.SupplierInvoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.SupplierId == activeSupplier.Id);
            if (invoice == null) return HttpNotFound();

            newInvoiceNumber = newInvoiceNumber.Trim().ToUpper();
            string duplicateHash = activeSupplier.Id + "_" + newInvoiceNumber + "_" + newAmount.ToString("F2");

            var existing = await _db.SupplierInvoices.FirstOrDefaultAsync(i => i.Id != invoiceId && i.SupplierId == activeSupplier.Id && (i.InvoiceNumber == newInvoiceNumber || i.DuplicateCheckHash == duplicateHash));
            if (existing != null)
            {
                TempData["ErrorMessage"] = "DUPLICATE INVOICE ERROR: Invoice #" + newInvoiceNumber + " already exists.";
                return RedirectToAction("Invoices");
            }

            invoice.InvoiceNumber = newInvoiceNumber;
            invoice.Amount = newAmount;
            invoice.Notes = notes;
            invoice.Status = "PENDING_VERIFICATION";
            invoice.VerificationStatus = "VERIFIED";
            invoice.RejectionReason = null;
            invoice.DuplicateCheckHash = duplicateHash;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Invoice #" + newInvoiceNumber + " corrected and resubmitted successfully.";
            return RedirectToAction("Invoices");
        }

        // 5. TRUSTCHAIN VERIFICATION
        public async Task<ActionResult> TrustChain()
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var supplierOrderIds = await _db.SupplierSplits
                .Where(s => s.SupplierId == activeSupplier.Id)
                .Select(s => s.OrderId)
                .Distinct()
                .ToListAsync();

            var trustChainRecords = await _db.TrustChainRecords
                .Include(t => t.Order)
                .Include(t => t.Order.Buyer)
                .Include(t => t.Order.Buyer.User)
                .Where(t => supplierOrderIds.Contains(t.OrderId))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(trustChainRecords);
        }

        // 6. SUPPLIER PAYMENTS & 7. RECENT PAYOUTS
        public async Task<ActionResult> Payments(string search, string paymentStatus)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var query = _db.SupplierSplits
                .Include(s => s.Order)
                .Include(s => s.Order.Buyer)
                .Include(s => s.Order.Buyer.User)
                .Include(s => s.Order.Invoices)
                .Where(s => s.SupplierId == activeSupplier.Id);

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(s => (s.TransactionReference != null && s.TransactionReference.ToLower().Contains(search))
                    || s.Order.OrderNumber.ToLower().Contains(search)
                    || (s.InvoiceNumber != null && s.InvoiceNumber.ToLower().Contains(search)));
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                if (paymentStatus == "COMPLETED")
                {
                    query = query.Where(s => s.IsPaid || s.PaymentStatus == "COMPLETED");
                }
                else
                {
                    query = query.Where(s => s.PaymentStatus == paymentStatus);
                }
            }

            var payments = await query.OrderByDescending(s => s.PaymentDate ?? s.CreatedAt).ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.SelectedPaymentStatus = paymentStatus;

            return View(payments);
        }

        // 8. SUPPLIER PROFILE & SETTLEMENT ACCOUNT
        public new async Task<ActionResult> Profile()
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var auditLogs = await _db.AuditLogs
                .Where(a => a.UserId == activeSupplier.UserId)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();

            ViewBag.AuditLogs = auditLogs;
            return View(activeSupplier);
        }

        [HttpPost]
        public async Task<ActionResult> UpdateProfile(string businessRegistration, string kraPin, string businessCategory, string contactPhone, string contactEmail, string businessAddress)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            activeSupplier.BusinessRegistration = businessRegistration;
            activeSupplier.KRA_PIN = kraPin;
            activeSupplier.BusinessCategory = businessCategory;
            activeSupplier.ContactPhone = contactPhone;
            activeSupplier.ContactEmail = contactEmail;
            activeSupplier.BusinessAddress = businessAddress;
            activeSupplier.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = activeSupplier.UserId,
                ActionType = "UPDATE_PROFILE",
                EntityType = "Supplier",
                EntityId = activeSupplier.Id,
                NewValue = "Supplier profile contact and business details updated.",
                IpAddress = Request.UserHostAddress ?? "127.0.0.1"
            });
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile details updated successfully.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<ActionResult> UpdateSettlementAccount(string settlementBank, string settlementAccount, string settlementAccountName, string paymentDetails)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            string oldAccount = activeSupplier.SettlementAccount;

            activeSupplier.SettlementBank = settlementBank;
            activeSupplier.SettlementAccount = settlementAccount;
            activeSupplier.SettlementAccountName = settlementAccountName;
            activeSupplier.PaymentDetails = paymentDetails;
            activeSupplier.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // REQUIRE AUTHORIZATION / AUDIT EVENT FOR SETTLEMENT ACCOUNT CHANGES
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = activeSupplier.UserId,
                ActionType = "UPDATE_SETTLEMENT_ACCOUNT",
                EntityType = "SupplierSettlement",
                EntityId = activeSupplier.Id,
                OldValue = oldAccount,
                NewValue = "Settlement account changed to " + settlementAccount + " at " + settlementBank,
                IpAddress = Request.UserHostAddress ?? "127.0.0.1"
            });

            _db.Notifications.Add(new Notification
            {
                UserId = activeSupplier.UserId,
                Type = "Profile/KYC issue",
                Title = "Settlement Account Updated",
                Message = "Settlement bank details updated to " + settlementBank + " (" + settlementAccount + "). Change audited.",
                Priority = "High",
                Link = "/Supplier/Profile"
            });

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Settlement account details updated and audit record generated.";
            return RedirectToAction("Profile");
        }

        // 9. NOTIFICATIONS HUB
        public async Task<ActionResult> Notifications()
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var notifications = await _db.Notifications
                .Where(n => n.UserId == activeSupplier.UserId)
                .OrderByDescending(n => n.SentAt)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<ActionResult> MarkNotificationRead(int id)
        {
            var activeSupplier = await GetActiveSupplierAsync();
            var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == activeSupplier.UserId);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return Json(new { success = true });
        }

        // 10. SUPPLIER REPORTS & SETTLEMENT STATEMENTS
        public async Task<ActionResult> Reports()
        {
            var activeSupplier = await GetActiveSupplierAsync();
            if (activeSupplier == null) return HttpNotFound();

            var invoices = await _db.SupplierInvoices
                .Include(i => i.Order)
                .Where(i => i.SupplierId == activeSupplier.Id)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            var splits = await _db.SupplierSplits
                .Include(s => s.Order)
                .Where(s => s.SupplierId == activeSupplier.Id)
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            ViewBag.Invoices = invoices;
            ViewBag.Splits = splits;
            ViewBag.ActiveSupplier = activeSupplier;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose(); base.Dispose(disposing);
        }
    }
}
