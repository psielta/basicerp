using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("stock_ledger")]
    public class StockLedger
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

        [Column("delta_on_hand", TypeName = "numeric(18,3)")]
        public decimal DeltaOnHand { get; set; }

        [Column("delta_reserved", TypeName = "numeric(18,3)")]
        public decimal DeltaReserved { get; set; }

        [Required]
        [Column("movement_type")]
        public short MovementType { get; set; }

        [Column("reason")]
        public string Reason { get; set; }

        [Column("source_type")]
        public string SourceType { get; set; }

        [Column("source_id")]
        public string SourceId { get; set; }

        [Column("source_line")]
        public string SourceLine { get; set; }

        [Required]
        [Column("deduplication_key")]
        public string DeduplicationKey { get; set; }

        [Column("occurred_at")]
        public DateTime OccurredAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("metadata")]
        public string Metadata { get; set; } = "{}";

        public StockLocation Location { get; set; }
        public ProductVariant Variant { get; set; }
        public Organization Organization { get; set; }
    }
}
