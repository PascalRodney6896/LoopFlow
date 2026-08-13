using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 11: SweepConfigurations
    [Table("SweepConfigurations")]
    public class SweepConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        [Required]
        [StringLength(50)]
        public string SweepType { get; set; } = "Fixed";

        [Column(TypeName = "decimal")]
        public decimal? FixedPercentage { get; set; } = 30.00m;

        [Column(TypeName = "decimal")]
        public decimal MinimumBalance { get; set; } = 1000.00m;

        [Column(TypeName = "decimal")]
        public decimal? Tier1Threshold { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? Tier1Percentage { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? Tier2Threshold { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? Tier2Percentage { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? Tier3Percentage { get; set; }

        [Required]
        [StringLength(50)]
        public string SweepFrequency { get; set; } = "Daily";

        public bool IsActive { get; set; } = true;
        public DateTime? LastSweepDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
