using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Services;

namespace LoopFlow.Controllers
{
    public class BuyerController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly LoopApiService _loopApi = new LoopApiService();

        public async Task<ActionResult> Index()
        {
            var buyer = await _db.Buyers
                .Include(b => b.User)
                .Include(b => b.CreditLimit)
                .Include(b => b.SweepConfiguration)
                .Include(b => b.PurchaseOrders.Select(p => p.SupplierSplits))
                .Include(b => b.LoanTransactions)
                .FirstOrDefaultAsync();

            if (buyer == null) return HttpNotFound();

            var wallet = await _db.LoopAccounts.FirstOrDefaultAsync(w => w.UserId == buyer.UserId);
            var cashForecasts = await _db.CashFlowForecasts.Where(c => c.BuyerId == buyer.Id).ToListAsync();
            var investmentRecs = await _db.InvestmentRecommendations.Where(i => i.BuyerId == buyer.Id).ToListAsync();
            var loanTransactions = await _db.LoanTransactions.Where(l => l.BuyerId == buyer.Id).OrderByDescending(l => l.CreatedAt).ToListAsync();

            ViewBag.Wallet = wallet;
            ViewBag.CashForecasts = cashForecasts;
            ViewBag.InvestmentRecs = investmentRecs;
            ViewBag.LoanTransactions = loanTransactions;

            return View(buyer);
        }

        [HttpGet]
        public async Task<ActionResult> CreateRequest()
        {
            var buyer = await _db.Buyers
                .Include(b => b.User)
                .Include(b => b.CreditLimit)
                .FirstOrDefaultAsync();

            int targetUserId = buyer != null ? buyer.UserId : 0;
            var suppliers = await _db.Suppliers.Include(s => s.User).ToListAsync();
            var wallet = await _db.LoopAccounts.FirstOrDefaultAsync(w => w.UserId == targetUserId);

            ViewBag.Buyer = buyer;
            ViewBag.Suppliers = suppliers;
            ViewBag.Wallet = wallet;
            return View(buyer);
        }

