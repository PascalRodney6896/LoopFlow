using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Services;
using LoopFlow.Attributes;

namespace LoopFlow.Controllers
{
    [CustomAuthorize(Roles = "Admin,Merchant,Buyer")]
    public class BuyerController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly LoopApiService _loopApi = new LoopApiService();

        private int GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
                var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? identity?.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                {
                    return id;
                }
            }
            return 0;
        }

        public async Task<ActionResult> Index()
        {
            int currentUserId = GetCurrentUserId();
            Buyer buyer = null;

            if (currentUserId > 0)
            {
                buyer = await _db.Buyers
                    .Include(b => b.User)
                    .Include(b => b.CreditLimit)
                    .Include(b => b.SweepConfiguration)
                    .Include(b => b.PurchaseOrders.Select(p => p.SupplierSplits))
                    .Include(b => b.LoanTransactions)
                    .FirstOrDefaultAsync(b => b.UserId == currentUserId);
            }

            if (buyer == null)
            {
                buyer = await _db.Buyers
                    .Include(b => b.User)
                    .Include(b => b.CreditLimit)
                    .Include(b => b.SweepConfiguration)
                    .Include(b => b.PurchaseOrders.Select(p => p.SupplierSplits))
                    .Include(b => b.LoanTransactions)
                    .FirstOrDefaultAsync();
            }

            if (buyer == null) return HttpNotFound();

            var wallet = await _db.LoopAccounts.FirstOrDefaultAsync(w => w.UserId == buyer.UserId);
            ViewBag.Wallet = wallet;

            return View(buyer);
        }

        [HttpGet]
        public async Task<ActionResult> CreateRequest()
        {
            int currentUserId = GetCurrentUserId();
            var buyer = (currentUserId > 0 ? await _db.Buyers.Include(b => b.CreditLimit).FirstOrDefaultAsync(b => b.UserId == currentUserId) : null)
                       ?? await _db.Buyers.Include(b => b.CreditLimit).FirstOrDefaultAsync();

            var suppliers = await _db.Suppliers.Include(s => s.User).ToListAsync();
            ViewBag.Buyer = buyer;
            ViewBag.Suppliers = suppliers;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> CreateRequest(int supplier1Id, decimal amount1, string desc1, int supplier2Id, decimal amount2, string desc2)
        {
            int currentUserId = GetCurrentUserId();
            var buyer = (currentUserId > 0 ? await _db.Buyers.Include(b => b.CreditLimit).FirstOrDefaultAsync(b => b.UserId == currentUserId) : null)
                       ?? await _db.Buyers.Include(b => b.CreditLimit).FirstOrDefaultAsync();

            if (buyer == null) return HttpNotFound();

            var supplier1 = await _db.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == supplier1Id);
            var supplier2 = await _db.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == supplier2Id);

            var totalAmount = amount1 + amount2;
            var poNumber = "ORD-2026-" + new Random().Next(1000, 9999);

            var po = new PurchaseOrder
            {
                OrderNumber = poNumber,
                BuyerId = buyer.Id,
                TotalAmount = totalAmount,
                PaymentMethod = "LOOP_BNPL",
                Status = "PendingSupplierApproval"
            };
            _db.PurchaseOrders.Add(po);
            await _db.SaveChangesAsync();

            if (supplier1 != null)
            {
                _db.SupplierSplits.Add(new SupplierSplit
                {
                    OrderId = po.Id,
                    SupplierId = supplier1.Id,
                    SupplierName = supplier1.User != null ? supplier1.User.BusinessName : supplier1.SupplierCode,
                    SupplierCode = supplier1.SupplierCode,
                    Amount = amount1,
                    ItemDescription = desc1,
                    Quantity = 1,
                    UnitPrice = amount1
                });
            }

            if (supplier2 != null)
            {
                _db.SupplierSplits.Add(new SupplierSplit
                {
                    OrderId = po.Id,
                    SupplierId = supplier2.Id,
                    SupplierName = supplier2.User != null ? supplier2.User.BusinessName : supplier2.SupplierCode,
                    SupplierCode = supplier2.SupplierCode,
                    Amount = amount2,
                    ItemDescription = desc2,
                    Quantity = 1,
                    UnitPrice = amount2
                });
            }

            var finReq = new FinancingRequest
            {
                OrderId = po.Id,
                BuyerId = buyer.Id,
                TotalAmount = totalAmount,
                CreditLimitAtRequest = buyer.CreditLimit != null ? buyer.CreditLimit.TotalCreditLimit : 500000.00m,
                Status = "Pending",
                Notes = "Buyer requested multi-supplier inventory financing."
            };
            _db.FinancingRequests.Add(finReq);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Financing Request " + poNumber + " submitted! Sent to Suppliers for approval.";
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
