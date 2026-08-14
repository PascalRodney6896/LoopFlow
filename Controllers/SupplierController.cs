using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Services;
using LoopFlow.Attributes;

namespace LoopFlow.Controllers
{
    [CustomAuthorize(Roles = "Admin,Supplier")]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly LoopApiService _loopApi = new LoopApiService();

        public async Task<ActionResult> Index()
        {
            var suppliers = await _db.Suppliers.Include(s => s.User).ToListAsync();
            var pendingRequests = await _db.FinancingRequests
                .Include(r => r.Order)
                .Include(r => r.Order.SupplierSplits)
                .Include(r => r.Buyer)
                .Include(r => r.Buyer.User)
                .Where(r => r.Status == "Pending")
                .ToListAsync();

            var completedSplits = await _db.SupplierSplits
                .Include(s => s.Order)
                .Where(s => s.IsPaid)
                .OrderByDescending(s => s.PaymentDate)
                .ToListAsync();

            ViewBag.PendingRequests = pendingRequests;
            ViewBag.CompletedSplits = completedSplits;
            return View(suppliers);
        }

        [HttpPost]
        public async Task<ActionResult> ApproveRequest(int requestId)
        {
            var trustChain = new TrustChainService(_db);
            var orchestrator = new SettlementOrchestrator(_db, _loopApi, trustChain);

            var success = await orchestrator.ProcessFinancingRequestApprovalAsync(requestId);
            if (success)
            {
                TempData["SuccessMessage"] = "Financing Request Approved! Loan disbursed & instant multi-supplier payout executed via LOOP Send Money API.";
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
                TempData["ErrorMessage"] = "Failed to process approval.";
            }
            return RedirectToAction("Index");
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
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
