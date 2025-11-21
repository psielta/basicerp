using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationBasic.Models.ViewModels
{
    public class CategoryListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Path { get; set; }
        public string ParentName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CategoryFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "O nome da categoria é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome deve ter no máximo {1} caracteres")]
        [Display(Name = "Nome")]
        public string Name { get; set; }

        // Slug será gerenciado automaticamente pelo sistema
        [StringLength(200, ErrorMessage = "O slug deve ter no máximo {1} caracteres")]
        public string Slug { get; set; }

        [Display(Name = "Categoria pai")]
        public Guid? ParentId { get; set; }

        public IList<ParentCategoryOption> ParentOptions { get; set; } = new List<ParentCategoryOption>();
    }

    public class ParentCategoryOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }
}

