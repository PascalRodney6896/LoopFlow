using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 8: FinancingRequests
    [Table("FinancingRequests")]
    public class FinancingRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual PurchaseOrder Order { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal CreditLimitAtRequest { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Expired

        public DateTime? ApprovedDate { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string RejectionReason { get; set; }

        [Column(TypeName = "decimal")]
        public decimal? ApprovedAmount { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
