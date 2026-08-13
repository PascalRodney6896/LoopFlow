using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 9: LoanTransactions
    [Table("LoanTransactions")]
    public class LoanTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual PurchaseOrder Order { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = "Disbursement"; // Disbursement, Repayment, Sweep, Adjustment

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? PrincipalAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? InterestAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? FeeAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal")]
        public decimal BalanceAfter { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed";

        [StringLength(100)]
        public string TransactionReference { get; set; }

        [StringLength(255)]
        public string LoopAccountId { get; set; }

        [StringLength(255)]
        public string LoopWalletId { get; set; }

        public string Notes { get; set; }
        public DateTime? SettlementDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
