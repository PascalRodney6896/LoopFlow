using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 12: SweepHistory
    [Table("SweepHistory")]
    public class SweepHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        [Column(TypeName = "decimal")]
        public decimal SweepAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal SweepPercentage { get; set; } = 30.00m;

        [Column(TypeName = "decimal")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal")]
        public decimal BalanceAfter { get; set; }

        [Column(TypeName = "decimal")]
        public decimal LoanBalanceBefore { get; set; }

        [Column(TypeName = "decimal")]
        public decimal LoanBalanceAfter { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed";

        [StringLength(100)]
        public string TransactionReference { get; set; }

        public string ErrorMessage { get; set; }
        public DateTime SweepDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
