using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    // TABLE 4: Suppliers
    [Table("Suppliers")]
    public class Supplier
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        [StringLength(50)]
        public string SupplierCode { get; set; }

        [StringLength(100)]
        public string BusinessRegistration { get; set; }

        [StringLength(50)]
        public string KRA_PIN { get; set; }

        [StringLength(100)]
        public string BusinessCategory { get; set; }

        [StringLength(50)]
        public string ContactPhone { get; set; }

        [StringLength(100)]
        public string ContactEmail { get; set; }

        [StringLength(255)]
        public string BusinessAddress { get; set; }

        [StringLength(100)]
        public string SettlementBank { get; set; } = "NCBA Bank Kenya";

        [StringLength(50)]
        public string SettlementAccount { get; set; }

        [StringLength(100)]
        public string SettlementAccountName { get; set; }

        [StringLength(255)]
        public string PaymentDetails { get; set; }

        [StringLength(50)]
        public string KYCStatus { get; set; } = "Verified";

        [Column(TypeName = "decimal")]
        public decimal? AverageOrderValue { get; set; }

        public bool IsVerifiedSupplier { get; set; } = true;

        [Column(TypeName = "decimal")]
        public decimal? Rating { get; set; } = 4.8m;

        [Column(TypeName = "decimal")]
        public decimal TotalSales { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<SupplierSplit> SupplierSplits { get; set; } = new List<SupplierSplit>();
        public virtual ICollection<SupplierInvoice> SupplierInvoices { get; set; } = new List<SupplierInvoice>();
    }
}
