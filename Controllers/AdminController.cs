using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Attributes;
using LoopFlow.Models;
using LoopFlow.Services;

namespace LoopFlow.Controllers
{
    [CustomAuthorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // 1. Admin Dashboard Summary
        public async Task<ActionResult> Index()
        {
            var totalUsers = await _db.DomainUsers.CountAsync();
            var activeUsers = await _db.DomainUsers.CountAsync(u => u.IsActive && !u.IsLocked);
            var inactiveUsers = await _db.DomainUsers.CountAsync(u => !u.IsActive);
            var lockedUsers = await _db.DomainUsers.CountAsync(u => u.IsLocked);

            var totalAdmins = await _db.DomainUsers.CountAsync(u => u.UserRole.Name == "Admin" || u.Role == "Admin");
            var totalMerchants = await _db.DomainUsers.CountAsync(u => u.UserRole.Name == "Merchant" || u.Role == "Merchant" || u.Role == "Buyer");
            var totalFinanciers = await _db.DomainUsers.CountAsync(u => u.UserRole.Name == "Financier" || u.Role == "Financier");
            var totalSuppliers = await _db.DomainUsers.CountAsync(u => u.UserRole.Name == "Supplier" || u.Role == "Supplier");

            var recentAuditLogs = await _db.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.InactiveUsers = inactiveUsers;
            ViewBag.LockedUsers = lockedUsers;

            ViewBag.TotalAdmins = totalAdmins;
            ViewBag.TotalMerchants = totalMerchants;
            ViewBag.TotalFinanciers = totalFinanciers;
            ViewBag.TotalSuppliers = totalSuppliers;

            ViewBag.RecentLogs = recentAuditLogs;

            return View();
        }

        // 2. User Management List with Search & Filtering
        public async Task<ActionResult> Users(string search, int? roleId, bool? active, bool? locked)
        {
            var query = _db.DomainUsers.Include(u => u.UserRole).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u => 
                    (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                    (u.Username != null && u.Username.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
                    (u.BusinessName != null && u.BusinessName.ToLower().Contains(term)));
            }

            if (roleId.HasValue && roleId.Value > 0)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            if (active.HasValue)
            {
                query = query.Where(u => u.IsActive == active.Value);
            }

            if (locked.HasValue)
            {
                query = query.Where(u => u.IsLocked == locked.Value);
            }

            var usersList = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            ViewBag.Roles = await _db.DomainRoles.ToListAsync();

            ViewBag.Search = search;
            ViewBag.RoleId = roleId;
            ViewBag.Active = active;
            ViewBag.Locked = locked;

            return View(usersList);
        }

        // 3. Create User GET & POST
        [HttpGet]
        public async Task<ActionResult> CreateUser()
        {
            ViewBag.Roles = await _db.DomainRoles.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateUser(string fullName, string username, string email, string phoneNumber, string businessName, int roleId, string password)
        {
            ViewBag.Roles = await _db.DomainRoles.ToListAsync();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || roleId <= 0)
            {
                ModelState.AddModelError("", "Full Name, Email, Role, and Password are required.");
                return View();
            }

            // Check duplicate email / username
            string normEmail = email.Trim().ToLower();
            string normUser = string.IsNullOrWhiteSpace(username) ? normEmail : username.Trim().ToLower();

            if (await _db.DomainUsers.AnyAsync(u => u.Email.ToLower() == normEmail))
            {
                ModelState.AddModelError("", "A user with this email address already exists.");
                return View();
            }

            if (await _db.DomainUsers.AnyAsync(u => u.Username != null && u.Username.ToLower() == normUser))
            {
                ModelState.AddModelError("", "A user with this username already exists.");
                return View();
            }

            var selectedRole = await _db.DomainRoles.FindAsync(roleId);
            if (selectedRole == null)
            {
                ModelState.AddModelError("", "Invalid role selected.");
                return View();
            }

            string salt;
            string passwordHash = PasswordHasher.HashPassword(password, out salt);

            var newUser = new User
            {
                FullName = fullName.Trim(),
                Username = normUser,
                Email = normEmail,
                PhoneNumber = phoneNumber?.Trim() ?? "",
                BusinessName = businessName?.Trim() ?? "",
                Role = selectedRole.Name,
                RoleId = selectedRole.Id,
                PasswordHash = passwordHash,
                Salt = salt,
                IsActive = true,
                IsVerified = true,
                IsLocked = false,
                MustChangePassword = true,
                CreatedBy = User.Identity.Name ?? "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.DomainUsers.Add(newUser);
            await _db.SaveChangesAsync();

            // Create domain profile if Merchant or Supplier
            if (selectedRole.Name == "Merchant" || selectedRole.Name == "Buyer")
            {
                var buyerProfile = new Buyer
                {
                    UserId = newUser.Id,
                    BuyerCode = "BUY-" + new Random().Next(1000, 9999),
                    BusinessCategory = string.IsNullOrWhiteSpace(businessName) ? "General Retail" : businessName,
                    CreditScore = 75,
                    IsCreditApproved = true,
                    TotalPurchases = 0.00m
                };
                _db.Buyers.Add(buyerProfile);

                var creditLimit = new CreditLimit
                {
                    BuyerId = buyerProfile.Id,
                    TotalCreditLimit = 500000.00m,
                    AvailableCredit = 500000.00m,
                    UsedCredit = 0.00m,
                    IsActive = true,
                    ApprovedDate = DateTime.UtcNow
                };
                _db.CreditLimits.Add(creditLimit);

                var wallet = new LoopAccount
                {
                    UserId = newUser.Id,
                    LoopAccountId = "ACC-LOOP-MER-" + newUser.Id,
                    LoopWalletId = "WLT-LOOP-MER-" + newUser.Id,
                    WalletNumber = "20200" + new Random().Next(10000, 99999),
                    AccountNumber = "0100" + new Random().Next(1000000, 9999999),
                    LoopCustomerCode = "CUST-MER-" + newUser.Id,
                    WalletBalance = 0.00m,
                    AccountBalance = 0.00m
                };
                _db.LoopAccounts.Add(wallet);
                await _db.SaveChangesAsync();
            }
            else if (selectedRole.Name == "Supplier")
            {
                var supplierProfile = new Supplier
                {
                    UserId = newUser.Id,
                    SupplierCode = "SUP-" + new Random().Next(1000, 9999),
                    BusinessCategory = string.IsNullOrWhiteSpace(businessName) ? "General Supply" : businessName,
                    BusinessRegistration = "REG-" + new Random().Next(10000, 99999),
                    KRA_PIN = "P051" + new Random().Next(100000, 999999) + "Z",
                    IsVerifiedSupplier = true,
                    TotalSales = 0.00m
                };
                _db.Suppliers.Add(supplierProfile);

                var wallet = new LoopAccount
                {
                    UserId = newUser.Id,
                    LoopAccountId = "ACC-LOOP-SUP-" + newUser.Id,
                    LoopWalletId = "WLT-LOOP-SUP-" + newUser.Id,
                    WalletNumber = "20200" + new Random().Next(10000, 99999),
                    AccountNumber = "0100" + new Random().Next(1000000, 9999999),
                    LoopCustomerCode = "CUST-SUP-" + newUser.Id,
                    WalletBalance = 0.00m,
                    AccountBalance = 0.00m
                };
                _db.LoopAccounts.Add(wallet);
                await _db.SaveChangesAsync();
            }

            await AuthService.LogAuditAsync(_db, GetCurrentUserId(), "User Created", "User", newUser.Id, $"Admin created {selectedRole.Name} user: {newUser.Email}", Request.UserHostAddress);

            TempData["Success"] = $"User '{newUser.FullName}' created successfully as {selectedRole.Name}.";
            return RedirectToAction("Users");
        }

        // 4. Edit User GET & POST
        [HttpGet]
        public async Task<ActionResult> EditUser(int id)
        {
            var user = await _db.DomainUsers.Include(u => u.UserRole).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return HttpNotFound();

            ViewBag.Roles = await _db.DomainRoles.ToListAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditUser(int id, string fullName, string username, string email, string phoneNumber, string businessName, int roleId, bool isActive, bool isLocked)
        {
            var user = await _db.DomainUsers.Include(u => u.UserRole).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return HttpNotFound();

            ViewBag.Roles = await _db.DomainRoles.ToListAsync();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || roleId <= 0)
            {
                ModelState.AddModelError("", "Full Name, Email, and Role are required.");
                return View(user);
            }

            var selectedRole = await _db.DomainRoles.FindAsync(roleId);
            if (selectedRole == null)
            {
                ModelState.AddModelError("", "Invalid role.");
                return View(user);
            }

            // Record audit changes
            string oldRole = user.UserRole?.Name ?? user.Role;
            bool oldActive = user.IsActive;
            bool oldLocked = user.IsLocked;

            user.FullName = fullName.Trim();
            user.Username = string.IsNullOrWhiteSpace(username) ? email.Trim().ToLower() : username.Trim().ToLower();
            user.Email = email.Trim().ToLower();
            user.PhoneNumber = phoneNumber?.Trim() ?? "";
            user.BusinessName = businessName?.Trim() ?? "";
            user.RoleId = selectedRole.Id;
            user.Role = selectedRole.Name;
            user.IsActive = isActive;
            user.IsLocked = isLocked;
            user.UpdatedBy = User.Identity.Name ?? "Admin";
            user.UpdatedAt = DateTime.UtcNow;

            if (isLocked != oldLocked && !isLocked)
            {
                user.FailedLoginAttempts = 0; // Reset failed attempts when unlocked
            }

            await _db.SaveChangesAsync();

            await AuthService.LogAuditAsync(_db, GetCurrentUserId(), "User Updated", "User", user.Id, $"Admin updated user details for {user.Email} (Role: {oldRole} -> {selectedRole.Name}, Active: {isActive}, Locked: {isLocked})", Request.UserHostAddress);

            TempData["Success"] = $"User '{user.FullName}' updated successfully.";
            return RedirectToAction("Users");
        }

        // 5. Toggle Active / Inactive Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleActive(int id)
        {
            var user = await _db.DomainUsers.FindAsync(id);
            if (user == null) return HttpNotFound();

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = User.Identity.Name ?? "Admin";
            await _db.SaveChangesAsync();

            string statusText = user.IsActive ? "Activated" : "Deactivated";
            await AuthService.LogAuditAsync(_db, GetCurrentUserId(), user.IsActive ? "User Activated" : "User Deactivated", "User", user.Id, $"Admin {statusText.ToLower()} user {user.Email}", Request.UserHostAddress);

            TempData["Success"] = $"User '{user.FullName}' has been {statusText.ToLower()}.";
            return RedirectToAction("Users");
        }

        // 6. Toggle Lock / Unlock Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleLock(int id)
        {
            var user = await _db.DomainUsers.FindAsync(id);
            if (user == null) return HttpNotFound();

            user.IsLocked = !user.IsLocked;
            if (!user.IsLocked)
            {
                user.FailedLoginAttempts = 0;
            }
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = User.Identity.Name ?? "Admin";
            await _db.SaveChangesAsync();

            string lockText = user.IsLocked ? "Locked" : "Unlocked";
            await AuthService.LogAuditAsync(_db, GetCurrentUserId(), user.IsLocked ? "User Locked" : "User Unlocked", "User", user.Id, $"Admin {lockText.ToLower()} user {user.Email}", Request.UserHostAddress);

            TempData["Success"] = $"User '{user.FullName}' has been {lockText.ToLower()}.";
            return RedirectToAction("Users");
        }

        // 7. Admin Reset Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(int id, string newPassword)
        {
            var user = await _db.DomainUsers.FindAsync(id);
            if (user == null) return HttpNotFound();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters long.";
                return RedirectToAction("Users");
            }

            string salt;
            string passwordHash = PasswordHasher.HashPassword(newPassword, out salt);

            user.PasswordHash = passwordHash;
            user.Salt = salt;
            user.MustChangePassword = true;
            user.PasswordChangedDate = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = User.Identity.Name ?? "Admin";
            await _db.SaveChangesAsync();

            await AuthService.LogAuditAsync(_db, GetCurrentUserId(), "Password Reset", "User", user.Id, $"Admin reset password for user {user.Email}", Request.UserHostAddress);

            TempData["Success"] = $"Password for '{user.FullName}' was reset successfully.";
            return RedirectToAction("Users");
        }

        // 8. Audit Logs History View
        public async Task<ActionResult> AuditLogs(string actionFilter, string search)
        {
            var query = _db.AuditLogs.Include(a => a.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.ActionType == actionFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(a => 
                    (a.OldValue != null && a.OldValue.ToLower().Contains(term)) ||
                    (a.IpAddress != null && a.IpAddress.Contains(term)) ||
                    (a.User != null && a.User.FullName.ToLower().Contains(term)));
            }

            var logs = await query.OrderByDescending(a => a.Timestamp).Take(200).ToListAsync();

            ViewBag.ActionFilter = actionFilter;
            ViewBag.Search = search;

            return View(logs);
        }

        private int GetCurrentUserId()
        {
            if (User.Identity.IsAuthenticated)
            {
                var identity = User.Identity as System.Security.Claims.ClaimsIdentity;
                var userIdClaim = identity?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? identity?.FindFirst("UserId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int id))
                {
                    return id;
                }
            }
            return 1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
