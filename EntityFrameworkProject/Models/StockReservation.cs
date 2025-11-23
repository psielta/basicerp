using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("stock_reservation")]
    public class StockReservation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Required]
        [Column("location_id")]
        public Guid LocationId { get; set; }

        [Required]
        [Column("variant_id")]
        public Guid VariantId { get; set; }

        [Required]
        [Column("quantity", TypeName = "numeric(18,3)")]
        public decimal Quantity { get; set; }

        [Required]
        [Column("status")]
        public short Status { get; set; }

        [Column("reserved_at")]
        public DateTime ReservedAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("source_type")]
        public string SourceType { get; set; }

        [Column("source_id")]
        public string SourceId { get; set; }

        [Column("source_line")]
        public string SourceLine { get; set; }

        [Required]
        [Column("deduplication_key")]
        public string DeduplicationKey { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("metadata")]
        public string Metadata { get; set; } = "{}";

        public StockLocation Location { get; set; }
        public ProductVariant Variant { get; set; }
        public Organization Organization { get; set; }
    }
}
