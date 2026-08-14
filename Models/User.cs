using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 1: Users
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(255)]
        public string FullName { get; set; }

        [StringLength(255)]
        public string BusinessName { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } // 'Merchant', 'Supplier', 'Admin', 'Financier'

        public int? RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role UserRole { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(255)]
        public string Salt { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LastLoginDate { get; set; }
        public DateTime? PasswordChangedDate { get; set; }
        public bool MustChangePassword { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; }

        [StringLength(100)]
        public string UpdatedBy { get; set; }

        public string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        // Navigation Properties
        public virtual LoopAccount LoopAccount { get; set; }
        public virtual Buyer BuyerProfile { get; set; }
        public virtual Supplier SupplierProfile { get; set; }
    }
}