        [HttpPost]
        public async Task<ActionResult> CreateRequest(
            FormCollection form = null,
            int? supplier1Id = null,
            decimal? amount1 = null,
            string desc1 = null,
            int? supplier2Id = null,
            decimal? amount2 = null,
            string desc2 = null,
            int[] supplierIds = null,
            decimal[] amounts = null,
            string[] descriptions = null,
            int[] quantities = null,
            decimal[] unitPrices = null,
            DateTime? requiredDeliveryDate = null)
        {
            var buyer = await _db.Buyers.Include(b => b.User).Include(b => b.CreditLimit).FirstOrDefaultAsync();
            if (buyer == null) return HttpNotFound();

            // Parse supplierIds & line item fields dynamically
            var rawSupplierIds = form.GetValues("supplierIds");
            var rawAmounts = form.GetValues("amounts");
            var rawDescriptions = form.GetValues("descriptions");
            var rawQuantities = form.GetValues("quantities");
            var rawUnitPrices = form.GetValues("unitPrices");
            var rawDeliveryDate = form["requiredDeliveryDate"];

            var parsedSupplierIds = new System.Collections.Generic.List<int>();
            var parsedAmounts = new System.Collections.Generic.List<decimal>();
            var parsedDescriptions = new System.Collections.Generic.List<string>();
            var parsedQuantities = new System.Collections.Generic.List<int>();
            var parsedUnitPrices = new System.Collections.Generic.List<decimal>();

            if (rawSupplierIds != null && rawSupplierIds.Length > 0)
            {
                for (int i = 0; i < rawSupplierIds.Length; i++)
                {
                    if (int.TryParse(rawSupplierIds[i], out int sid) && sid > 0)
                    {
                        parsedSupplierIds.Add(sid);

                        decimal amt = (rawAmounts != null && rawAmounts.Length > i && decimal.TryParse(rawAmounts[i], out decimal a)) ? a : 0;
                        int qty = (rawQuantities != null && rawQuantities.Length > i && int.TryParse(rawQuantities[i], out int q)) ? q : 1;
                        decimal price = (rawUnitPrices != null && rawUnitPrices.Length > i && decimal.TryParse(rawUnitPrices[i], out decimal p)) ? p : amt;
                        string desc = (rawDescriptions != null && rawDescriptions.Length > i) ? rawDescriptions[i] : "General Goods";

                        if (amt <= 0 && qty > 0 && price > 0) amt = qty * price;

                        parsedAmounts.Add(amt);
                        parsedQuantities.Add(qty);
                        parsedUnitPrices.Add(price);
                        parsedDescriptions.Add(desc);
                    }
                }
            }
            else
            {
                // Fallback for legacy parameter names (supplier1Id / supplier2Id)
                if (int.TryParse(form["supplier1Id"], out int s1Id) && s1Id > 0)
                {
                    parsedSupplierIds.Add(s1Id);
                    decimal.TryParse(form["amount1"], out decimal a1);
                    parsedAmounts.Add(a1 > 0 ? a1 : 300000);
                    parsedDescriptions.Add(form["desc1"] ?? "Hybrid Maize Seed");
                    parsedQuantities.Add(1000);
                    parsedUnitPrices.Add(a1 > 0 ? a1 / 1000 : 300);
                }

                if (int.TryParse(form["supplier2Id"], out int s2Id) && s2Id > 0)
                {
                    parsedSupplierIds.Add(s2Id);
                    decimal.TryParse(form["amount2"], out decimal a2);
                    parsedAmounts.Add(a2 > 0 ? a2 : 200000);
                    parsedDescriptions.Add(form["desc2"] ?? "CAN Fertilizer");
                    parsedQuantities.Add(500);
                    parsedUnitPrices.Add(a2 > 0 ? a2 / 500 : 400);
                }
            }

            if (!parsedSupplierIds.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one supplier for your purchase order.";
                return RedirectToAction("CreateRequest");
            }

            DateTime deliveryDate = DateTime.UtcNow.AddDays(7);
            if (!string.IsNullOrEmpty(rawDeliveryDate) && DateTime.TryParse(rawDeliveryDate, out DateTime dt))
            {
                deliveryDate = dt;
            }

            decimal totalAmount = parsedAmounts.Sum();
            string paymentPath = form["paymentPath"];
            if (string.IsNullOrEmpty(paymentPath)) paymentPath = "BANK_FINANCED";

            var poNumber = "ORD-2026-" + new Random().Next(1000, 9999);
            string financingStatus = "FACILITY_RESERVED";
            string paymentStatus = "UNPAID";

            if (paymentPath == "BANK_FINANCED")
            {
                // Validate & Reserve Pre-approved Facility Limit
                if (buyer.CreditLimit != null)
                {
                    if (buyer.CreditLimit.AvailableCredit < totalAmount)
                    {
                        TempData["ErrorMessage"] = "Order amount (KES " + totalAmount.ToString("N0") + ") exceeds available credit line facility (KES " + buyer.CreditLimit.AvailableCredit.ToString("N0") + ").";
                        return RedirectToAction("CreateRequest");
                    }
                    buyer.CreditLimit.UsedCredit += totalAmount;
                    buyer.CreditLimit.AvailableCredit -= totalAmount;
                }
                financingStatus = "FACILITY_RESERVED";
                paymentStatus = "UNPAID";
            }
            else if (paymentPath == "MERCHANT_FUNDED")
            {
                // Validate & Deduct from Merchant Own Funds (Wallet/Account)
                var wallet = await _db.LoopAccounts.FirstOrDefaultAsync(w => w.UserId == buyer.UserId);
                if (wallet != null && wallet.WalletBalance >= totalAmount)
                {
                    wallet.WalletBalance -= totalAmount;
                }
                else if (wallet != null && wallet.AccountBalance >= totalAmount)
                {
                    wallet.AccountBalance -= totalAmount;
                }
                financingStatus = "NOT_REQUIRED";
                paymentStatus = "PAID";
            }

            var po = new PurchaseOrder
            {
                OrderNumber = poNumber,
                BuyerId = buyer.Id,
                TotalAmount = totalAmount,
                PaymentMethod = paymentPath,
                FundingPath = paymentPath,
                FinancingStatus = financingStatus,
                PaymentStatus = paymentStatus,
                InvoiceStatus = "PENDING_GENERATION",
                DeliveryStatus = "PENDING",
                Status = "PendingSupplierApproval",
                SupplierVerificationStatus = "Pending",
                InventoryAvailabilityConfirmed = false,
                FulfilmentStatus = "Order Received",
                RequiredDeliveryDate = deliveryDate,
                OrderDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _db.PurchaseOrders.Add(po);
            await _db.SaveChangesAsync();

            var merchantName = buyer.User != null ? buyer.User.FullName : "Merchant";

            for (int i = 0; i < parsedSupplierIds.Count; i++)
            {
                int sId = parsedSupplierIds[i];
                var supplier = await _db.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == sId);
                if (supplier == null) continue;

                decimal itemAmt = parsedAmounts[i];
                string itemDesc = parsedDescriptions[i];
                int itemQty = parsedQuantities[i];
                decimal itemPrice = parsedUnitPrices[i];

                _db.SupplierSplits.Add(new SupplierSplit
                {
                    OrderId = po.Id,
                    SupplierId = supplier.Id,
                    SupplierName = supplier.User != null ? supplier.User.BusinessName : supplier.SupplierCode,
                    SupplierCode = supplier.SupplierCode,
                    Amount = itemAmt,
                    ItemDescription = itemDesc,
                    Quantity = itemQty,
                    UnitPrice = itemPrice,
                    VerificationStatus = "Pending",
                    PaymentStatus = paymentStatus,
                    CreatedAt = DateTime.UtcNow
                });

                // NOTIFY SUPPLIER - Order populates directly in Supplier Pending Requests Queue!
                _db.Notifications.Add(new Notification
                {
                    UserId = supplier.UserId,
                    Type = "Purchase Order Verification",
                    Title = "New Pending Order #" + poNumber,
                    Message = "Merchant " + merchantName + " has placed a " + paymentPath.Replace("_", " ") + " order for '" + itemDesc + "' (KES " + itemAmt.ToString("N0") + "). Verification required.",
                    Priority = "High",
                    Link = "/Supplier/Requests",
                    IsRead = false,
                    SentAt = DateTime.UtcNow
                });
            }

            if (paymentPath == "BANK_FINANCED")
            {
                var finReq = new FinancingRequest
                {
                    OrderId = po.Id,
                    BuyerId = buyer.Id,
                    TotalAmount = totalAmount,
                    CreditLimitAtRequest = buyer.CreditLimit != null ? buyer.CreditLimit.TotalCreditLimit : 500000.00m,
                    Status = "FacilityReserved",
                    Notes = "Merchant pre-scored facility reserved for PO #" + poNumber + ". Awaiting supplier order acceptance.",
                    CreatedAt = DateTime.UtcNow
                };
                _db.FinancingRequests.Add(finReq);
                await _db.SaveChangesAsync();
            }

            // RECORD TRUSTCHAIN CRYPTOGRAPHIC AUDIT EVENT
            try
            {
                var trustChainService = new TrustChainService(_db);
                await trustChainService.RecordEventAsync(po.Id, "ORDER_CREATED", "Merchant " + merchantName + " placed " + paymentPath + " purchase order #" + poNumber + " for KES " + totalAmount.ToString("N0") + ".", buyer.UserId);
            }
            catch { }

            TempData["SuccessMessage"] = "Purchase Order " + poNumber + " (" + paymentPath.Replace("_", " ") + ") submitted successfully! Sent to supplier(s) for verification.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> SimulateDailySale(decimal saleAmount)
        {
            var buyer = await _db.Buyers.FirstOrDefaultAsync();
            if (buyer == null) return HttpNotFound();

            var sweepEngine = new SweepEngine(_db, _loopApi);
            var result = await sweepEngine.ProcessIncomingSalesCollectionAsync(buyer.Id, saleAmount);

            if (result != null)
            {
                TempData["SuccessMessage"] = "Daily Sale of KES " + saleAmount.ToString("N2") + " processed! Automated 30% Sweep executed: KES " + result.SweepAmount.ToString("N2") + " sent to repay loan via LOOP Send Money API.";
            }
            else
            {
                TempData["InfoMessage"] = "Daily Sale of KES " + saleAmount.ToString("N2") + " processed! No active loan balance requires repayment.";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
