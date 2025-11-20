using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("product_template")]
    public class ProductTemplate
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Required]
        [Column("slug")]
        public string Slug { get; set; }

        [Column("brand")]
        public string Brand { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("warranty_months")]
        public int? WarrantyMonths { get; set; }

        [Column("is_service")]
        public bool IsService { get; set; }

        [Column("is_rental")]
        public bool IsRental { get; set; }

        [Column("has_delivery")]
        public bool HasDelivery { get; set; }

        [Column("ncm")]
        public string Ncm { get; set; }

        [Column("nbs")]
        public string Nbs { get; set; }

        [Column("freight_mode")]
        public short? FreightMode { get; set; }

        [Column("aggregator_code")]
        public string AggregatorCode { get; set; }

        [Column("created_by_user_id")]
        public Guid? CreatedByUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        public Organization Organization { get; set; }

        public User CreatedByUser { get; set; }

        public ICollection<ProductVariant> Variants { get; set; }

        public ICollection<ProductTemplateAttributeValue> AttributeValues { get; set; }

        public ICollection<ProductTemplateCategory> Categories { get; set; }

        public ICollection<ProductImage> Images { get; set; }
    }
}

