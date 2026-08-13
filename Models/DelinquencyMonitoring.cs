using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 13: DelinquencyMonitoring
    [Table("DelinquencyMonitoring")]
    public class DelinquencyMonitoring
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int BuyerId { get; set; }

        [ForeignKey("BuyerId")]
        public virtual Buyer Buyer { get; set; }

        public int? LoanTransactionId { get; set; }

        [ForeignKey("LoanTransactionId")]
        public virtual LoanTransaction LoanTransaction { get; set; }

        public int DaysPastDue { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string DelinquencyStage { get; set; } = "Stage0";

        public bool IsNonPerforming { get; set; } = false;
        public DateTime? LastActionDate { get; set; }
        public string CollectionsNotes { get; set; }

        [StringLength(50)]
        public string RecoveryStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
