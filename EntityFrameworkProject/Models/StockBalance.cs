using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("stock_balance")]
    public class StockBalance
    {
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("location_id")]
        public Guid LocationId { get; set; }

        [Column("variant_id")]
        public Guid VariantId { get; set; }

        [Column("on_hand", TypeName = "numeric(18,3)")]
        public decimal OnHand { get; set; }

        [Column("reserved", TypeName = "numeric(18,3)")]
        public decimal Reserved { get; set; }

        [Column("last_movement_at")]
        public DateTime LastMovementAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public StockLocation Location { get; set; }
        public ProductVariant Variant { get; set; }
        public Organization Organization { get; set; }
    }
}
