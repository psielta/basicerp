using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WebApplicationBasic.Models.ViewModels
{
    // Enum para tipo de produto
    public enum ProductType
    {
        [Display(Name = "Produto Simples")]
        Simple,
        [Display(Name = "Produto com Variações")]
        Configurable
    }

    public class ProductListItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Brand { get; set; }
        public string Sku { get; set; }
        public bool IsService { get; set; }
        public bool IsRental { get; set; }
        public bool HasDelivery { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Novos campos para mostrar informações adicionais
        public int VariantCount { get; set; }
        public int ActiveVariantCount { get; set; }
        public ProductType ProductType => VariantCount > 1 ? ProductType.Configurable : ProductType.Simple;
        public List<string> Categories { get; set; } = new List<string>();
    }

    // ViewModel para seleção inicial do tipo de produto
    public class ProductTypeSelectionViewModel
    {
        [Required(ErrorMessage = "Selecione o tipo de produto")]
        [Display(Name = "Tipo de Produto")]
        public ProductType ProductType { get; set; }

        [Display(Name = "Continuar para cadastro")]
        public bool ContinueToForm { get; set; }
    }

    // ViewModel aprimorado para formulário de produto
    public class ProductFormViewModel
    {
        public Guid? Id { get; set; }

        [Display(Name = "Tipo de Produto")]
        public ProductType ProductType { get; set; }

        // === Dados do Template (SPU) ===
        [Required(ErrorMessage = "O nome do produto é obrigatório")]
        [StringLength(200, ErrorMessage = "O nome deve ter no máximo {1} caracteres")]
        [Display(Name = "Nome")]
        public string Name { get; set; }

        [StringLength(200, ErrorMessage = "O slug deve ter no máximo {1} caracteres")]
        public string Slug { get; set; }

        [StringLength(200)]
        [Display(Name = "Marca")]
        public string Brand { get; set; }

        [Display(Name = "Descrição")]
        public string Description { get; set; }

        [Display(Name = "Garantia (meses)")]
        public int? WarrantyMonths { get; set; }

        [Display(Name = "É serviço")]
        public bool IsService { get; set; }

        [Display(Name = "É aluguel")]
        public bool IsRental { get; set; }

        [Display(Name = "Possui entrega")]
        public bool HasDelivery { get; set; } = true;

        [Display(Name = "NCM")]
        public string Ncm { get; set; }

        [Display(Name = "NBS")]
        public string Nbs { get; set; }

        [Display(Name = "Modo de frete (código interno)")]
        public short? FreightMode { get; set; }

        [Display(Name = "Código agregador (API externa)")]
        public string AggregatorCode { get; set; }

        // === Categorias ===
        [Display(Name = "Categorias")]
        public List<Guid> SelectedCategoryIds { get; set; } = new List<Guid>();
        public List<CategorySelectionViewModel> AvailableCategories { get; set; } = new List<CategorySelectionViewModel>();

        // === Atributos Descritivos (não-variantes) ===
        [Display(Name = "Atributos do Produto")]
        public List<AttributeAssignmentViewModel> DescriptiveAttributes { get; set; } = new List<AttributeAssignmentViewModel>();

        // === Variante principal (para produtos simples) ===
        public Guid? VariantId { get; set; }

        [Display(Name = "SKU")]
        public string Sku { get; set; }

        [Display(Name = "Nome da variação (opcional)")]
        public string VariantName { get; set; }

        [Display(Name = "Descrição da variação (opcional)")]
        public string VariantDescription { get; set; }

        [Display(Name = "Custo")]
        [DataType(DataType.Currency)]
        public decimal? Cost { get; set; }

        [Display(Name = "Peso")]
        public decimal? Weight { get; set; }

        [Display(Name = "Altura")]
        public decimal? Height { get; set; }

        [Display(Name = "Largura")]
        public decimal? Width { get; set; }

        [Display(Name = "Comprimento")]
        public decimal? Length { get; set; }

        [Display(Name = "Código de barras (EAN/GTIN)")]
        public string Barcode { get; set; }

        [Display(Name = "Descrição de variação (bruta)")]
        public string RawVariationDescription { get; set; }

        [Display(Name = "Ativo")]
        public bool IsActive { get; set; } = true;

        // === Atributos de Variação (para produtos configuráveis) ===
        [Display(Name = "Atributos de Variação")]
        public List<VariantAttributeSelectionViewModel> VariantAttributes { get; set; } = new List<VariantAttributeSelectionViewModel>();

        // === Lista de Variantes (para produtos configuráveis) ===
        public List<VariantFormViewModel> Variants { get; set; } = new List<VariantFormViewModel>();
    }

    // ViewModel para cada variante individual
    public class VariantFormViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "O SKU é obrigatório")]
        [StringLength(50, ErrorMessage = "O SKU deve ter no máximo 50 caracteres")]
        [Display(Name = "SKU")]
        public string Sku { get; set; }

        [Display(Name = "Nome")]
        public string Name { get; set; }

        [Display(Name = "Descrição")]
        public string Description { get; set; }

        [Display(Name = "Custo")]
        [Range(0, 999999.99, ErrorMessage = "O custo deve ser entre 0 e 999999.99")]
        public decimal? Cost { get; set; }

        [Display(Name = "Peso (kg)")]
        public decimal? Weight { get; set; }

        [Display(Name = "Altura (cm)")]
        public decimal? Height { get; set; }

        [Display(Name = "Largura (cm)")]
        public decimal? Width { get; set; }

        [Display(Name = "Comprimento (cm)")]
        public decimal? Length { get; set; }

        [Display(Name = "Código de Barras")]
        public string Barcode { get; set; }

        [Display(Name = "Ativo")]
        public bool IsActive { get; set; }

        // Atributos de variação desta variante específica
        public Dictionary<Guid, Guid> VariantAttributeValues { get; set; } = new Dictionary<Guid, Guid>();

        // Para exibição
        public string VariantDescription { get; set; }
    }

    // ViewModel para seleção de categorias
    public class CategorySelectionViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public Guid? ParentId { get; set; }
        public bool IsSelected { get; set; }
        public int Level { get; set; }
    }

    // ViewModel para atribuição de atributos descritivos
    public class AttributeAssignmentViewModel
    {
        public Guid AttributeId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeCode { get; set; }
        public List<Guid> SelectedValueIds { get; set; } = new List<Guid>();
        public List<AttributeValueSelectionViewModel> AvailableValues { get; set; } = new List<AttributeValueSelectionViewModel>();
    }

    // ViewModel para seleção de valores de atributo
    public class AttributeValueSelectionViewModel
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
        public int SortOrder { get; set; }
        public bool IsSelected { get; set; }
    }

    // ViewModel para seleção de atributos de variação
    public class VariantAttributeSelectionViewModel
    {
        public Guid AttributeId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeCode { get; set; }
        public bool IsUsedForVariants { get; set; }
        public List<AttributeValueSelectionViewModel> SelectedValues { get; set; } = new List<AttributeValueSelectionViewModel>();
    }

    // ViewModel para gerenciamento de variantes
    public class ProductVariantManagementViewModel
    {
        public Guid ProductTemplateId { get; set; }
        public string ProductName { get; set; }
        public string ProductSlug { get; set; }
        public List<VariantListItemViewModel> Variants { get; set; } = new List<VariantListItemViewModel>();
        public Dictionary<Guid, string> VariantAttributes { get; set; } = new Dictionary<Guid, string>();
    }

    // ViewModel para item de lista de variantes
    public class VariantListItemViewModel
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public decimal? Cost { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<string, string> AttributeValues { get; set; } = new Dictionary<string, string>();
        public string VariantDescription => string.Join(", ", AttributeValues.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    // ViewModel para edição individual de variante
    public class VariantEditViewModel
    {
        public Guid Id { get; set; }
        public Guid ProductTemplateId { get; set; }

        [Display(Name = "Produto")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "O SKU é obrigatório")]
        [StringLength(50, ErrorMessage = "O SKU deve ter no máximo 50 caracteres")]
        [Display(Name = "SKU")]
        public string Sku { get; set; }

        [Display(Name = "Nome da Variante")]
        public string Name { get; set; }

        [Display(Name = "Descrição")]
        public string Description { get; set; }

        [Display(Name = "Custo")]
        [Range(0, 999999.99, ErrorMessage = "O custo deve ser entre 0 e 999999.99")]
        public decimal? Cost { get; set; }

        [Display(Name = "Peso (kg)")]
        public decimal? Weight { get; set; }

        [Display(Name = "Altura (cm)")]
        public decimal? Height { get; set; }

        [Display(Name = "Largura (cm)")]
        public decimal? Width { get; set; }

        [Display(Name = "Comprimento (cm)")]
        public decimal? Length { get; set; }

        [Display(Name = "Código de Barras")]
        public string Barcode { get; set; }

        [Display(Name = "Ativo")]
        public bool IsActive { get; set; }

        [Display(Name = "Atributos de Variação")]
        public Dictionary<string, string> VariantAttributes { get; set; } = new Dictionary<string, string>();
    }

    // ViewModel para adicionar nova variante
    public class AddVariantViewModel
    {
        public Guid ProductTemplateId { get; set; }

        [Display(Name = "Produto")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "O SKU é obrigatório")]
        [StringLength(50, ErrorMessage = "O SKU deve ter no máximo 50 caracteres")]
        [Display(Name = "SKU")]
        public string Sku { get; set; }

        [Display(Name = "Nome da Variante")]
        public string Name { get; set; }

        [Display(Name = "Descrição")]
        public string Description { get; set; }

        [Display(Name = "Custo")]
        [Range(0, 999999.99, ErrorMessage = "O custo deve ser entre 0 e 999999.99")]
        public decimal? Cost { get; set; }

        [Display(Name = "Peso (kg)")]
        public decimal? Weight { get; set; }

        [Display(Name = "Altura (cm)")]
        public decimal? Height { get; set; }

        [Display(Name = "Largura (cm)")]
        public decimal? Width { get; set; }

        [Display(Name = "Comprimento (cm)")]
        public decimal? Length { get; set; }

        [Display(Name = "Código de Barras")]
        public string Barcode { get; set; }

        [Display(Name = "Ativo")]
        public bool IsActive { get; set; }

        [Display(Name = "Atributos de Variação")]
        public List<VariantAttributeSelectionViewModel> VariantAttributes { get; set; } = new List<VariantAttributeSelectionViewModel>();

        [Display(Name = "Valores de Atributos Selecionados")]
        public List<Guid> SelectedAttributeValues { get; set; } = new List<Guid>();
    }
}