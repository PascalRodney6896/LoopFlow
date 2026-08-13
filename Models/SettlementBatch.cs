using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 10: SettlementBatches
    [Table("SettlementBatches")]
    public class SettlementBatch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchNumber { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual PurchaseOrder Order { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalAmount { get; set; }

        public int SupplierCount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Completed";

        [StringLength(255)]
        public string HoldingAccountId { get; set; }

        public DateTime? SettlementDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; } = DateTime.UtcNow;
        public string ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
