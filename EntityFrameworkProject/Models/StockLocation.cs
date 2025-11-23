using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("stock_location")]
    public class StockLocation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Required]
        [Column("code")]
        public string Code { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("is_virtual")]
        public bool IsVirtual { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Organization Organization { get; set; }
    }
}
