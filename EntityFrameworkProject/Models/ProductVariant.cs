using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("product_variant")]
    public class ProductVariant
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("product_template_id")]
        public Guid ProductTemplateId { get; set; }

        // Mantemos organization_id para suportar unicidade de SKU por tenant
        [Required]
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Required]
        [Column("sku")]
        public string Sku { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("cost")]
        public decimal? Cost { get; set; }

        [Column("weight")]
        public decimal? Weight { get; set; }

        [Column("height")]
        public decimal? Height { get; set; }

        [Column("width")]
        public decimal? Width { get; set; }

        [Column("length")]
        public decimal? Length { get; set; }

        [Column("barcode")]
        public string Barcode { get; set; }

        [Column("raw_variation_description")]
        public string RawVariationDescription { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        public ProductTemplate ProductTemplate { get; set; }

        public Organization Organization { get; set; }

        public ICollection<ProductVariantAttributeValue> AttributeValues { get; set; }

        public ICollection<ProductImage> Images { get; set; }
    }
}

