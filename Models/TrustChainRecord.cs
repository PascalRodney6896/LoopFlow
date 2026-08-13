using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 14: TrustChainRecords
    [Table("TrustChainRecords")]
    public class TrustChainRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual PurchaseOrder Order { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = "Settlement";

        [Required]
        public string EventData { get; set; }

        [Required]
        [StringLength(255)]
        public string Hash { get; set; }

        [StringLength(255)]
        public string PreviousHash { get; set; }

        public int? VerifiedBy { get; set; }

        [ForeignKey("VerifiedBy")]
        public virtual User VerifiedByUser { get; set; }

        [Required]
        [StringLength(50)]
        public string VerificationStatus { get; set; } = "Verified";

        public DateTime? VerificationDate { get; set; } = DateTime.UtcNow;
        public bool IsTampered { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
