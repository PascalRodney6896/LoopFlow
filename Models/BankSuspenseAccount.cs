using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    public class BankSuspenseAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string AccountNumber { get; set; } = "NCBA-SUSPENSE-001";

        [Required]
        [StringLength(100)]
        public string AccountName { get; set; } = "NCBA Bank Trade Financing Suspense Ledger";

        [Column(TypeName = "decimal")]
        public decimal TotalBalance { get; set; }

        // BUCKET 1: Supplier Disbursement Holding & Escrow
        [Column(TypeName = "decimal")]
        public decimal SupplierDisbursementBalance { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalDisbursedToSuppliers { get; set; }

        // BUCKET 2: Merchant Loan Repayment & Sweeps Collection
        [Column(TypeName = "decimal")]
        public decimal MerchantRepaymentCollectionBalance { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalMerchantRepaymentsCollected { get; set; }

        // Legacy / Summary aggregate fields
        [Column(TypeName = "decimal")]
        public decimal MerchantFundsReceived { get; set; }

        [Column(TypeName = "decimal")]
        public decimal FundsHeld { get; set; }

        [Column(TypeName = "decimal")]
        public decimal PendingDisbursement { get; set; }

        [Column(TypeName = "decimal")]
        public decimal TotalDisbursed { get; set; }

        [Column(TypeName = "decimal")]
        public decimal ReversedFailedAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal ReconciledBalance { get; set; }

        public int UnreconciledItemsCount { get; set; }

        public DateTime LastReconciledAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
