using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 7: SupplierSplits
    [Table("SupplierSplits")]
    public class SupplierSplit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual PurchaseOrder Order { get; set; }

        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        [Required]
        [StringLength(255)]
        public string SupplierName { get; set; }

        [Required]
        [StringLength(50)]
        public string SupplierCode { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        public string ItemDescription { get; set; }
        public int? Quantity { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? UnitPrice { get; set; }

        public bool IsPaid { get; set; } = false;

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "PENDING"; // PENDING, PROCESSING, COMPLETED, FAILED, REVERSED

        public DateTime? PaymentDate { get; set; }

        [StringLength(50)]
        public string InvoiceNumber { get; set; }

        [StringLength(50)]
        public string VerificationStatus { get; set; } = "PENDING_VERIFICATION"; // PENDING_VERIFICATION, VERIFIED, REJECTED

        public string RejectionReason { get; set; }

        [StringLength(100)]
        public string TransactionReference { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
