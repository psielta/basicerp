using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationBasic.Models.ViewModels
{
    public class ProductAttributeListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsVariant { get; set; }
        public int ValuesCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductAttributeValueItemViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "O valor do atributo é obrigatório")]
        [StringLength(200, ErrorMessage = "O valor deve ter no máximo {1} caracteres")]
        [Display(Name = "Valor")]
        public string Value { get; set; }

        [Display(Name = "Ordem")]
        public int SortOrder { get; set; }
    }

    public class ProductAttributeFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "O nome do atributo é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome deve ter no máximo {1} caracteres")]
        [Display(Name = "Nome")]
        public string Name { get; set; }

        // Code será gerenciado automaticamente a partir do nome
        [StringLength(200, ErrorMessage = "O código deve ter no máximo {1} caracteres")]
        public string Code { get; set; }

        [Display(Name = "Usar em variações (SKU)")]
        public bool IsVariant { get; set; }

        public IList<ProductAttributeValueItemViewModel> Values { get; set; } = new List<ProductAttributeValueItemViewModel>();
    }
}
