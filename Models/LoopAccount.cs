using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 2: LoopAccounts
    [Table("LoopAccounts")]
    public class LoopAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [StringLength(255)]
        public string LoopAccountId { get; set; }

        [StringLength(255)]
        public string LoopWalletId { get; set; }

        [StringLength(50)]
        public string WalletNumber { get; set; }

        [StringLength(50)]
        public string AccountNumber { get; set; }

        [StringLength(100)]
        public string LoopCustomerCode { get; set; }

        public bool IsWalletCreated { get; set; } = true;
        public bool IsAccountLinked { get; set; } = true;

        [Column(TypeName = "decimal")]
        public decimal WalletBalance { get; set; }

        [Column(TypeName = "decimal")]
        public decimal AccountBalance { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastBalanceCheck { get; set; }
    }
}
