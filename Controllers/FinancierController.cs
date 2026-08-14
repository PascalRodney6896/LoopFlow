using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Attributes;

namespace LoopFlow.Controllers
{
    [CustomAuthorize(Roles = "Admin,Financier")]
    public class FinancierController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public async Task<ActionResult> Index()
        {
            var buyers = await _db.Buyers
                .Include(b => b.User)
                .Include(b => b.CreditLimit)
                .Include(b => b.LoanTransactions)
                .ToListAsync();

            var loans = await _db.LoanTransactions
                .Include(l => l.Buyer)
                .Include(l => l.Buyer.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            ViewBag.Loans = loans;
            return View(buyers);
        }

        public async Task<ActionResult> TrustChain()
        {
            var records = await _db.TrustChainRecords
                .Include(t => t.Order)
                .OrderByDescending(t => t.Id)
                .ToListAsync();
            return View(records);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
