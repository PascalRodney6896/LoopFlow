using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 18: CashFlowForecasts
    [Table("CashFlowForecasts")]
    public class CashFlowForecast
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        public DateTime ForecastDate { get; set; } = DateTime.UtcNow.Date;

        [Column(TypeName = "decimal")]
        public decimal ProjectedInflow { get; set; }

        [Column(TypeName = "decimal")]
        public decimal ProjectedOutflow { get; set; }

        [Column(TypeName = "decimal")]
        public decimal NetCashFlow { get; set; }

        [Column(TypeName = "decimal")]
        public decimal ProjectedBalance { get; set; }

        public bool IsDeficit { get; set; } = false;

        [Column(TypeName = "decimal")]
        public decimal? DeficitAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? SurplusAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? ConfidenceLevel { get; set; } = 0.94m;

        [Required]
        [StringLength(20)]
        public string PeriodType { get; set; } = "30Day";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
