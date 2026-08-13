using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Services;

namespace LoopFlow.Controllers
{
    public class LoopWebhookController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly LoopApiService _loopApi = new LoopApiService();

        [HttpPost]
        public async Task<JsonResult> Ipn(int buyerId, decimal saleAmount, string transactionReference)
        {
            if (buyerId <= 0 || saleAmount <= 0)
            {
                return Json(new { status = "FAILED", message = "Invalid IPN payload" }, JsonRequestBehavior.AllowGet);
            }

            var sweepEngine = new SweepEngine(_db, _loopApi);
            var sweepResult = await sweepEngine.ProcessIncomingSalesCollectionAsync(buyerId, saleAmount);

            return Json(new
            {
                status = "SUCCESS",
                reference = transactionReference,
                sweepExecuted = sweepResult != null,
                sweptAmount = sweepResult != null ? sweepResult.SweepAmount : 0
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
