using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using WebApplicationBasic.Filters;
using WebApplicationBasic.Models.ViewModels;
using WebApplicationBasic.Services;

namespace WebApplicationBasic.Controllers
{
    [CustomAuthorize(OrganizationRoles = "admin,owner")]
    public class StockController : BaseController
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        // GET: /Stock
        public async Task<ActionResult> Index(string search, Guid? locationId)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao para gerenciar estoque.";
                return RedirectToAction("Index", "Home");
            }

            var model = new StockDashboardViewModel
            {
                Search = search,
                LocationId = locationId
            };

            var locations = await _stockService.GetLocationsAsync(CurrentOrganizationId, includeInactive: false);
            model.Locations = locations
                .Select(l => new StockLocationOption
                {
                    Id = l.Id,
                    Code = l.Code,
                    Name = l.Name,
                    IsDefault = l.IsDefault,
                    IsVirtual = l.IsVirtual,
                    IsActive = l.IsActive
                })
                .ToList();

            var balances = await _stockService.GetBalancesAsync(CurrentOrganizationId, search, locationId);
            model.Balances = balances
                .Select(b => new StockBalanceListItemViewModel
                {
                    VariantId = b.VariantId,
                    LocationId = b.LocationId,
                    Sku = b.Variant?.Sku,
                    VariantName = b.Variant?.Name,
                    ProductName = b.Variant?.ProductTemplate?.Name,
                    LocationName = b.Location?.Name,
                    IsDefaultLocation = b.Location?.IsDefault ?? false,
                    OnHand = b.OnHand,
                    Reserved = b.Reserved,
                    LastMovementAt = b.LastMovementAt
                })
                .ToList();

            var ledger = await _stockService.GetRecentLedgerAsync(CurrentOrganizationId, 20);
            model.RecentLedger = ledger
                .Select(l => new StockLedgerItemViewModel
                {
                    Id = l.Id,
                    Sku = l.Variant?.Sku,
                    VariantName = l.Variant?.Name,
                    ProductName = l.Variant?.ProductTemplate?.Name,
                    LocationName = l.Location?.Name,
                    DeltaOnHand = l.DeltaOnHand,
                    DeltaReserved = l.DeltaReserved,
                    MovementType = (StockMovementType)l.MovementType,
                    Reason = l.Reason,
                    Source = string.IsNullOrWhiteSpace(l.SourceType) ? null : $"{l.SourceType} {l.SourceId}",
                    OccurredAt = l.OccurredAt
                })
                .ToList();

            var reservations = await _stockService.GetReservationsAsync(CurrentOrganizationId, onlyActive: true);
            model.ActiveReservations = reservations
                .Select(r => new StockReservationListItemViewModel
                {
                    Id = r.Id,
                    Sku = r.Variant?.Sku,
                    VariantName = r.Variant?.Name,
                    ProductName = r.Variant?.ProductTemplate?.Name,
                    LocationName = r.Location?.Name,
                    Quantity = r.Quantity,
                    Status = (StockReservationStatus)r.Status,
                    ReservedAt = r.ReservedAt,
                    ExpiresAt = r.ExpiresAt,
                    Source = string.IsNullOrWhiteSpace(r.SourceType) ? null : $"{r.SourceType} {r.SourceId}"
                })
                .ToList();

            ViewBag.Locations = model.Locations;
            ViewBag.Search = search;
            return View(model);
        }

        // GET: /Stock/Locations
        public async Task<ActionResult> Locations()
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao.";
                return RedirectToAction("Index", "Home");
            }

            var locations = await _stockService.GetLocationsAsync(CurrentOrganizationId, includeInactive: true);
            ViewBag.Locations = locations;
            return View(new StockLocationFormViewModel());
        }

        // POST: /Stock/CreateLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateLocation(StockLocationFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Locations = await _stockService.GetLocationsAsync(CurrentOrganizationId, includeInactive: true);
                return View("Locations", model);
            }

            try
            {
                var entity = new StockLocation
                {
                    Id = Guid.Empty,
                    Code = model.Code,
                    Name = model.Name,
                    Description = model.Description,
                    IsVirtual = model.IsVirtual,
                    IsDefault = model.IsDefault,
                    IsActive = model.IsActive
                };

                await _stockService.SaveLocationAsync(CurrentOrganizationId, entity);
                TempData["Success"] = "Local salvo com sucesso.";
                return RedirectToAction("Locations");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Locations = await _stockService.GetLocationsAsync(CurrentOrganizationId, includeInactive: true);
                return View("Locations", model);
            }
        }

        // GET: /Stock/EditLocation/{id}
        public async Task<ActionResult> EditLocation(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao.";
                return RedirectToAction("Index", "Home");
            }

            var location = await _stockService.GetLocationsAsync(CurrentOrganizationId, includeInactive: true);
            var entity = location.FirstOrDefault(l => l.Id == id);
            if (entity == null)
            {
                return HttpNotFound();
            }

            var model = new StockLocationFormViewModel
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Description = entity.Description,
                IsVirtual = entity.IsVirtual,
                IsDefault = entity.IsDefault,
                IsActive = entity.IsActive
            };

            return View(model);
        }

        // POST: /Stock/EditLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditLocation(StockLocationFormViewModel model)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var entity = new StockLocation
                {
                    Id = model.Id ?? Guid.Empty,
                    Code = model.Code,
                    Name = model.Name,
                    Description = model.Description,
                    IsVirtual = model.IsVirtual,
                    IsDefault = model.IsDefault,
                    IsActive = model.IsActive
                };

                await _stockService.SaveLocationAsync(CurrentOrganizationId, entity);
                TempData["Success"] = "Local atualizado com sucesso.";
                return RedirectToAction("Locations");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        // GET: /Stock/Receive
        public async Task<ActionResult> Receive()
        {
            return await LoadMovementForm(StockMovementType.StockReceived, "Registrar entrada de estoque");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Receive(StockMovementFormViewModel model)
        {
            return await HandleMovement(model, async m =>
            {
                var dedup = Guid.NewGuid().ToString();
                var sourceType = "AJUSTE MANUAL";
                var sourceId = Guid.NewGuid().ToString();
                var reason = string.IsNullOrWhiteSpace(m.Reason) ? "Entrada manual" : m.Reason;
                return await _stockService.ReceiveAsync(CurrentOrganizationId, m.VariantId, m.LocationId, m.Quantity, reason, sourceType, sourceId, dedup);
            });
        }

        // GET: /Stock/Adjust
        public async Task<ActionResult> Adjust()
        {
            return await LoadMovementForm(StockMovementType.StockAdjusted, "Ajustar saldo (novo valor)");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Adjust(StockMovementFormViewModel model)
        {
            return await HandleMovement(model, async m =>
            {
                var dedup = Guid.NewGuid().ToString();
                var sourceType = "AJUSTE MANUAL";
                var sourceId = Guid.NewGuid().ToString();
                var reason = string.IsNullOrWhiteSpace(m.Reason) ? "Ajuste manual" : m.Reason;
                return await _stockService.AdjustAsync(CurrentOrganizationId, m.VariantId, m.LocationId, m.Quantity, reason, sourceType, sourceId, dedup);
            }, isAdjust: true);
        }

        // GET: /Stock/Transfer
        public async Task<ActionResult> Transfer()
        {
            return await LoadMovementForm(StockMovementType.StockTransferredOut, "Transferir entre locais");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Transfer(StockMovementFormViewModel model)
        {
            if (!model.TargetLocationId.HasValue)
            {
                ModelState.AddModelError("TargetLocationId", "Selecione o local de destino.");
            }

            return await HandleMovement(model, async m =>
            {
                var dedup = Guid.NewGuid().ToString();
                var sourceType = "AJUSTE MANUAL";
                var sourceId = Guid.NewGuid().ToString();
                var reason = string.IsNullOrWhiteSpace(m.Reason) ? "Transferencia manual" : m.Reason;
                return await _stockService.TransferAsync(CurrentOrganizationId, m.VariantId, m.LocationId, m.TargetLocationId.Value, m.Quantity, reason, sourceType, sourceId, dedup);
            }, requiresTarget: true);
        }

        // GET: /Stock/Reserve
        public async Task<ActionResult> Reserve()
        {
            return await LoadMovementForm(StockMovementType.StockReserved, "Criar reserva de estoque");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Reserve(StockMovementFormViewModel model)
        {
            return await HandleMovement(model, async m =>
            {
                var dedup = Guid.NewGuid().ToString();
                var sourceType = "AJUSTE MANUAL";
                var sourceId = Guid.NewGuid().ToString();
                var reason = string.IsNullOrWhiteSpace(m.Reason) ? "Reserva manual" : m.Reason;
                return await _stockService.ReserveAsync(CurrentOrganizationId, m.VariantId, m.LocationId, m.Quantity, m.ExpiresAt, sourceType, sourceId, dedup);
            });
        }

        // GET: /Stock/Ship
        public async Task<ActionResult> Ship(Guid? reservationId = null)
        {
            var viewResult = await LoadMovementForm(StockMovementType.StockShipped, "Dar baixa (saida)") as ViewResult;
            if (viewResult != null && viewResult.Model is StockMovementFormViewModel model)
            {
                model.ReservationId = reservationId;
            }
            if (viewResult != null)
            {
                return viewResult;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Ship(StockMovementFormViewModel model)
        {
            return await HandleMovement(model, async m =>
            {
                var dedup = Guid.NewGuid().ToString();
                var sourceType = "AJUSTE MANUAL";
                var sourceId = Guid.NewGuid().ToString();
                var reason = string.IsNullOrWhiteSpace(m.Reason) ? "Baixa manual" : m.Reason;
                return await _stockService.ShipAsync(CurrentOrganizationId, m.VariantId, m.LocationId, m.Quantity, m.ReservationId, reason, sourceType, sourceId, dedup);
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReleaseReservation(Guid id)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao.";
                return RedirectToAction("Index");
            }

            try
            {
                await _stockService.ReleaseReservationAsync(CurrentOrganizationId, id, "Liberacao manual", null, null, Guid.NewGuid().ToString());
                TempData["Success"] = "Reserva liberada.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index");
        }

        private async Task<ActionResult> LoadMovementForm(StockMovementType type, string title)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao.";
                return RedirectToAction("Index", "Home");
            }

            var model = new StockMovementFormViewModel
            {
                MovementType = type,
                Quantity = 1
            };

            await PopulateOptions(model);
            ViewBag.Title = title;
            return View("Movement", model);
        }

        private async Task<ActionResult> HandleMovement(StockMovementFormViewModel model, Func<StockMovementFormViewModel, Task<StockMovementResult>> action, bool isAdjust = false, bool requiresTarget = false)
        {
            if (CurrentOrganizationId == Guid.Empty)
            {
                TempData["Error"] = "Selecione uma organizacao.";
                return RedirectToAction("Index", "Home");
            }

            if (requiresTarget && !model.TargetLocationId.HasValue)
            {
                ModelState.AddModelError("TargetLocationId", "Selecione o local de destino.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateOptions(model);
                ViewBag.Title = GetMovementTitle(model.MovementType);
                return View("Movement", model);
            }

            try
            {
                StockMovementResult result;
                if (isAdjust)
                {
                    result = await action(model);
                }
                else
                {
                    result = await action(model);
                }

                TempData["Success"] = result.IsDuplicate ? "Operacao ja havia sido processada (idempotente)." : "Operacao registrada com sucesso.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateOptions(model);
                ViewBag.Title = GetMovementTitle(model.MovementType);
                return View("Movement", model);
            }
        }

        private string GetMovementTitle(StockMovementType type)
        {
            switch (type)
            {
                case StockMovementType.StockReceived: return "Registrar entrada de estoque";
                case StockMovementType.StockAdjusted: return "Ajustar saldo (novo valor)";
                case StockMovementType.StockTransferredOut: return "Transferir entre locais";
                case StockMovementType.StockReserved: return "Criar reserva de estoque";
                case StockMovementType.StockShipped: return "Dar baixa (saida)";
                default: return "Movimentacao de estoque";
            }
        }

        private async Task PopulateOptions(StockMovementFormViewModel model)
        {
            var locations = await _stockService.GetLocationsAsync(CurrentOrganizationId, includeInactive: false);
            model.Locations = locations
                .Select(l => new StockLocationOption
                {
                    Id = l.Id,
                    Code = l.Code,
                    Name = l.Name,
                    IsDefault = l.IsDefault,
                    IsVirtual = l.IsVirtual,
                    IsActive = l.IsActive
                })
                .ToList();

            var variants = await Context.ProductVariants
                .Include(v => v.ProductTemplate)
                .Where(v => v.OrganizationId == CurrentOrganizationId && v.DeletedAt == null && v.IsActive)
                .OrderBy(v => v.Sku)
                .Select(v => new StockVariantOption
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    Name = v.Name,
                    ProductName = v.ProductTemplate.Name
                })
                .ToListAsync();

            model.Variants = variants;
        }
    }
}
