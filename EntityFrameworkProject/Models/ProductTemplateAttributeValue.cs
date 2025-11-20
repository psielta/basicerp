using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("product_template_attribute_value")]
    public class ProductTemplateAttributeValue
    {
        [Column("product_template_id")]
        public Guid ProductTemplateId { get; set; }

        [Column("attribute_value_id")]
        public Guid AttributeValueId { get; set; }

        public ProductTemplate ProductTemplate { get; set; }

        public ProductAttributeValue AttributeValue { get; set; }
    }
}

