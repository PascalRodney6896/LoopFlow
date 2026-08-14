using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public class AuthService
    {
        private const int MaxFailedLoginAttempts = 5;

        public async Task<(bool Success, string ErrorMessage, User User)> AuthenticateAsync(string usernameOrEmail, string password, string ipAddress)
        {
            using (var db = new ApplicationDbContext())
            {
                if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Username/Email and Password are required.", null);
                }

                var normalizedInput = usernameOrEmail.Trim().ToLower();

                var user = await db.DomainUsers
                    .Include(u => u.UserRole)
                    .FirstOrDefaultAsync(u => 
                        (u.Username != null && u.Username.ToLower() == normalizedInput) || 
                        (u.Email != null && u.Email.ToLower() == normalizedInput));

                if (user == null)
                {
                    await LogAuditAsync(db, null, "Failed Login", "User", null, $"Login attempt failed for non-existent target: {usernameOrEmail}", ipAddress, false, "Invalid credentials");
                    return (false, "Invalid username/email or password.", null);
                }

                if (!user.IsActive)
                {
                    await LogAuditAsync(db, user.Id, "Failed Login", "User", user.Id, "Attempted login to deactivated account", ipAddress, false, "Deactivated account");
                    return (false, "Your account has been deactivated. Please contact an administrator.", null);
                }

                if (user.IsLocked)
                {
                    await LogAuditAsync(db, user.Id, "Failed Login", "User", user.Id, "Attempted login to locked account", ipAddress, false, "Locked account");
                    return (false, "Your account is locked due to repeated failed login attempts. Please contact an administrator.", null);
                }

                bool isPasswordValid = PasswordHasher.VerifyPassword(password, user.PasswordHash, user.Salt);

                if (!isPasswordValid)
                {
                    user.FailedLoginAttempts += 1;
                    user.UpdatedAt = DateTime.UtcNow;

                    string errorReason = "Invalid password";
                    if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
                    {
                        user.IsLocked = true;
                        errorReason = "Account locked due to 5 consecutive failed attempts";
                        await LogAuditAsync(db, user.Id, "User Locked", "User", user.Id, $"Account locked after {user.FailedLoginAttempts} failed attempts.", ipAddress, true);
                    }

                    await LogAuditAsync(db, user.Id, "Failed Login", "User", user.Id, $"Failed password attempt ({user.FailedLoginAttempts}/{MaxFailedLoginAttempts})", ipAddress, false, errorReason);
                    await db.SaveChangesAsync();

                    return (false, "Invalid username/email or password.", null);
                }

                // Successful login
                user.FailedLoginAttempts = 0;
                user.LastLoginDate = DateTime.UtcNow;
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await LogAuditAsync(db, user.Id, "Successful Login", "User", user.Id, "User authenticated successfully", ipAddress, true);
                await db.SaveChangesAsync();

                return (true, null, user);
            }
        }

        public void SignInCookie(HttpContextBase httpContext, User user, bool rememberMe = false)
        {
            var roleName = user.UserRole?.Name ?? user.Role ?? "Merchant";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("Username", user.Username ?? user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "ASP.NET Identity")
            };

            var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);

            var authManager = httpContext.GetOwinContext().Authentication;
            authManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            authManager.SignIn(new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? (DateTimeOffset?)DateTime.UtcNow.AddDays(14) : DateTime.UtcNow.AddHours(8)
            }, identity);

            // Also set a session cookie for UI role switching fallback
            var cookie = new HttpCookie("CurrentRole", roleName) { Expires = DateTime.Now.AddDays(7) };
            httpContext.Response.Cookies.Add(cookie);
        }

        public void SignOutCookie(HttpContextBase httpContext)
        {
            var authManager = httpContext.GetOwinContext().Authentication;
            authManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
        }

        public static async Task LogAuditAsync(ApplicationDbContext db, int? userId, string action, string entityType, int? entityId, string description, string ipAddress, bool success = true, string errorMessage = null)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    ActionType = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValue = description,
                    IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
                    Timestamp = DateTime.UtcNow,
                    Success = success,
                    ErrorMessage = errorMessage
                };
                db.AuditLogs.Add(log);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Silent fail for logging errors
            }
        }
    }
}
