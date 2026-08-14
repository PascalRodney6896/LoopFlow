using System;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;

namespace LoopFlow.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult SwitchRole(string role)
        {
            var cookie = new HttpCookie("CurrentRole", role)
            {
                Expires = DateTime.Now.AddDays(7)
            };
            Response.Cookies.Add(cookie);

            switch (role)
            {
                case "Supplier":
                    return RedirectToAction("Index", "Supplier");
                case "Financier":
                    return RedirectToAction("Index", "Financier");
                default:
                    return RedirectToAction("Index", "Buyer");
            }
        }

        // Helper class for OWIN external login challenges
        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary["XsrfId"] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
    }
}