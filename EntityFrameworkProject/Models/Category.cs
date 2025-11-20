using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("category")]
    public class Category
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

        [Column("path")]
        public string Path { get; set; }

        [Column("parent_id")]
        public Guid? ParentId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Organization Organization { get; set; }

        public Category Parent { get; set; }

        public ICollection<Category> Children { get; set; }

        public ICollection<ProductTemplateCategory> ProductTemplates { get; set; }
    }
}

