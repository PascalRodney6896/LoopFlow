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
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to process approval.";
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
