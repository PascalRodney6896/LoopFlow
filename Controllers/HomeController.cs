using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;

namespace LoopFlow.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            ViewBag.BuyerCount = await _db.Buyers.CountAsync();
            ViewBag.SupplierCount = await _db.Suppliers.CountAsync();
            ViewBag.TotalGMV = await _db.PurchaseOrders.SumAsync(p => (decimal?)p.TotalAmount) ?? 0;
            ViewBag.TotalLoans = await _db.LoanTransactions.Where(l => l.TransactionType == "Disbursement").SumAsync(l => (decimal?)l.Amount) ?? 0;
            ViewBag.TotalSweeps = await _db.SweepHistories.SumAsync(s => (decimal?)s.SweepAmount) ?? 0;
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}