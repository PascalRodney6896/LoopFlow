using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 5: CreditLimits
    [Table("CreditLimits")]
    public class CreditLimit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalCreditLimit { get; set; } = 500000.00m;

        [Column(TypeName = "decimal")]
        public decimal UsedCredit { get; set; } = 0.00m;

        [Column(TypeName = "decimal")]
        public decimal AvailableCredit { get; set; } = 500000.00m;

        [Column(TypeName = "decimal")]
        public decimal InterestRate { get; set; } = 17.00m;

        [Column(TypeName = "decimal")]
        public decimal FacilityFeeRate { get; set; } = 0.50m;

        [Column(TypeName = "decimal")]
        public decimal InsuranceFeeRate { get; set; } = 0.11m;

        [Column(TypeName = "decimal")]
        public decimal SweepPercentage { get; set; } = 30.00m;

        [Column(TypeName = "decimal")]
        public decimal? MaxExposureLimit { get; set; } = 35000000.00m;

        public bool IsActive { get; set; } = true;
        public DateTime? ApprovedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; } = DateTime.UtcNow.AddYears(1);

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
