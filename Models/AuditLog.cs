using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 15: AuditLogs & Activity Timeline Ledger
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [StringLength(100)]
        public string ActorName { get; set; }

        [StringLength(50)]
        public string ActorRole { get; set; } // Merchant, Supplier, NCBA Bank System, Automated Orchestrator

        [Required]
        [StringLength(100)]
        public string ActionType { get; set; }

        [StringLength(50)]
        public string EntityType { get; set; } // PurchaseOrder, SupplierInvoice, FinancingRequest, SupplierSplit, LoanTransaction

        public int? EntityId { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; }

        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Notes { get; set; }

        [StringLength(50)]
        public string IpAddress { get; set; } = "127.0.0.1";

        [StringLength(255)]
        public string UserAgent { get; set; } = "LoopFlowApp/1.0";

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; }
    }
}
