using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using WebApplicationBasic.Filters;
using WebApplicationBasic.Models.ViewModels;
using Serilog;

namespace WebApplicationBasic.Controllers
{
    [CustomAuthorize(OrganizationRoles = "admin,owner")]
    public class ProductsController : BaseController
    {
        // GET: /Products
        public async Task<ActionResult> Index(string search)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para gerenciar produtos.";
                return RedirectToAction("Index", "Home");
            }

            var query = Context.ProductTemplates
                .Include(p => p.Variants)
                .Include(p => p.Categories)
                    .ThenInclude(pc => pc.Category)
                .Where(p => p.OrganizationId == CurrentOrganizationId && p.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    p.Slug.Contains(search) ||
                    p.Brand.Contains(search) ||
                    p.Variants.Any(v => v.Sku.Contains(search)));
            }

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductListItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Brand = p.Brand,
                    ProductType = (ProductType)p.ProductType,
                    IsService = p.IsService,
                    IsRental = p.IsRental,
                    HasDelivery = p.HasDelivery,
                    CreatedAt = p.CreatedAt,
                    Sku = p.Variants
                        .Where(v => v.DeletedAt == null)
                        .OrderBy(v => v.CreatedAt)
                        .Select(v => v.Sku)
                        .FirstOrDefault(),
                    VariantCount = p.Variants.Count(v => v.DeletedAt == null),
                    ActiveVariantCount = p.Variants.Count(v => v.IsActive && v.DeletedAt == null),
                    IsActive = p.Variants.Any(v => v.IsActive && v.DeletedAt == null),
                    Categories = p.Categories.Select(pc => pc.Category.Name).ToList()
                })
                .ToListAsync();

            ViewBag.Search = search;
            return View(items);
        }

        // GET: /Products/SelectType
        public ActionResult SelectType()
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para cadastrar produtos.";
                return RedirectToAction("Index", "Home");
            }

            var model = new ProductTypeSelectionViewModel();
            return View(model);
        }

        // POST: /Products/CreateWizard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateWizard(ProductTypeSelectionViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para cadastrar produtos.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View("SelectType", model);
            }

            var formModel = new ProductFormViewModel
            {
                ProductType = model.ProductType,
                HasDelivery = true,
                IsActive = true
            };

            // Carregar categorias disponíveis
            await LoadAvailableCategories(formModel);

            // Carregar atributos disponíveis
            await LoadAvailableAttributes(formModel);

            return View("CreateWizard", formModel);
        }

        // GET: /Products/Create (redireciona para novo fluxo)
        public ActionResult Create()
        {
            return RedirectToAction("SelectType");
        }

        // GET: /Products/Edit/{id}
        public async Task<ActionResult> Edit(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para editar produtos.";
                return RedirectToAction("Index", "Home");
            }

            // Carregar template com TODAS as relações necessárias
            var template = await Context.ProductTemplates
                .Include(p => p.Variants)
                    .ThenInclude(v => v.AttributeValues)
                        .ThenInclude(va => va.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .Include(p => p.Categories)
                    .ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == CurrentOrganizationId && p.DeletedAt == null);

            if (template == null)
            {
                return HttpNotFound();
            }

            // Usar o tipo de produto armazenado no banco
            var activeVariants = template.Variants.Where(v => v.DeletedAt == null).ToList();
            var productType = (ProductType)template.ProductType;

            var model = new ProductFormViewModel
            {
                Id = template.Id,
                ProductType = productType,
                Name = template.Name,
                Slug = template.Slug,
                Brand = template.Brand,
                Description = template.Description,
                WarrantyMonths = template.WarrantyMonths,
                IsService = template.IsService,
                IsRental = template.IsRental,
                HasDelivery = template.HasDelivery,
                Ncm = template.Ncm,
                Nbs = template.Nbs,
                FreightMode = template.FreightMode,
                AggregatorCode = template.AggregatorCode
            };

            // Carregar categorias selecionadas
            model.SelectedCategoryIds = template.Categories
                .Select(c => c.CategoryId)
                .ToList();

            // Para produto simples, carregar dados da única variante
            if (productType == ProductType.Simple)
            {
                var mainVariant = activeVariants.FirstOrDefault();
                if (mainVariant != null)
                {
                    model.VariantId = mainVariant.Id;
                    model.Sku = mainVariant.Sku;
                    model.VariantName = mainVariant.Name;
                    model.VariantDescription = mainVariant.Description;
                    model.Cost = mainVariant.Cost;
                    model.Weight = mainVariant.Weight;
                    model.Height = mainVariant.Height;
                    model.Width = mainVariant.Width;
                    model.Length = mainVariant.Length;
                    model.Barcode = mainVariant.Barcode;
                    model.RawVariationDescription = mainVariant.RawVariationDescription;
                    model.IsActive = mainVariant.IsActive;
                }
            }
            else
            {
                // Para produto configurável, carregar todas as variantes
                model.Variants = activeVariants.Select(v => new VariantFormViewModel
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    Name = v.Name,
                    Description = v.Description,
                    Cost = v.Cost,
                    Weight = v.Weight,
                    Height = v.Height,
                    Width = v.Width,
                    Length = v.Length,
                    Barcode = v.Barcode,
                    IsActive = v.IsActive,
                    VariantAttributeValues = v.AttributeValues
                        .Where(av => av.AttributeValue != null && av.AttributeValue.Attribute != null)
                        .ToDictionary(
                            av => av.AttributeValue.AttributeId,
                            av => av.AttributeValueId
                        ),
                    VariantAttributeValuesList = v.AttributeValues
                        .Where(av => av.AttributeValue != null && av.AttributeValue.Attribute != null)
                        .Select(av => new VariantAttributeValuePair
                        {
                            AttributeId = av.AttributeValue.AttributeId,
                            AttributeValueId = av.AttributeValueId
                        })
                        .ToList(),
                    VariantDescription = string.Join(", ", v.AttributeValues
                        .Where(av => av.AttributeValue != null && av.AttributeValue.Attribute != null)
                        .Select(av => $"{av.AttributeValue.Attribute.Name}: {av.AttributeValue.Value}")
                    )
                }).ToList();
            }

            // Carregar categorias disponíveis
            await LoadAvailableCategories(model);

            // Carregar atributos disponíveis e marcar os selecionados
            await LoadAvailableAttributesForEdit(model, template);

            return View("EditWizard", model);
        }

        // Novo método auxiliar para carregar atributos no modo edição
        private async Task LoadAvailableAttributesForEdit(ProductFormViewModel model, ProductTemplate template)
        {
            var attributes = await Context.ProductAttributes
                .Include(a => a.Values)
                .Where(a => a.OrganizationId == CurrentOrganizationId)
                .ToListAsync();

            // Atributos de variação usados nas variantes
            var variantAttributeValueIds = new HashSet<Guid>();
            if (template != null && template.Variants != null)
            {
                foreach (var variant in template.Variants.Where(v => v.DeletedAt == null))
                {
                    foreach (var av in variant.AttributeValues)
                    {
                        variantAttributeValueIds.Add(av.AttributeValueId);
                    }
                }
            }

            if (model.ProductType == ProductType.Simple)
            {
                // Para produtos simples, não há mais atributos descritivos no template
                // Mantendo a estrutura vazia para compatibilidade
                model.DescriptiveAttributes = new List<AttributeAssignmentViewModel>();
            }
            else
            {
                // Para produtos configuráveis, apenas atributos de variação
                model.VariantAttributes = attributes
                    .Select(a => new VariantAttributeSelectionViewModel
                    {
                        AttributeId = a.Id,
                        AttributeName = a.Name,
                        AttributeCode = a.Code,
                        IsUsedForVariants = a.Values.Any(v => variantAttributeValueIds.Contains(v.Id)),
                        SelectedValues = a.Values
                            .OrderBy(v => v.SortOrder)
                            .Select(v => new AttributeValueSelectionViewModel
                            {
                                Id = v.Id,
                                Value = v.Value,
                                SortOrder = v.SortOrder,
                                IsSelected = variantAttributeValueIds.Contains(v.Id)
                            })
                            .ToList()
                    })
                    .ToList();

                // Não há mais atributos descritivos no template
                model.DescriptiveAttributes = new List<AttributeAssignmentViewModel>();
            }
        }

        // POST: /Products/UpdateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProduct(ProductFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para editar produtos.";
                return RedirectToAction("Index", "Home");
            }

            // Remover validações de campos opcionais para produtos configuráveis
            if (model.ProductType == ProductType.Configurable)
            {
                ModelState.Remove("Sku");
                ModelState.Remove("VariantName");
                ModelState.Remove("VariantDescription");
                ModelState.Remove("Cost");
                ModelState.Remove("Weight");
                ModelState.Remove("Height");
                ModelState.Remove("Width");
                ModelState.Remove("Length");
                ModelState.Remove("Barcode");
                ModelState.Remove("RawVariationDescription");

                // Remover validações de VariantAttributes (são usados apenas para UI, não para persistência)
                // e validações de variantes individuais que podem ter campos opcionais vazios
                var keysToRemove = ModelState.Keys
                    .Where(k => k.StartsWith("VariantAttributes[") ||
                               (k.StartsWith("Variants[") &&
                               (k.EndsWith("].Name") ||
                                k.EndsWith("].Description") ||
                                k.EndsWith("].Barcode") ||
                                k.EndsWith("].Weight") ||
                                k.EndsWith("].Height") ||
                                k.EndsWith("].Width") ||
                                k.EndsWith("].Length"))))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    ModelState.Remove(key);
                }
            }

            if (!ModelState.IsValid)
            {
                // Log detalhado dos erros de validação
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Key = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList() })
                    .ToList();

                if (errors.Any())
                {
                    var errorMessages = string.Join("; ", errors.SelectMany(e =>
                        e.Errors.Select(err => $"{e.Key}: {err}")));
                    Log.Warning("UPDATE_PRODUCT_VALIDATION_FAILED: Erros de validação para produto {ProductId}: {Errors}",
                        model.Id, errorMessages);
                }

                await LoadAvailableCategories(model);
                await LoadAvailableAttributesForEdit(model, null);
                return View("EditWizard", model);
            }

            // Carregar template com todas as relações
            var template = await Context.ProductTemplates
                .Include(p => p.Variants)
                    .ThenInclude(v => v.AttributeValues)
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.OrganizationId == CurrentOrganizationId);

            if (template == null)
            {
                return HttpNotFound();
            }

            try
            {
                // Gerar slug automaticamente a partir do nome
                var slug = Slugify(model.Name);

                // Validar slug único (exceto o próprio)
                var slugExists = await Context.ProductTemplates
                    .AnyAsync(p => p.OrganizationId == CurrentOrganizationId &&
                                   p.Slug == slug &&
                                   p.Id != template.Id);

                if (slugExists)
                {
                    ModelState.AddModelError("Name", "Já existe um produto com este nome nesta organização.");
                    await LoadAvailableCategories(model);
                    await LoadAvailableAttributesForEdit(model, template);
                    return View("EditWizard", model);
                }

                // Atualizar dados do template
                template.Name = model.Name;
                template.Slug = slug;
                template.Brand = model.Brand;
                template.Description = model.Description;
                template.WarrantyMonths = model.WarrantyMonths;
                template.IsService = model.IsService;
                template.IsRental = model.IsRental;
                template.HasDelivery = model.HasDelivery;
                template.Ncm = model.Ncm;
                template.Nbs = model.Nbs;
                template.FreightMode = model.FreightMode;
                template.AggregatorCode = model.AggregatorCode;
                template.UpdatedAt = DateTime.UtcNow;

                // Sincronizar categorias
                var existingCategories = template.Categories.ToList();

                // Remover categorias não mais selecionadas
                foreach (var cat in existingCategories)
                {
                    if (model.SelectedCategoryIds == null || !model.SelectedCategoryIds.Contains(cat.CategoryId))
                    {
                        Context.Entry(cat).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
                    }
                }

                // Adicionar novas categorias
                if (model.SelectedCategoryIds != null)
                {
                    foreach (var catId in model.SelectedCategoryIds)
                    {
                        if (!existingCategories.Any(c => c.CategoryId == catId))
                        {
                            template.Categories.Add(new ProductTemplateCategory
                            {
                                ProductTemplateId = template.Id,
                                CategoryId = catId
                            });
                        }
                    }
                }

                // Atualizar variantes
                if (model.ProductType == ProductType.Simple)
                {
                    // Para produtos simples, atualizar única variante
                    var mainVariant = template.Variants.FirstOrDefault(v => v.Id == model.VariantId);
                    if (mainVariant == null)
                    {
                        mainVariant = template.Variants.FirstOrDefault();
                        if (mainVariant == null)
                        {
                            // Criar nova variante se não existir
                            mainVariant = new ProductVariant
                            {
                                OrganizationId = CurrentOrganizationId,
                                CreatedAt = DateTime.UtcNow
                            };
                            template.Variants.Add(mainVariant);
                        }
                    }

                    // Validar SKU único
                    var skuExists = await Context.ProductVariants
                        .AnyAsync(v => v.OrganizationId == CurrentOrganizationId &&
                                       v.Sku == model.Sku &&
                                       v.Id != mainVariant.Id);

                    if (skuExists)
                    {
                        ModelState.AddModelError("Sku", "Já existe um produto com este SKU nesta organização.");
                        await LoadAvailableCategories(model);
                        await LoadAvailableAttributesForEdit(model, template);
                        return View("EditWizard", model);
                    }

                    mainVariant.Sku = model.Sku;
                    mainVariant.Name = string.IsNullOrWhiteSpace(model.VariantName) ? model.Name : model.VariantName;
                    mainVariant.Description = string.IsNullOrWhiteSpace(model.VariantDescription)
                        ? model.Description
                        : model.VariantDescription;
                    mainVariant.Cost = model.Cost;
                    mainVariant.Weight = model.Weight;
                    mainVariant.Height = model.Height;
                    mainVariant.Width = model.Width;
                    mainVariant.Length = model.Length;
                    mainVariant.Barcode = model.Barcode;
                    mainVariant.RawVariationDescription = model.RawVariationDescription;
                    mainVariant.IsActive = model.IsActive;
                    mainVariant.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Para produtos configuráveis, atualizar múltiplas variantes
                    if (model.Variants != null)
                    {
                        foreach (var variantModel in model.Variants)
                        {
                            if (variantModel.Id.HasValue)
                            {
                                // Atualizar variante existente
                                var existingVariant = template.Variants.FirstOrDefault(v => v.Id == variantModel.Id.Value);
                                if (existingVariant != null)
                                {
                                    existingVariant.Sku = variantModel.Sku;
                                    existingVariant.Name = variantModel.Name ?? model.Name;
                                    existingVariant.Description = variantModel.Description ?? model.Description;
                                    existingVariant.Cost = variantModel.Cost;
                                    existingVariant.Weight = variantModel.Weight;
                                    existingVariant.Height = variantModel.Height;
                                    existingVariant.Width = variantModel.Width;
                                    existingVariant.Length = variantModel.Length;
                                    existingVariant.Barcode = variantModel.Barcode;
                                    existingVariant.IsActive = variantModel.IsActive;
                                    existingVariant.UpdatedAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                // Criar nova variante
                                var newVariant = new ProductVariant
                                {
                                    OrganizationId = CurrentOrganizationId,
                                    ProductTemplateId = template.Id,
                                    Sku = variantModel.Sku,
                                    Name = variantModel.Name ?? model.Name,
                                    Description = variantModel.Description ?? model.Description,
                                    Cost = variantModel.Cost,
                                    Weight = variantModel.Weight,
                                    Height = variantModel.Height,
                                    Width = variantModel.Width,
                                    Length = variantModel.Length,
                                    Barcode = variantModel.Barcode,
                                    IsActive = variantModel.IsActive,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                // Adicionar atributos de variação
                                if (variantModel.VariantAttributeValuesList != null && variantModel.VariantAttributeValuesList.Any())
                                {
                                    newVariant.AttributeValues = new List<ProductVariantAttributeValue>();
                                    foreach (var attrValue in variantModel.VariantAttributeValuesList)
                                    {
                                        newVariant.AttributeValues.Add(new ProductVariantAttributeValue
                                        {
                                            VariantId = newVariant.Id,
                                            AttributeValueId = attrValue.AttributeValueId
                                        });
                                    }
                                }
                                else if (variantModel.VariantAttributeValues != null && variantModel.VariantAttributeValues.Any())
                                {
                                    // Fallback para Dictionary (compatibilidade)
                                    newVariant.AttributeValues = new List<ProductVariantAttributeValue>();
                                    foreach (var kvp in variantModel.VariantAttributeValues)
                                    {
                                        newVariant.AttributeValues.Add(new ProductVariantAttributeValue
                                        {
                                            VariantId = newVariant.Id,
                                            AttributeValueId = kvp.Value
                                        });
                                    }
                                }

                                template.Variants.Add(newVariant);
                            }
                        }
                    }
                }

                await Context.SaveChangesAsync();

                TempData["Success"] = "Produto atualizado com sucesso.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException dbEx) when (IsDuplicateSkuError(dbEx))
            {
                Log.Warning(dbEx, "PRODUCT_UPDATE_DUPLICATE_SKU: SKU duplicado ao atualizar produto {ProductId} para organização {OrganizationId} por usuário {UserId}",
                    model.Id, CurrentOrganizationId, CurrentUserId);

                // Identificar qual SKU está duplicado
                var duplicateSkus = FindDuplicateSkus(model);
                var errorMessage = duplicateSkus.Any()
                    ? $"Já existe(m) variante(s) com o(s) SKU(s): {string.Join(", ", duplicateSkus)}. Por favor, altere para valores únicos."
                    : "Já existe uma variante com este SKU. Por favor, utilize um SKU diferente.";

                ModelState.AddModelError("", errorMessage);
                await LoadAvailableCategories(model);
                await LoadAvailableAttributesForEdit(model, template);
                return View("EditWizard", model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PRODUCT_UPDATE_ERROR: Erro ao atualizar produto {ProductId} para organização {OrganizationId} por usuário {UserId}",
                    model.Id, CurrentOrganizationId, CurrentUserId);
                ModelState.AddModelError("", "Ocorreu um erro ao atualizar o produto: " + ex.Message);
                await LoadAvailableCategories(model);
                await LoadAvailableAttributesForEdit(model, template);
                return View("EditWizard", model);
            }
        }

        // POST: /Products/Edit (manter para compatibilidade)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ProductFormViewModel model)
        {
            // Redirecionar para o novo método
            return await UpdateProduct(model);
        }

        // GET: /Products/Delete/{id}
        public async Task<ActionResult> Delete(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para excluir produtos.";
                return RedirectToAction("Index", "Home");
            }

            var template = await Context.ProductTemplates
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == CurrentOrganizationId);

            if (template == null)
            {
                return HttpNotFound();
            }

            var mainVariant = template.Variants
                .OrderBy(v => v.CreatedAt)
                .FirstOrDefault();

            var model = new ProductListItemViewModel
            {
                Id = template.Id,
                Name = template.Name,
                Slug = template.Slug,
                Brand = template.Brand,
                Sku = mainVariant?.Sku,
                IsService = template.IsService,
                IsRental = template.IsRental,
                HasDelivery = template.HasDelivery,
                IsActive = mainVariant?.IsActive ?? false,
                CreatedAt = template.CreatedAt
            };

            return View(model);
        }

        // POST: /Products/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para excluir produtos.";
                return RedirectToAction("Index", "Home");
            }

            var template = await Context.ProductTemplates
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == CurrentOrganizationId);

            if (template == null)
            {
                return HttpNotFound();
            }

            var productName = template.Name;
            Context.ProductTemplates.Remove(template);
            await Context.SaveChangesAsync();

            Log.Information("PRODUCT_DELETED: Produto {ProductId} \"{ProductName}\" excluído por usuário {UserId} na organização {OrganizationId}",
                id, productName, CurrentUserId, CurrentOrganizationId);

            TempData["Success"] = "Produto excluído com sucesso.";
            return RedirectToAction("Index");
        }

        // POST: /Products/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateProduct(ProductFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para cadastrar produtos.";
                return RedirectToAction("Index", "Home");
            }

            // Debug: Log model state errors
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                // Para debug: mostrar os erros
                var errorMessages = string.Join("; ", errors.SelectMany(e =>
                    e.Errors.Select(err => $"{e.Key}: {err.ErrorMessage}")));

                System.Diagnostics.Debug.WriteLine("ModelState Errors: " + errorMessages);

                // Remover validações de campos que não são obrigatórios para todos os tipos
                if (model.ProductType == ProductType.Configurable)
                {
                    // Para produtos configuráveis, o SKU da variante principal não é obrigatório
                    ModelState.Remove("Sku");
                    ModelState.Remove("VariantName");
                    ModelState.Remove("VariantDescription");
                    ModelState.Remove("Cost");
                    ModelState.Remove("Weight");
                    ModelState.Remove("Height");
                    ModelState.Remove("Width");
                    ModelState.Remove("Length");
                    ModelState.Remove("Barcode");
                    ModelState.Remove("RawVariationDescription");
                }

                if (!ModelState.IsValid)
                {
                    await LoadAvailableCategories(model);
                    await LoadAvailableAttributes(model);
                    return View("CreateWizard", model);
                }
            }

            // Validações específicas por tipo de produto
            if (model.ProductType == ProductType.Simple)
            {
                if (string.IsNullOrWhiteSpace(model.Sku))
                {
                    ModelState.AddModelError("Sku", "O SKU é obrigatório para produtos simples.");
                    await LoadAvailableCategories(model);
                    await LoadAvailableAttributes(model);
                    return View("CreateWizard", model);
                }
            }
            else if (model.ProductType == ProductType.Configurable)
            {
                if (model.Variants == null || !model.Variants.Any())
                {
                    ModelState.AddModelError("", "Produtos configuráveis devem ter pelo menos uma variante. Use o botão 'Gerar Variações'.");
                    await LoadAvailableCategories(model);
                    await LoadAvailableAttributes(model);
                    return View("CreateWizard", model);
                }
            }

            try
            {
                // Gerar slug
                var slug = Slugify(model.Name);

                // Validar slug único
                var slugExists = await Context.ProductTemplates
                    .AnyAsync(p => p.OrganizationId == CurrentOrganizationId && p.Slug == slug);

                if (slugExists)
                {
                    ModelState.AddModelError("Name", "Já existe um produto com este nome nesta organização.");
                    await LoadAvailableCategories(model);
                    await LoadAvailableAttributes(model);
                    return View("CreateWizard", model);
                }

                // Criar o template do produto
                var template = new ProductTemplate
                {
                    OrganizationId = CurrentOrganizationId,
                    Name = model.Name,
                    Slug = slug,
                    ProductType = (short)model.ProductType,
                    Brand = model.Brand,
                    Description = model.Description,
                    WarrantyMonths = model.WarrantyMonths,
                    IsService = model.IsService,
                    IsRental = model.IsRental,
                    HasDelivery = model.HasDelivery,
                    Ncm = model.Ncm,
                    Nbs = model.Nbs,
                    FreightMode = model.FreightMode,
                    AggregatorCode = model.AggregatorCode,
                    CreatedByUserId = CurrentUserId == Guid.Empty ? (Guid?)null : CurrentUserId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (model.ProductType == ProductType.Simple)
                {
                    // Produto simples - criar única variante
                    var variant = new ProductVariant
                    {
                        OrganizationId = CurrentOrganizationId,
                        Sku = model.Sku,
                        Name = string.IsNullOrWhiteSpace(model.VariantName) ? model.Name : model.VariantName,
                        Description = string.IsNullOrWhiteSpace(model.VariantDescription)
                            ? model.Description
                            : model.VariantDescription,
                        Cost = model.Cost,
                        Weight = model.Weight,
                        Height = model.Height,
                        Width = model.Width,
                        Length = model.Length,
                        Barcode = model.Barcode,
                        RawVariationDescription = model.RawVariationDescription,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    template.Variants = new[] { variant };
                }
                else
                {
                    // Produto configurável - criar múltiplas variantes
                    var variants = new List<ProductVariant>();

                    if (model.Variants != null)
                    {
                        foreach (var variantModel in model.Variants)
                        {
                            var variant = new ProductVariant
                            {
                                OrganizationId = CurrentOrganizationId,
                                Sku = variantModel.Sku,
                                Name = variantModel.Name ?? model.Name,
                                Description = variantModel.Description ?? model.Description,
                                Cost = variantModel.Cost,
                                Weight = variantModel.Weight,
                                Height = variantModel.Height,
                                Width = variantModel.Width,
                                Length = variantModel.Length,
                                Barcode = variantModel.Barcode,
                                IsActive = variantModel.IsActive,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            // Adicionar relações de atributos de variação
                            if (variantModel.VariantAttributeValuesList != null && variantModel.VariantAttributeValuesList.Any())
                            {
                                variant.AttributeValues = new List<ProductVariantAttributeValue>();
                                foreach (var attrValue in variantModel.VariantAttributeValuesList)
                                {
                                    variant.AttributeValues.Add(new ProductVariantAttributeValue
                                    {
                                        AttributeValueId = attrValue.AttributeValueId
                                    });
                                }
                            }
                            else if (variantModel.VariantAttributeValues != null && variantModel.VariantAttributeValues.Any())
                            {
                                // Fallback para Dictionary (compatibilidade com código antigo)
                                variant.AttributeValues = new List<ProductVariantAttributeValue>();
                                foreach (var kvp in variantModel.VariantAttributeValues)
                                {
                                    variant.AttributeValues.Add(new ProductVariantAttributeValue
                                    {
                                        AttributeValueId = kvp.Value
                                    });
                                }
                            }

                            variants.Add(variant);
                        }
                    }

                    if (variants.Count == 0)
                    {
                        ModelState.AddModelError("", "Produtos com variações devem ter pelo menos uma variante.");
                        await LoadAvailableCategories(model);
                        await LoadAvailableAttributes(model);
                        return View("CreateWizard", model);
                    }

                    template.Variants = variants;
                }

                // Adicionar categorias selecionadas
                if (model.SelectedCategoryIds != null && model.SelectedCategoryIds.Any())
                {
                    template.Categories = new List<ProductTemplateCategory>();
                    foreach (var categoryId in model.SelectedCategoryIds)
                    {
                        template.Categories.Add(new ProductTemplateCategory
                        {
                            CategoryId = categoryId
                        });
                    }
                }

                Context.ProductTemplates.Add(template);
                await Context.SaveChangesAsync();

                Log.Information("PRODUCT_CREATED: Produto {ProductId} \"{ProductName}\" ({ProductType}) criado com {VariantCount} variante(s) por usuário {UserId} na organização {OrganizationId}",
                    template.Id, template.Name, model.ProductType, template.Variants?.Count ?? 0, CurrentUserId, CurrentOrganizationId);

                TempData["Success"] = "Produto criado com sucesso.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException dbEx) when (IsDuplicateSkuError(dbEx))
            {
                Log.Warning(dbEx, "PRODUCT_CREATE_DUPLICATE_SKU: SKU duplicado ao criar produto para organização {OrganizationId} por usuário {UserId}",
                    CurrentOrganizationId, CurrentUserId);

                // Identificar qual SKU está duplicado
                var duplicateSkus = FindDuplicateSkus(model);
                var errorMessage = duplicateSkus.Any()
                    ? $"Já existe(m) variante(s) com o(s) SKU(s): {string.Join(", ", duplicateSkus)}. Por favor, altere para valores únicos."
                    : "Já existe uma variante com este SKU. Por favor, utilize um SKU diferente.";

                ModelState.AddModelError("", errorMessage);
                await LoadAvailableCategories(model);
                await LoadAvailableAttributes(model);
                return View("CreateWizard", model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PRODUCT_CREATE_ERROR: Erro ao criar produto para organização {OrganizationId} por usuário {UserId}",
                    CurrentOrganizationId, CurrentUserId);
                ModelState.AddModelError("", "Ocorreu um erro ao criar o produto: " + ex.Message);
                await LoadAvailableCategories(model);
                await LoadAvailableAttributes(model);
                return View("CreateWizard", model);
            }
        }

        // GET: /Products/ManageVariants/{id}
        public async Task<ActionResult> ManageVariants(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para gerenciar variantes.";
                return RedirectToAction("Index", "Home");
            }

            var template = await Context.ProductTemplates
                .Include(p => p.Variants)
                    .ThenInclude(v => v.AttributeValues)
                        .ThenInclude(va => va.AttributeValue)
                            .ThenInclude(av => av.Attribute)
                .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == CurrentOrganizationId);

            if (template == null)
            {
                return HttpNotFound();
            }

            var model = new ProductVariantManagementViewModel
            {
                ProductTemplateId = template.Id,
                ProductName = template.Name,
                ProductSlug = template.Slug,
                Variants = template.Variants
                    .Where(v => v.DeletedAt == null)
                    .Select(v => new VariantListItemViewModel
                    {
                        Id = v.Id,
                        Sku = v.Sku,
                        Name = v.Name,
                        Cost = v.Cost,
                        IsActive = v.IsActive,
                        CreatedAt = v.CreatedAt,
                        UpdatedAt = v.UpdatedAt,
                        AttributeValues = v.AttributeValues
                            .Where(av => av.AttributeValue != null && av.AttributeValue.Attribute != null)
                            .ToDictionary(
                                av => av.AttributeValue.Attribute.Name,
                                av => av.AttributeValue.Value
                            )
                    })
                    .ToList()
            };

            return View(model);
        }

        // POST: /Products/ToggleVariant/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleVariant(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização.";
                return RedirectToAction("Index", "Home");
            }

            var variant = await Context.ProductVariants
                .Include(v => v.ProductTemplate)
                .FirstOrDefaultAsync(v => v.Id == id && v.OrganizationId == CurrentOrganizationId);

            if (variant == null)
            {
                return HttpNotFound();
            }

            variant.IsActive = !variant.IsActive;
            variant.UpdatedAt = DateTime.UtcNow;
            await Context.SaveChangesAsync();

            TempData["Success"] = variant.IsActive ?
                "Variante ativada com sucesso." :
                "Variante desativada com sucesso.";

            return RedirectToAction("ManageVariants", new { id = variant.ProductTemplateId });
        }

        // GET: /Products/EditVariant/{id}
        public async Task<ActionResult> EditVariant(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização.";
                return RedirectToAction("Index", "Home");
            }

            var variant = await Context.ProductVariants
                .Include(v => v.ProductTemplate)
                .Include(v => v.AttributeValues)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.Attribute)
                .FirstOrDefaultAsync(v => v.Id == id && v.OrganizationId == CurrentOrganizationId);

            if (variant == null)
            {
                return HttpNotFound();
            }

            var model = new VariantEditViewModel
            {
                Id = variant.Id,
                ProductTemplateId = variant.ProductTemplateId,
                ProductName = variant.ProductTemplate.Name,
                Sku = variant.Sku,
                Name = variant.Name,
                Description = variant.Description,
                Cost = variant.Cost,
                Weight = variant.Weight,
                Height = variant.Height,
                Width = variant.Width,
                Length = variant.Length,
                Barcode = variant.Barcode,
                IsActive = variant.IsActive,
                VariantAttributes = variant.AttributeValues
                    .Where(av => av.AttributeValue != null && av.AttributeValue.Attribute != null)
                    .ToDictionary(
                        av => av.AttributeValue.Attribute.Name,
                        av => av.AttributeValue.Value
                    )
            };

            return View(model);
        }

        // POST: /Products/EditVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditVariant(VariantEditViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var variant = await Context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == model.Id && v.OrganizationId == CurrentOrganizationId);

            if (variant == null)
            {
                return HttpNotFound();
            }

            // Validar SKU único (exceto o próprio)
            var skuExists = await Context.ProductVariants
                .AnyAsync(v => v.OrganizationId == CurrentOrganizationId &&
                               v.Sku == model.Sku &&
                               v.Id != variant.Id);

            if (skuExists)
            {
                ModelState.AddModelError("Sku", "Já existe uma variante com este SKU nesta organização.");
                return View(model);
            }

            variant.Sku = model.Sku;
            variant.Name = model.Name;
            variant.Description = model.Description;
            variant.Cost = model.Cost;
            variant.Weight = model.Weight;
            variant.Height = model.Height;
            variant.Width = model.Width;
            variant.Length = model.Length;
            variant.Barcode = model.Barcode;
            variant.IsActive = model.IsActive;
            variant.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            Log.Information("PRODUCT_VARIANT_UPDATED: Variante {VariantId} (SKU: {Sku}) atualizada por usuário {UserId} na organização {OrganizationId}",
                variant.Id, variant.Sku, CurrentUserId, CurrentOrganizationId);

            TempData["Success"] = "Variante atualizada com sucesso.";
            return RedirectToAction("ManageVariants", new { id = variant.ProductTemplateId });
        }

        // GET: /Products/AddVariant/{templateId}
        public async Task<ActionResult> AddVariant(Guid templateId)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização.";
                return RedirectToAction("Index", "Home");
            }

            var template = await Context.ProductTemplates
                .FirstOrDefaultAsync(p => p.Id == templateId && p.OrganizationId == CurrentOrganizationId);

            if (template == null)
            {
                return HttpNotFound();
            }

            // Carregar atributos de variação que estão em uso neste produto
            var existingVariants = await Context.ProductVariants
                .Include(v => v.AttributeValues)
                .Where(v => v.ProductTemplateId == templateId && v.DeletedAt == null)
                .ToListAsync();

            // Obter os IDs de atributos usados nas variantes existentes
            var usedAttributeValueIds = existingVariants
                .SelectMany(v => v.AttributeValues.Select(av => av.AttributeValueId))
                .Distinct()
                .ToList();

            var usedAttributeIds = await Context.ProductAttributeValues
                .Where(pav => usedAttributeValueIds.Contains(pav.Id))
                .Select(pav => pav.AttributeId)
                .Distinct()
                .ToListAsync();

            // Carregar apenas atributos que estão em uso neste produto
            var variantAttributes = await Context.ProductAttributes
                .Include(a => a.Values)
                .Where(a => a.OrganizationId == CurrentOrganizationId && usedAttributeIds.Contains(a.Id))
                .Select(a => new VariantAttributeSelectionViewModel
                {
                    AttributeId = a.Id,
                    AttributeName = a.Name,
                    AttributeCode = a.Code,
                    IsUsedForVariants = true,
                    SelectedValues = a.Values
                        .OrderBy(v => v.SortOrder)
                        .Select(v => new AttributeValueSelectionViewModel
                        {
                            Id = v.Id,
                            Value = v.Value,
                            SortOrder = v.SortOrder,
                            IsSelected = false
                        })
                        .ToList()
                })
                .ToListAsync();

            // Criar lista de combinações de atributos já existentes
            var existingCombinations = existingVariants
                .Select(v => System.Text.Json.JsonSerializer.Serialize(
                    v.AttributeValues.Select(av => av.AttributeValueId).OrderBy(id => id).ToList()))
                .ToList();

            // Carregar SKUs existentes na organização
            var existingSkus = await Context.ProductVariants
                .Where(v => v.OrganizationId == CurrentOrganizationId && v.DeletedAt == null)
                .Select(v => v.Sku)
                .ToListAsync();

            var model = new AddVariantViewModel
            {
                ProductTemplateId = templateId,
                ProductName = template.Name,
                VariantAttributes = variantAttributes,
                ExistingAttributeCombinations = existingCombinations,
                ExistingSkus = existingSkus,
                IsActive = true
            };

            return View(model);
        }

        // POST: /Products/AddVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddVariant(AddVariantViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                await ReloadAddVariantModel(model);
                return View(model);
            }

            // Validar SKU único
            var skuExists = await Context.ProductVariants
                .AnyAsync(v => v.OrganizationId == CurrentOrganizationId &&
                              v.DeletedAt == null &&
                              v.Sku == model.Sku);

            if (skuExists)
            {
                ModelState.AddModelError("Sku", "Já existe uma variante com este SKU nesta organização.");
                await ReloadAddVariantModel(model);
                return View(model);
            }

            // Validar combinação de atributos única
            if (model.SelectedAttributeValues != null && model.SelectedAttributeValues.Any())
            {
                var selectedIds = model.SelectedAttributeValues.OrderBy(id => id).ToList();
                var selectedCombinationJson = System.Text.Json.JsonSerializer.Serialize(selectedIds);

                var existingVariants = await Context.ProductVariants
                    .Include(v => v.AttributeValues)
                    .Where(v => v.ProductTemplateId == model.ProductTemplateId && v.DeletedAt == null)
                    .ToListAsync();

                var existingCombinations = existingVariants
                    .Select(v => System.Text.Json.JsonSerializer.Serialize(
                        v.AttributeValues.Select(av => av.AttributeValueId).OrderBy(id => id).ToList()))
                    .ToList();

                if (existingCombinations.Contains(selectedCombinationJson))
                {
                    // Buscar descrição da combinação duplicada
                    var attributeValueNames = await Context.ProductAttributeValues
                        .Include(pav => pav.Attribute)
                        .Where(pav => selectedIds.Contains(pav.Id))
                        .Select(pav => new { pav.Attribute.Name, pav.Value })
                        .ToListAsync();

                    var combinationDescription = string.Join(", ", attributeValueNames.Select(a => $"{a.Name}: {a.Value}"));
                    ModelState.AddModelError("", $"Já existe uma variante com esta combinação de atributos: {combinationDescription}");
                    await ReloadAddVariantModel(model);
                    return View(model);
                }
            }

            var newVariant = new ProductVariant
            {
                OrganizationId = CurrentOrganizationId,
                ProductTemplateId = model.ProductTemplateId,
                Sku = model.Sku,
                Name = model.Name,
                Description = model.Description,
                Cost = model.Cost,
                Weight = model.Weight,
                Height = model.Height,
                Width = model.Width,
                Length = model.Length,
                Barcode = model.Barcode,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Adicionar atributos de variação selecionados
            if (model.SelectedAttributeValues != null && model.SelectedAttributeValues.Any())
            {
                newVariant.AttributeValues = new List<ProductVariantAttributeValue>();
                foreach (var valueId in model.SelectedAttributeValues)
                {
                    newVariant.AttributeValues.Add(new ProductVariantAttributeValue
                    {
                        VariantId = newVariant.Id,
                        AttributeValueId = valueId
                    });
                }
            }

            try
            {
                Context.ProductVariants.Add(newVariant);
                await Context.SaveChangesAsync();

                Log.Information("PRODUCT_VARIANT_ADDED: Variante {VariantId} (SKU: {Sku}) adicionada ao produto {ProductId} por usuário {UserId}",
                    newVariant.Id, newVariant.Sku, model.ProductTemplateId, CurrentUserId);

                TempData["Success"] = "Variante adicionada com sucesso.";
                return RedirectToAction("ManageVariants", new { id = model.ProductTemplateId });
            }
            catch (DbUpdateException dbEx) when (IsDuplicateSkuError(dbEx))
            {
                Log.Warning(dbEx, "VARIANT_ADD_DUPLICATE_SKU: SKU duplicado ao adicionar variante para produto {ProductId}", model.ProductTemplateId);
                ModelState.AddModelError("Sku", "Já existe uma variante com este SKU. Por favor, utilize um SKU diferente.");
                await ReloadAddVariantModel(model);
                return View(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "VARIANT_ADD_ERROR: Erro ao adicionar variante para produto {ProductId}", model.ProductTemplateId);
                ModelState.AddModelError("", "Ocorreu um erro ao adicionar a variante: " + ex.Message);
                await ReloadAddVariantModel(model);
                return View(model);
            }
        }

        /// <summary>
        /// Recarrega os dados necessários para o formulário de adicionar variante
        /// </summary>
        private async Task ReloadAddVariantModel(AddVariantViewModel model)
        {
            // Carregar variantes existentes do produto
            var existingVariants = await Context.ProductVariants
                .Include(v => v.AttributeValues)
                .Where(v => v.ProductTemplateId == model.ProductTemplateId && v.DeletedAt == null)
                .ToListAsync();

            // Obter os IDs de atributos usados nas variantes existentes
            var usedAttributeValueIds = existingVariants
                .SelectMany(v => v.AttributeValues.Select(av => av.AttributeValueId))
                .Distinct()
                .ToList();

            var usedAttributeIds = await Context.ProductAttributeValues
                .Where(pav => usedAttributeValueIds.Contains(pav.Id))
                .Select(pav => pav.AttributeId)
                .Distinct()
                .ToListAsync();

            // Carregar apenas atributos que estão em uso neste produto
            model.VariantAttributes = await Context.ProductAttributes
                .Include(a => a.Values)
                .Where(a => a.OrganizationId == CurrentOrganizationId && usedAttributeIds.Contains(a.Id))
                .Select(a => new VariantAttributeSelectionViewModel
                {
                    AttributeId = a.Id,
                    AttributeName = a.Name,
                    AttributeCode = a.Code,
                    IsUsedForVariants = true,
                    SelectedValues = a.Values
                        .OrderBy(v => v.SortOrder)
                        .Select(v => new AttributeValueSelectionViewModel
                        {
                            Id = v.Id,
                            Value = v.Value,
                            SortOrder = v.SortOrder,
                            IsSelected = model.SelectedAttributeValues != null && model.SelectedAttributeValues.Contains(v.Id)
                        })
                        .ToList()
                })
                .ToListAsync();

            // Criar lista de combinações de atributos já existentes
            model.ExistingAttributeCombinations = existingVariants
                .Select(v => System.Text.Json.JsonSerializer.Serialize(
                    v.AttributeValues.Select(av => av.AttributeValueId).OrderBy(id => id).ToList()))
                .ToList();

            // Carregar SKUs existentes na organização
            model.ExistingSkus = await Context.ProductVariants
                .Where(v => v.OrganizationId == CurrentOrganizationId && v.DeletedAt == null)
                .Select(v => v.Sku)
                .ToListAsync();
        }

        // Métodos auxiliares
        private async Task LoadAvailableCategories(ProductFormViewModel model)
        {
            var categories = await Context.Categories
                .Where(c => c.OrganizationId == CurrentOrganizationId)
                .OrderBy(c => c.Path)
                .Select(c => new CategorySelectionViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Path = c.Path,
                    ParentId = c.ParentId,
                    IsSelected = model.SelectedCategoryIds.Contains(c.Id)
                })
                .ToListAsync();

            // Calcular nível de hierarquia
            foreach (var cat in categories)
            {
                cat.Level = string.IsNullOrEmpty(cat.Path) ? 0 :
                    cat.Path.Count(ch => ch == '>');
            }

            model.AvailableCategories = categories;
        }

        private async Task LoadAvailableAttributes(ProductFormViewModel model)
        {
            var attributes = await Context.ProductAttributes
                .Include(a => a.Values)
                .Where(a => a.OrganizationId == CurrentOrganizationId)
                .ToListAsync();

            if (model.ProductType == ProductType.Simple)
            {
                // Para produtos simples, não há atributos no template (SPU)
                // Atributos são definidos diretamente nas variantes (SKU)
                model.DescriptiveAttributes = new List<AttributeAssignmentViewModel>();
            }
            else
            {
                // Para produtos configuráveis, apenas atributos de variação
                model.VariantAttributes = attributes
                    .Select(a => new VariantAttributeSelectionViewModel
                    {
                        AttributeId = a.Id,
                        AttributeName = a.Name,
                        AttributeCode = a.Code,
                        IsUsedForVariants = false,
                        SelectedValues = a.Values
                            .OrderBy(v => v.SortOrder)
                            .Select(v => new AttributeValueSelectionViewModel
                            {
                                Id = v.Id,
                                Value = v.Value,
                                SortOrder = v.SortOrder,
                                IsSelected = false
                            })
                            .ToList()
                    })
                    .ToList();

                // Não há mais atributos descritivos no template
                model.DescriptiveAttributes = new List<AttributeAssignmentViewModel>();
            }
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            var clean = sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Trim();
            clean = System.Text.RegularExpressions.Regex.Replace(clean, "[^a-z0-9\\s-]", "");
            clean = System.Text.RegularExpressions.Regex.Replace(clean, "\\s+", "-");
            clean = System.Text.RegularExpressions.Regex.Replace(clean, "-+", "-");
            return clean.Trim('-');
        }

        /// <summary>
        /// Verifica se a exceção é um erro de SKU duplicado (constraint violation)
        /// </summary>
        private static bool IsDuplicateSkuError(DbUpdateException ex)
        {
            var innerException = ex.InnerException?.ToString() ?? ex.ToString();
            return innerException.Contains("ux_product_variant_org_sku") ||
                   innerException.Contains("duplicate key") ||
                   innerException.Contains("23505"); // PostgreSQL unique violation code
        }

        /// <summary>
        /// Encontra SKUs duplicados no modelo (duplicados entre si ou já existentes no banco)
        /// </summary>
        private List<string> FindDuplicateSkus(ProductFormViewModel model)
        {
            var duplicates = new List<string>();

            if (model.Variants == null || !model.Variants.Any())
                return duplicates;

            // Pegar todos os SKUs do modelo
            var modelSkus = model.Variants
                .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
                .Select(v => v.Sku.Trim())
                .ToList();

            // Encontrar duplicados dentro do próprio modelo
            var duplicatesInModel = modelSkus
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            duplicates.AddRange(duplicatesInModel);

            // Verificar SKUs que já existem no banco (excluindo variantes do mesmo produto)
            var existingVariantIds = model.Variants
                .Where(v => v.Id.HasValue && v.Id.Value != Guid.Empty)
                .Select(v => v.Id.Value)
                .ToList();

            var existingSkusInDb = Context.ProductVariants
                .Where(v => v.OrganizationId == CurrentOrganizationId &&
                           v.DeletedAt == null &&
                           !existingVariantIds.Contains(v.Id) &&
                           modelSkus.Contains(v.Sku))
                .Select(v => v.Sku)
                .ToList();

            foreach (var sku in existingSkusInDb)
            {
                if (!duplicates.Any(d => d.Equals(sku, StringComparison.OrdinalIgnoreCase)))
                {
                    duplicates.Add(sku);
                }
            }

            return duplicates;
        }

        /// <summary>
        /// Valida se um SKU já existe no banco de dados (endpoint AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ValidateSku(string sku, Guid? excludeVariantId = null)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                return Json(new { valid = false, message = "Organização não selecionada" });
            }

            if (string.IsNullOrWhiteSpace(sku))
            {
                return Json(new { valid = false, message = "SKU é obrigatório" });
            }

            var query = Context.ProductVariants
                .Where(v => v.OrganizationId == CurrentOrganizationId &&
                           v.DeletedAt == null &&
                           v.Sku == sku.Trim());

            // Excluir a própria variante se estiver editando
            if (excludeVariantId.HasValue && excludeVariantId.Value != Guid.Empty)
            {
                query = query.Where(v => v.Id != excludeVariantId.Value);
            }

            var exists = await query.AnyAsync();

            if (exists)
            {
                return Json(new { valid = false, message = "Este SKU já existe" });
            }

            return Json(new { valid = true, message = "" });
        }

        /// <summary>
        /// Valida múltiplos SKUs de uma vez (endpoint AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ValidateSkus(List<string> skus)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                return Json(new { valid = false, duplicates = new List<string>(), message = "Organização não selecionada" });
            }

            if (skus == null || !skus.Any())
            {
                return Json(new { valid = true, duplicates = new List<string>(), message = "" });
            }

            // Normalizar SKUs
            var normalizedSkus = skus
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();

            // Buscar SKUs que já existem no banco
            var existingSkus = await Context.ProductVariants
                .Where(v => v.OrganizationId == CurrentOrganizationId &&
                           v.DeletedAt == null &&
                           normalizedSkus.Contains(v.Sku))
                .Select(v => v.Sku)
                .ToListAsync();

            if (existingSkus.Any())
            {
                return Json(new {
                    valid = false,
                    duplicates = existingSkus,
                    message = $"Os seguintes SKUs já existem: {string.Join(", ", existingSkus)}"
                });
            }

            return Json(new { valid = true, duplicates = new List<string>(), message = "" });
        }

        /// <summary>
        /// Valida se um nome de produto já existe (endpoint AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ValidateProductName(string name, Guid? excludeProductId = null)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                return Json(new { valid = false, message = "Organização não selecionada" });
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { valid = false, message = "Nome é obrigatório" });
            }

            // Gerar slug para comparação
            var slug = Slugify(name.Trim());

            var query = Context.ProductTemplates
                .Where(p => p.OrganizationId == CurrentOrganizationId &&
                           p.DeletedAt == null &&
                           p.Slug == slug);

            // Excluir o próprio produto se estiver editando
            if (excludeProductId.HasValue && excludeProductId.Value != Guid.Empty)
            {
                query = query.Where(p => p.Id != excludeProductId.Value);
            }

            var exists = await query.AnyAsync();

            if (exists)
            {
                return Json(new { valid = false, message = "Já existe um produto com este nome" });
            }

            return Json(new { valid = true, message = "" });
        }
    }
}
