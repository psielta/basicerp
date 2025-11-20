using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("product_variant_attribute_value")]
    public class ProductVariantAttributeValue
    {
        [Column("variant_id")]
        public Guid VariantId { get; set; }

        [Column("attribute_value_id")]
        public Guid AttributeValueId { get; set; }

        public ProductVariant Variant { get; set; }

        public ProductAttributeValue AttributeValue { get; set; }
    }
}

