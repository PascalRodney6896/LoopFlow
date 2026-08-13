using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 3: Buyers
    [Table("Buyers")]
    public class Buyer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [StringLength(50)]
        public string BuyerCode { get; set; }

        [StringLength(100)]
        public string BusinessCategory { get; set; }

        public int? YearsInBusiness { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? AverageMonthlySpend { get; set; }

        public int? CreditScore { get; set; } = 85;

        public bool IsCreditApproved { get; set; } = true;

        [Column(TypeName = "decimal")]
        public decimal TotalPurchases { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual CreditLimit CreditLimit { get; set; }
        public virtual SweepConfiguration SweepConfiguration { get; set; }
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
        public virtual ICollection<FinancingRequest> FinancingRequests { get; set; } = new List<FinancingRequest>();
        public virtual ICollection<LoanTransaction> LoanTransactions { get; set; } = new List<LoanTransaction>();
    }
}
