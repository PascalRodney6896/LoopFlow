using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    public class BankSuspenseLedgerEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(60)]
        public string TransactionReference { get; set; }

        public int BankSuspenseAccountId { get; set; }
        [ForeignKey("BankSuspenseAccountId")]
        public virtual BankSuspenseAccount BankSuspenseAccount { get; set; }

        public int? BuyerId { get; set; }
        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual PurchaseOrder Order { get; set; }

        public int? InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public virtual SupplierInvoice Invoice { get; set; }

        public int? FinancingRequestId { get; set; }
        [ForeignKey("FinancingRequestId")]
        public virtual FinancingRequest FinancingRequest { get; set; }

        [Required]
        [StringLength(40)]
        public string BucketType { get; set; } = "DISBURSEMENT_HOLDING"; // "DISBURSEMENT_HOLDING" vs "MERCHANT_REPAYMENT_COLLECTION"

        [Required]
        [StringLength(20)]
        public string EntryType { get; set; } // "Credit", "Debit"

        [Required]
        [StringLength(40)]
        public string LedgerState { get; set; } // "FundsReceived", "FundsHeld", "PendingDisbursement", "DisbursedToSupplier", "Failed", "Reversed", "Refunded", "Reconciled"

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        [StringLength(80)]
        public string SourceAccount { get; set; }

        [StringLength(80)]
        public string DestinationAccount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal")]
        public decimal ClosingBalance { get; set; }

        [StringLength(60)]
        public string ActorRole { get; set; } = "NCBA Bank System";

        [StringLength(30)]
        public string ReconciliationStatus { get; set; } = "Reconciled";

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
