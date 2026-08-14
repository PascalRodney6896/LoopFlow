using System;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Services;

namespace LoopFlow.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService = new AuthService();

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToLocal(returnUrl, GetUserRole());
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string ipAddress = Request.UserHostAddress;
            var result = await _authService.AuthenticateAsync(model.UsernameOrEmail, model.Password, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage);
                return View(model);
            }

            var user = result.User;

            // Sign in OWIN auth cookie
            _authService.SignInCookie(HttpContext, user, model.RememberMe);

            if (user.MustChangePassword)
            {
                TempData["Warning"] = "Your account requires a password change before continuing.";
                return RedirectToAction("ChangePassword");
            }

            string role = user.UserRole?.Name ?? user.Role ?? "Merchant";
            return RedirectToLocal(returnUrl, role);
        }

        [HttpGet]
        public ActionResult Logout()
        {
            _authService.SignOutCookie(HttpContext);
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogoutPost()
        {
            _authService.SignOutCookie(HttpContext);
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? identity?.FindFirst("UserId");
            int userId = 0;
            if (userIdClaim != null)
            {
                int.TryParse(userIdClaim.Value, out userId);
            }

            string currentIdentityName = User.Identity.Name?.Trim().ToLower();

            using (var db = new ApplicationDbContext())
            {
                var user = await db.DomainUsers.Include(u => u.UserRole).FirstOrDefaultAsync(u => 
                    (userId > 0 && u.Id == userId) || 
                    (!string.IsNullOrEmpty(currentIdentityName) && u.Username != null && u.Username.ToLower() == currentIdentityName) ||
                    (!string.IsNullOrEmpty(currentIdentityName) && u.Email != null && u.Email.ToLower() == currentIdentityName));

                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                string inputOldPass = model.OldPassword ?? "";
                bool isOldValid = PasswordHasher.VerifyPassword(inputOldPass, user.PasswordHash, user.Salt) ||
                                  PasswordHasher.VerifyPassword(inputOldPass.Trim(), user.PasswordHash, user.Salt);

                if (!isOldValid)
                {
                    ModelState.AddModelError("OldPassword", "The current password you entered is incorrect.");
                    return View(model);
                }

                string salt;
                string newHash = PasswordHasher.HashPassword(model.NewPassword, out salt);

                user.PasswordHash = newHash;
                user.Salt = salt;
                user.MustChangePassword = false;
                user.PasswordChangedDate = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                await AuthService.LogAuditAsync(db, user.Id, "Password Reset", "User", user.Id, "User updated their password upon mandatory prompt", Request.UserHostAddress);

                // Re-sign in cookie
                _authService.SignInCookie(HttpContext, user, false);

                TempData["Success"] = "Your password has been changed successfully!";

                string role = user.UserRole?.Name ?? user.Role ?? "Merchant";
                return RedirectToLocal(null, role);
            }
        }

        public ActionResult SwitchRole(string role)
        {
            var cookie = new HttpCookie("CurrentRole", role)
            {
                Expires = DateTime.Now.AddDays(7)
            };
            Response.Cookies.Add(cookie);

            switch (role)
            {
                case "Admin":
                    return RedirectToAction("Index", "Admin");
                case "Supplier":
                    return RedirectToAction("Index", "Supplier");
                case "Financier":
                    return RedirectToAction("Index", "Financier");
                default:
                    return RedirectToAction("Index", "Buyer");
            }
        }

        private ActionResult RedirectToLocal(string returnUrl, string role)
        {
            if (Url.IsLocalUrl(returnUrl) && !string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            switch (role)
            {
                case "Admin":
                    return RedirectToAction("Index", "Admin");
                case "Supplier":
                    return RedirectToAction("Index", "Supplier");
                case "Financier":
                    return RedirectToAction("Index", "Financier");
                case "Merchant":
                case "Buyer":
                default:
                    return RedirectToAction("Index", "Buyer");
            }
        }

        private string GetUserRole()
        {
            if (User.IsInRole("Admin")) return "Admin";
            if (User.IsInRole("Supplier")) return "Supplier";
            if (User.IsInRole("Financier")) return "Financier";
            return "Merchant";
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
                var properties = new Microsoft.Owin.Security.AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary["XsrfId"] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
    }
}