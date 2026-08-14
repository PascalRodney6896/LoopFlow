using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoopFlow.Models
{
    [Table("Roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // 'Admin', 'Merchant', 'Financier', 'Supplier'

        [StringLength(255)]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
