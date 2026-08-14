using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    [Table("LoopTransactions")]
    public class LoopTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string InternalTransactionId { get; set; }

        [Required]
        [StringLength(100)]
        public string TxnReference { get; set; }

        [StringLength(60)]
        public string ApiOperation { get; set; }

        [StringLength(50)]
        public string ServiceCode { get; set; }

        [StringLength(30)]
        public string Channel { get; set; }

        [StringLength(50)]
        public string MerchantTill { get; set; }

        [StringLength(100)]
        public string Recipient { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        [StringLength(255)]
        public string Purpose { get; set; }

        [StringLength(50)]
        public string RequestStatus { get; set; } = "PENDING"; // PENDING, COMPLETED, FAILED, RETRYING

        [StringLength(50)]
        public string ServiceTransactionStatus { get; set; }

        [StringLength(50)]
        public string LoopStatusCode { get; set; }

        public string LoopMessage { get; set; }

        [StringLength(100)]
        public string TransactionRef { get; set; }

        [StringLength(100)]
        public string RequestReference { get; set; }

        [StringLength(100)]
        public string TransferOrderId { get; set; }

        [StringLength(100)]
        public string TransferRefNo { get; set; }

        [StringLength(100)]
        public string RequestId { get; set; }

        [StringLength(100)]
        public string ResponseId { get; set; }

        public int RetryCount { get; set; } = 0;

        public string FailureReason { get; set; }

        public string RawResponseJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
