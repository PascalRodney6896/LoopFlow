using System.Threading.Tasks;
using System.Web.Mvc;

namespace LoopFlow.Controllers
{
    public class MerchantController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Buyer");
        }

        public ActionResult CreateRequest()
        {
            return RedirectToAction("CreateRequest", "Buyer");
        }

        public ActionResult Reports()
        {
            return RedirectToAction("Reports", "Buyer");
        }
    }
}
