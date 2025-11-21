using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using WebApplicationBasic.Filters;
using WebApplicationBasic.Models.ViewModels;

namespace WebApplicationBasic.Controllers
{
    [CustomAuthorize(OrganizationRoles = "admin,owner")]
    public class ProductAttributesController : BaseController
    {
        public async Task<ActionResult> Index()
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para gerenciar atributos.";
                return RedirectToAction("Index", "Home");
            }

            var list = await Context.ProductAttributes
                .Include(a => a.Values)
                .Where(a => a.OrganizationId == CurrentOrganizationId)
                .OrderBy(a => a.Name)
                .Select(a => new ProductAttributeListItemViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Code = a.Code,
                    IsVariant = a.IsVariant,
                    ValuesCount = a.Values.Count,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return View(list);
        }

        public ActionResult Create()
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para cadastrar atributos.";
                return RedirectToAction("Index", "Home");
            }

            return View(new ProductAttributeFormViewModel
            {
                IsVariant = true,
                Values = new List<ProductAttributeValueItemViewModel>()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProductAttributeFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para cadastrar atributos.";
                return RedirectToAction("Index", "Home");
            }

            if (Request["addValue"] != null)
            {
                if (model.Values == null)
                    model.Values = new List<ProductAttributeValueItemViewModel>();

                model.Values.Add(new ProductAttributeValueItemViewModel
                {
                    SortOrder = model.Values.Count
                });
                ModelState.Clear();
                return View(model);
            }

            var allValues = model.Values ?? new List<ProductAttributeValueItemViewModel>();
            var nonEmptyValues = allValues
                .Where(v => !string.IsNullOrWhiteSpace(v.Value))
                .ToList();

            if (!nonEmptyValues.Any())
            {
                ModelState.AddModelError("", "Defina pelo menos um valor de atributo.");
            }

            var duplicateGroup = nonEmptyValues
                .GroupBy(v => v.Value.Trim().ToLowerInvariant())
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateGroup != null)
            {
                ModelState.AddModelError("", $"Existem valores duplicados: \"{duplicateGroup.Key}\".");
            }

            if (!ModelState.IsValid)
            {
                model.Values = allValues;
                return View(model);
            }

            var code = Slugify(model.Name);

            var codeExists = await Context.ProductAttributes
                .AnyAsync(a => a.OrganizationId == CurrentOrganizationId && a.Code == code);

            if (codeExists)
            {
                ModelState.AddModelError("Name", "Já existe um atributo com um código gerado a partir deste nome nesta organização.");
                model.Values = allValues;
                return View(model);
            }

            var attribute = new ProductAttribute
            {
                OrganizationId = CurrentOrganizationId,
                Name = model.Name,
                Code = code,
                IsVariant = model.IsVariant,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Values = new List<ProductAttributeValue>()
            };

            var index = 0;
            foreach (var v in nonEmptyValues)
            {
                attribute.Values.Add(new ProductAttributeValue
                {
                    Value = v.Value,
                    SortOrder = v.SortOrder != 0 ? v.SortOrder : index++,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            Context.ProductAttributes.Add(attribute);
            await Context.SaveChangesAsync();

            TempData["Success"] = "Atributo criado com sucesso.";
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Edit(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para editar atributos.";
                return RedirectToAction("Index", "Home");
            }

            var attribute = await Context.ProductAttributes
                .Include(a => a.Values)
                .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == CurrentOrganizationId);

            if (attribute == null)
                return HttpNotFound();

            var model = new ProductAttributeFormViewModel
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Code = attribute.Code,
                IsVariant = attribute.IsVariant,
                Values = attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.Value)
                    .Select(v => new ProductAttributeValueItemViewModel
                    {
                        Id = v.Id,
                        Value = v.Value,
                        SortOrder = v.SortOrder
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ProductAttributeFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para editar atributos.";
                return RedirectToAction("Index", "Home");
            }

            var attribute = await Context.ProductAttributes
                .Include(a => a.Values)
                .FirstOrDefaultAsync(a => a.Id == model.Id && a.OrganizationId == CurrentOrganizationId);

            if (attribute == null)
                return HttpNotFound();

            if (Request["addValue"] != null)
            {
                if (model.Values == null)
                    model.Values = new List<ProductAttributeValueItemViewModel>();

                model.Values.Add(new ProductAttributeValueItemViewModel
                {
                    SortOrder = model.Values.Count
                });
                ModelState.Clear();
                return View(model);
            }

            var allValues = model.Values ?? new List<ProductAttributeValueItemViewModel>();
            var nonEmptyValues = allValues
                .Where(v => !string.IsNullOrWhiteSpace(v.Value))
                .ToList();

            if (!nonEmptyValues.Any())
            {
                ModelState.AddModelError("", "Defina pelo menos um valor de atributo.");
            }

            var duplicateGroup = nonEmptyValues
                .GroupBy(v => v.Value.Trim().ToLowerInvariant())
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateGroup != null)
            {
                ModelState.AddModelError("", $"Existem valores duplicados: \"{duplicateGroup.Key}\".");
            }

            if (!ModelState.IsValid)
            {
                model.Values = allValues;
                return View(model);
            }

            var code = Slugify(model.Name);

            var codeExists = await Context.ProductAttributes
                .AnyAsync(a => a.OrganizationId == CurrentOrganizationId &&
                               a.Code == code &&
                               a.Id != attribute.Id);

            if (codeExists)
            {
                ModelState.AddModelError("Name", "Já existe um atributo com um código gerado a partir deste nome nesta organização.");
                model.Values = allValues;
                return View(model);
            }

            attribute.Name = model.Name;
            attribute.Code = code;
            attribute.IsVariant = model.IsVariant;
            attribute.UpdatedAt = DateTime.UtcNow;

            var existing = attribute.Values.ToDictionary(v => v.Id, v => v);
            var keptIds = new HashSet<Guid>();
            var index = 0;

            foreach (var v in nonEmptyValues)
            {
                var sort = v.SortOrder != 0 ? v.SortOrder : index++;

                if (v.Id.HasValue && existing.TryGetValue(v.Id.Value, out var current))
                {
                    current.Value = v.Value;
                    current.SortOrder = sort;
                    current.UpdatedAt = DateTime.UtcNow;
                    keptIds.Add(current.Id);
                }
                else
                {
                    var newValue = new ProductAttributeValue
                    {
                        AttributeId = attribute.Id,
                        Value = v.Value,
                        SortOrder = sort,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    attribute.Values.Add(newValue);
                }
            }

            var toRemove = attribute.Values
                .Where(v => v.Id != Guid.Empty && !keptIds.Contains(v.Id))
                .ToList();

            if (toRemove.Any())
            {
                Context.ProductAttributeValues.RemoveRange(toRemove);
            }

            await Context.SaveChangesAsync();

            TempData["Success"] = "Atributo atualizado com sucesso.";
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Delete(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para excluir atributos.";
                return RedirectToAction("Index", "Home");
            }

            var attribute = await Context.ProductAttributes
                .Include(a => a.Values)
                .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == CurrentOrganizationId);

            if (attribute == null)
                return HttpNotFound();

            var model = new ProductAttributeListItemViewModel
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Code = attribute.Code,
                IsVariant = attribute.IsVariant,
                ValuesCount = attribute.Values.Count,
                CreatedAt = attribute.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para excluir atributos.";
                return RedirectToAction("Index", "Home");
            }

            var attribute = await Context.ProductAttributes
                .FirstOrDefaultAsync(a => a.Id == id && a.OrganizationId == CurrentOrganizationId);

            if (attribute == null)
                return HttpNotFound();

            Context.ProductAttributes.Remove(attribute);
            await Context.SaveChangesAsync();

            TempData["Success"] = "Atributo excluído com sucesso.";
            return RedirectToAction("Index");
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var builder = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            var clean = builder.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Trim();
            clean = System.Text.RegularExpressions.Regex.Replace(clean, "[^a-z0-9\\s-]", "");
            clean = System.Text.RegularExpressions.Regex.Replace(clean, "\\s+", "-");
            clean = System.Text.RegularExpressions.Regex.Replace(clean, "-+", "-");
            return clean.Trim('-');
        }
    }
}

