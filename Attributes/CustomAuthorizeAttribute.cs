using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using LoopFlow.Models;

namespace LoopFlow.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public new string Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null) return false;

            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity == null) return false;

            var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier) ?? identity.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return false;
            }

            // Verify active status in DB
            using (var db = new ApplicationDbContext())
            {
                var user = db.DomainUsers.Include(u => u.UserRole).FirstOrDefault(u => u.Id == userId);
                if (user == null || !user.IsActive || user.IsLocked)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Roles))
                {
                    return true;
                }

                var allowedRoles = Roles.Split(',').Select(r => r.Trim()).ToList();
                var userRoleName = user.UserRole?.Name ?? user.Role ?? "";

                // Check role match (Support both 'Merchant' and legacy 'Buyer' terminology)
                bool isAuthorized = allowedRoles.Any(r => 
                    r.Equals(userRoleName, StringComparison.OrdinalIgnoreCase) ||
                    (r.Equals("Merchant", StringComparison.OrdinalIgnoreCase) && userRoleName.Equals("Buyer", StringComparison.OrdinalIgnoreCase)) ||
                    (r.Equals("Buyer", StringComparison.OrdinalIgnoreCase) && userRoleName.Equals("Merchant", StringComparison.OrdinalIgnoreCase))
                );

                return isAuthorized;
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" },
                        { "returnUrl", filterContext.HttpContext.Request.RawUrl }
                    });
            }
            else
            {
                // Authenticated but wrong role
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Error.cshtml",
                    MasterName = "~/Views/Shared/_Layout.cshtml",
                    ViewData = new ViewDataDictionary
                    {
                        { "HandleInfo", new HandleErrorInfo(new Exception("Access Denied. You do not have permission to access this resource."), filterContext.RouteData.Values["controller"].ToString(), filterContext.RouteData.Values["action"].ToString()) }
                    }
                };
            }
        }
    }
}
