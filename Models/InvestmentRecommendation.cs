using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 17: InvestmentRecommendations
    [Table("InvestmentRecommendations")]
    public class InvestmentRecommendation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductType { get; set; } = "MMF";

        [Required]
        [StringLength(255)]
        public string ProductName { get; set; } = "NCBA Loop High-Yield MMF";

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Rate { get; set; } = 11.50m;

        [Required]
        [StringLength(50)]
        public string Tenor { get; set; } = "30 days";

        [Required]
        [StringLength(50)]
        public string Liquidity { get; set; } = "High";

        [Required]
        [StringLength(20)]
        public string RiskLevel { get; set; } = "Low";

        [Required]
        public string Explanation { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime? AcceptedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(7);

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
