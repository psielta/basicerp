using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("product_attribute_value")]
    public class ProductAttributeValue
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("attribute_id")]
        public Guid AttributeId { get; set; }

        [Required]
        [Column("value")]
        public string Value { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ProductAttribute Attribute { get; set; }

        public ICollection<ProductVariantAttributeValue> VariantValues { get; set; }
    }
}

