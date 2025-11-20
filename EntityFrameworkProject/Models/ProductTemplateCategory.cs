using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("product_template_category")]
    public class ProductTemplateCategory
    {
        [Column("product_template_id")]
        public Guid ProductTemplateId { get; set; }

        [Column("category_id")]
        public Guid CategoryId { get; set; }

        public ProductTemplate ProductTemplate { get; set; }

        public Category Category { get; set; }
    }
}

