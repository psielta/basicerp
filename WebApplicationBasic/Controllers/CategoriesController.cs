using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using WebApplicationBasic.Filters;
using WebApplicationBasic.Models.ViewModels;

namespace WebApplicationBasic.Controllers
{
    [CustomAuthorize(OrganizationRoles = "admin,owner")]
        public class CategoriesController : BaseController
    {
        // GET: /Categories
        public async Task<ActionResult> Index()
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para gerenciar categorias.";
                return RedirectToAction("Index", "Home");
            }

            var categories = await Context.Categories
                .Include(c => c.Parent)
                .Where(c => c.OrganizationId == CurrentOrganizationId)
                .OrderBy(c => c.Path)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryListItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Path = c.Path,
                    ParentName = c.Parent != null ? c.Parent.Name : null,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return View(categories);
        }

        // GET: /Categories/Create
        public async Task<ActionResult> Create()
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para cadastrar categorias.";
                return RedirectToAction("Index", "Home");
            }

            var model = new CategoryFormViewModel();
            await PopulateParentOptionsAsync(model);
            return View(model);
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CategoryFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para cadastrar categorias.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                await PopulateParentOptionsAsync(model);
                return View(model);
            }

            // Gerar slug automaticamente a partir do nome
            var slug = Slugify(model.Name);

            // Slug único por organização
            var slugExists = await Context.Categories
                .AnyAsync(c => c.OrganizationId == CurrentOrganizationId && c.Slug == slug);

            if (slugExists)
            {
                ModelState.AddModelError("Name", "Já existe uma categoria com um slug gerado a partir deste nome nesta organização.");
                await PopulateParentOptionsAsync(model);
                return View(model);
            }

            // Garantir que o parent é da mesma organização (se informado)
            Category parent = null;
            if (model.ParentId.HasValue)
            {
                parent = await Context.Categories
                    .FirstOrDefaultAsync(c => c.Id == model.ParentId.Value && c.OrganizationId == CurrentOrganizationId);

                if (parent == null)
                {
                    ModelState.AddModelError("ParentId", "Categoria pai inválida.");
                    await PopulateParentOptionsAsync(model);
                    return View(model);
                }
            }

            var category = new Category
            {
                OrganizationId = CurrentOrganizationId,
                Name = model.Name,
                Slug = slug,
                ParentId = parent?.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            category.Path = parent == null
                ? category.Name
                : string.IsNullOrEmpty(parent.Path)
                    ? parent.Name + ">" + category.Name
                    : parent.Path + ">" + category.Name;

            Context.Categories.Add(category);
            await Context.SaveChangesAsync();

            TempData["Success"] = "Categoria criada com sucesso.";
            return RedirectToAction("Index");
        }

        // GET: /Categories/Edit/{id}
        public async Task<ActionResult> Edit(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para editar categorias.";
                return RedirectToAction("Index", "Home");
            }

            var category = await Context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == CurrentOrganizationId);

            if (category == null)
            {
                return HttpNotFound();
            }

            var model = new CategoryFormViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ParentId = category.ParentId
            };

            await PopulateParentOptionsAsync(model, excludeCategoryId: category.Id);
            return View(model);
        }

        // POST: /Categories/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(CategoryFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para editar categorias.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                await PopulateParentOptionsAsync(model, excludeCategoryId: model.Id);
                return View(model);
            }

            var category = await Context.Categories
                .FirstOrDefaultAsync(c => c.Id == model.Id && c.OrganizationId == CurrentOrganizationId);

            if (category == null)
            {
                return HttpNotFound();
            }

            // Gerar slug automaticamente a partir do nome
            var slug = Slugify(model.Name);

            // Slug único por organização (exceto a própria)
            var slugExists = await Context.Categories
                .AnyAsync(c => c.OrganizationId == CurrentOrganizationId &&
                               c.Slug == slug &&
                               c.Id != category.Id);

            if (slugExists)
            {
                ModelState.AddModelError("Name", "Já existe uma categoria com um slug gerado a partir deste nome nesta organização.");
                await PopulateParentOptionsAsync(model, excludeCategoryId: category.Id);
                return View(model);
            }

            // Validar parent
            Category parent = null;
            if (model.ParentId.HasValue)
            {
                if (model.ParentId.Value == category.Id)
                {
                    ModelState.AddModelError("ParentId", "Uma categoria não pode ser pai de si mesma.");
                    await PopulateParentOptionsAsync(model, excludeCategoryId: category.Id);
                    return View(model);
                }

                parent = await Context.Categories
                    .FirstOrDefaultAsync(c => c.Id == model.ParentId.Value && c.OrganizationId == CurrentOrganizationId);

                if (parent == null)
                {
                    ModelState.AddModelError("ParentId", "Categoria pai inválida.");
                    await PopulateParentOptionsAsync(model, excludeCategoryId: category.Id);
                    return View(model);
                }
            }

            category.Name = model.Name;
            category.Slug = slug;
            category.ParentId = parent?.Id;

            category.Path = parent == null
                ? category.Name
                : string.IsNullOrEmpty(parent.Path)
                    ? parent.Name + ">" + category.Name
                    : parent.Path + ">" + category.Name;

            category.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            TempData["Success"] = "Categoria atualizada com sucesso.";
            return RedirectToAction("Index");
        }

        private async Task PopulateParentOptionsAsync(CategoryFormViewModel model, Guid? excludeCategoryId = null)
        {
            if (CurrentOrganizationId == Guid.Empty)
                return;

            var query = Context.Categories
                .Where(c => c.OrganizationId == CurrentOrganizationId);

            if (excludeCategoryId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCategoryId.Value);
            }

            var categories = await query
                .OrderBy(c => c.Path)
                .ThenBy(c => c.Name)
                .Select(c => new ParentCategoryOption
                {
                    Id = c.Id,
                    Name = c.Name,
                    Path = c.Path
                })
                .ToListAsync();

            model.ParentOptions = categories;
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            var clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
            clean = Regex.Replace(clean, "[^a-z0-9\\s-]", "");
            clean = Regex.Replace(clean, "\\s+", "-");
            clean = Regex.Replace(clean, "-+", "-");
            return clean.Trim('-');
        }

        // GET: /Categories/Delete/{id}
        public async Task<ActionResult> Delete(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para excluir categorias.";
                return RedirectToAction("Index", "Home");
            }

            var category = await Context.Categories
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == CurrentOrganizationId);

            if (category == null)
            {
                return HttpNotFound();
            }

            var model = new CategoryListItemViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Path = category.Path,
                ParentName = category.Parent?.Name,
                CreatedAt = category.CreatedAt
            };

            return View(model);
        }

        // POST: /Categories/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organização para excluir categorias.";
                return RedirectToAction("Index", "Home");
            }

            var category = await Context.Categories
                .FirstOrDefaultAsync(c => c.Id == id && c.OrganizationId == CurrentOrganizationId);

            if (category == null)
            {
                return HttpNotFound();
            }

            Context.Categories.Remove(category);
            await Context.SaveChangesAsync();

            TempData["Success"] = "Categoria excluída com sucesso.";
            return RedirectToAction("Index");
        }
    }
}
