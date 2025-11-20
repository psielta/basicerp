using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("product_image")]
    public class ProductImage
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("product_template_id")]
        public Guid ProductTemplateId { get; set; }

        [Column("variant_id")]
        public Guid? VariantId { get; set; }

        [Required]
        [Column("url")]
        public string Url { get; set; }

        [Column("alt_text")]
        public string AltText { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("is_main")]
        public bool IsMain { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ProductTemplate ProductTemplate { get; set; }

        public ProductVariant Variant { get; set; }
    }
}

