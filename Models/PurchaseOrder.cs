using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 6: PurchaseOrders
    [Table("PurchaseOrders")]
    public class PurchaseOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "LOOP_BNPL"; // Cash, LOOP_BNPL

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "PendingApproval"; // PendingApproval, Approved, Funded, Delivered, Completed

        public DateTime? RequiredDeliveryDate { get; set; }

        [StringLength(50)]
        public string SupplierVerificationStatus { get; set; } = "PENDING_VERIFICATION"; // PENDING_VERIFICATION, VERIFIED, REJECTED

        public string RejectionReason { get; set; }

        public bool InventoryAvailabilityConfirmed { get; set; } = false;

        [StringLength(50)]
        public string FulfilmentStatus { get; set; } = "Order Received"; // Order Received, Accepted, Inventory Confirmed, Fulfilment, Dispatched, Delivered, Paid

        public DateTime? DispatchedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        [StringLength(50)]
        public string FundingPath { get; set; } = "BANK_FINANCED"; // BANK_FINANCED, MERCHANT_FUNDED

        [StringLength(50)]
        public string FinancingStatus { get; set; } = "FACILITY_RESERVED"; // NOT_REQUIRED, FACILITY_RESERVED, UTILISATION_REQUESTED, BANK_APPROVED, DISBURSED

        [StringLength(50)]
        public string InvoiceStatus { get; set; } = "PENDING_GENERATION"; // PENDING_GENERATION, AUTO_GENERATED, POSTED_TO_BANK, VALIDATED, PAID

        [StringLength(50)]
        public string PaymentStatus { get; set; } = "UNPAID"; // UNPAID, PROCESSING, PAID

        [StringLength(50)]
        public string DeliveryStatus { get; set; } = "PENDING"; // PENDING, DISPATCHED, DELIVERED

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SupplierSplit> SupplierSplits { get; set; } = new List<SupplierSplit>();
        public virtual ICollection<SupplierInvoice> Invoices { get; set; } = new List<SupplierInvoice>();
        public virtual FinancingRequest FinancingRequest { get; set; }
    }
}
