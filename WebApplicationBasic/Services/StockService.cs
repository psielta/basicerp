using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EntityFrameworkProject.Data;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplicationBasic.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;

        public StockService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<StockLocation>> GetLocationsAsync(Guid organizationId, bool includeInactive = false)
        {
            var query = _context.StockLocations
                .Where(l => l.OrganizationId == organizationId);

            if (!includeInactive)
            {
                query = query.Where(l => l.IsActive);
            }

            return await query
                .OrderByDescending(l => l.IsDefault)
                .ThenBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<StockLocation> SaveLocationAsync(Guid organizationId, StockLocation location)
        {
            if (string.IsNullOrWhiteSpace(location.Code))
                throw new InvalidOperationException("O campo Codigo e obrigatorio.");
            if (string.IsNullOrWhiteSpace(location.Name))
                throw new InvalidOperationException("O campo Nome e obrigatorio.");

            var normalizedCode = location.Code.Trim();
            var normalizedName = location.Name.Trim();

            var existingWithCode = await _context.StockLocations
                .FirstOrDefaultAsync(l => l.OrganizationId == organizationId && l.Code == normalizedCode && l.Id != location.Id);

            if (existingWithCode != null)
                throw new InvalidOperationException("Ja existe um local com este codigo nesta organizacao.");

            var now = DateTime.UtcNow;
            StockLocation entity;

            if (location.Id == Guid.Empty)
            {
                entity = new StockLocation
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    Code = normalizedCode,
                    Name = normalizedName,
                    Description = location.Description,
                    IsVirtual = location.IsVirtual,
                    IsDefault = location.IsDefault,
                    IsActive = location.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.StockLocations.Add(entity);
            }
            else
            {
                entity = await _context.StockLocations
                    .FirstOrDefaultAsync(l => l.Id == location.Id && l.OrganizationId == organizationId);

                if (entity == null)
                    throw new InvalidOperationException("Local de estoque nao encontrado.");

                entity.Code = normalizedCode;
                entity.Name = normalizedName;
                entity.Description = location.Description;
                entity.IsVirtual = location.IsVirtual;
                entity.IsDefault = location.IsDefault;
                entity.IsActive = location.IsActive;
                entity.UpdatedAt = now;
            }

            if (entity.IsDefault)
            {
                // Desmarcar outros locais padrao
                var others = _context.StockLocations.Where(l => l.OrganizationId == organizationId && l.Id != entity.Id && l.IsDefault);
                await others.ForEachAsync(l => l.IsDefault = false);
            }

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<List<StockBalance>> GetBalancesAsync(Guid organizationId, string search = null, Guid? locationId = null)
        {
            var query = _context.StockBalances
                .Include(b => b.Variant)
                    .ThenInclude(v => v.ProductTemplate)
                .Include(b => b.Location)
                .Where(b => b.OrganizationId == organizationId);

            if (locationId.HasValue)
            {
                query = query.Where(b => b.LocationId == locationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(b =>
                    b.Variant.Sku.Contains(term) ||
                    b.Variant.Name.Contains(term) ||
                    b.Variant.ProductTemplate.Name.Contains(term));
            }

            return await query
                .OrderBy(b => b.Variant.Sku)
                .ThenBy(b => b.Location.Name)
                .ToListAsync();
        }

        public async Task<List<StockLedger>> GetRecentLedgerAsync(Guid organizationId, int take = 50)
        {
            return await _context.StockLedgers
                .Include(l => l.Location)
                .Include(l => l.Variant)
                    .ThenInclude(v => v.ProductTemplate)
                .Where(l => l.OrganizationId == organizationId)
                .OrderByDescending(l => l.OccurredAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<StockReservation>> GetReservationsAsync(Guid organizationId, bool onlyActive = true)
        {
            var query = _context.StockReservations
                .Include(r => r.Location)
                .Include(r => r.Variant)
                    .ThenInclude(v => v.ProductTemplate)
                .Where(r => r.OrganizationId == organizationId);

            if (onlyActive)
            {
                query = query.Where(r => r.Status == (short)StockReservationStatus.Active);
            }

            return await query
                .OrderByDescending(r => r.ReservedAt)
                .ToListAsync();
        }

        public async Task<StockMovementResult> ReceiveAsync(Guid organizationId, Guid variantId, Guid locationId, decimal quantity, string reason, string sourceType, string sourceId, string deduplicationKey)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("A quantidade deve ser maior que zero.");

            deduplicationKey = NormalizeDedup(deduplicationKey);
            await EnsureVariantAndLocationBelongToOrg(organizationId, variantId, locationId);

            var existing = await FindExistingLedger(organizationId, deduplicationKey);
            if (existing != null)
            {
                var existingBalance = await GetBalanceAsync(organizationId, locationId, variantId);
                return StockMovementResult.Duplicated(existing, existingBalance);
            }

            var now = DateTime.UtcNow;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var balance = await GetOrCreateBalance(organizationId, locationId, variantId, now);

                balance.OnHand += quantity;
                balance.LastMovementAt = now;
                balance.UpdatedAt = now;

                var ledger = new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = locationId,
                    VariantId = variantId,
                    DeltaOnHand = quantity,
                    DeltaReserved = 0,
                    MovementType = (short)StockMovementType.StockReceived,
                    Reason = reason,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    OccurredAt = now,
                    CreatedAt = now,
                    Metadata = "{}"
                };

                _context.StockLedgers.Add(ledger);
                AddOutboxEvent(organizationId, "stock_balance", balance.VariantId, "StockReceived", new
                {
                    variantId,
                    locationId,
                    quantity,
                    occurredAt = now
                }, now);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StockMovementResult.Success(balance, ledger);
            }
        }

        public async Task<StockMovementResult> AdjustAsync(Guid organizationId, Guid variantId, Guid locationId, decimal newOnHand, string reason, string sourceType, string sourceId, string deduplicationKey)
        {
            if (newOnHand < 0)
                throw new InvalidOperationException("O saldo ajustado nao pode ser negativo.");

            deduplicationKey = NormalizeDedup(deduplicationKey);
            await EnsureVariantAndLocationBelongToOrg(organizationId, variantId, locationId);

            var existing = await FindExistingLedger(organizationId, deduplicationKey);
            if (existing != null)
            {
                var existingBalance = await GetBalanceAsync(organizationId, locationId, variantId);
                return StockMovementResult.Duplicated(existing, existingBalance);
            }

            var now = DateTime.UtcNow;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var balance = await GetOrCreateBalance(organizationId, locationId, variantId, now);

                var delta = newOnHand - balance.OnHand;
                if (balance.Reserved > newOnHand)
                {
                    throw new InvalidOperationException("Nao e possivel ajustar para um valor menor que o estoque reservado.");
                }

                if (delta == 0)
                {
                    return StockMovementResult.Success(balance, null);
                }

                balance.OnHand = newOnHand;
                balance.LastMovementAt = now;
                balance.UpdatedAt = now;

                var ledger = new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = locationId,
                    VariantId = variantId,
                    DeltaOnHand = delta,
                    DeltaReserved = 0,
                    MovementType = (short)StockMovementType.StockAdjusted,
                    Reason = reason,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    OccurredAt = now,
                    CreatedAt = now,
                    Metadata = "{}"
                };

                _context.StockLedgers.Add(ledger);
                AddOutboxEvent(organizationId, "stock_balance", balance.VariantId, "StockAdjusted", new
                {
                    variantId,
                    locationId,
                    delta,
                    newOnHand,
                    occurredAt = now,
                    reason
                }, now);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StockMovementResult.Success(balance, ledger);
            }
        }

        public async Task<StockMovementResult> TransferAsync(Guid organizationId, Guid variantId, Guid originLocationId, Guid destinationLocationId, decimal quantity, string reason, string sourceType, string sourceId, string deduplicationKey)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("A quantidade deve ser maior que zero.");
            if (originLocationId == destinationLocationId)
                throw new InvalidOperationException("Selecione locais diferentes para transferencia.");

            deduplicationKey = NormalizeDedup(deduplicationKey);
            await EnsureVariantAndLocationBelongToOrg(organizationId, variantId, originLocationId);
            await EnsureVariantAndLocationBelongToOrg(organizationId, variantId, destinationLocationId);

            var existing = await FindExistingLedger(organizationId, deduplicationKey);
            if (existing != null)
            {
                var existingBalance = await GetBalanceAsync(organizationId, destinationLocationId, variantId);
                return StockMovementResult.Duplicated(existing, existingBalance);
            }

            var now = DateTime.UtcNow;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var originBalance = await GetOrCreateBalance(organizationId, originLocationId, variantId, now);
                var destinationBalance = await GetOrCreateBalance(organizationId, destinationLocationId, variantId, now);

                var available = originBalance.OnHand - originBalance.Reserved;
                if (available < quantity)
                    throw new InvalidOperationException("Saldo insuficiente no local de origem.");

                originBalance.OnHand -= quantity;
                originBalance.LastMovementAt = now;
                originBalance.UpdatedAt = now;

                destinationBalance.OnHand += quantity;
                destinationBalance.LastMovementAt = now;
                destinationBalance.UpdatedAt = now;

                var ledgerOut = new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = originLocationId,
                    VariantId = variantId,
                    DeltaOnHand = -quantity,
                    DeltaReserved = 0,
                    MovementType = (short)StockMovementType.StockTransferredOut,
                    Reason = reason,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    OccurredAt = now,
                    CreatedAt = now,
                    Metadata = "{}"
                };

                var ledgerIn = new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = destinationLocationId,
                    VariantId = variantId,
                    DeltaOnHand = quantity,
                    DeltaReserved = 0,
                    MovementType = (short)StockMovementType.StockTransferredIn,
                    Reason = reason,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    OccurredAt = now,
                    CreatedAt = now,
                    Metadata = "{}"
                };

                _context.StockLedgers.AddRange(ledgerOut, ledgerIn);

                AddOutboxEvent(organizationId, "stock_balance", destinationBalance.VariantId, "StockTransferred", new
                {
                    variantId,
                    originLocationId,
                    destinationLocationId,
                    quantity,
                    occurredAt = now,
                    reason
                }, now);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StockMovementResult.Success(destinationBalance, ledgerIn, ledgerOut);
            }
        }

        public async Task<StockMovementResult> ReserveAsync(Guid organizationId, Guid variantId, Guid locationId, decimal quantity, DateTime? expiresAt, string sourceType, string sourceId, string deduplicationKey)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("A quantidade deve ser maior que zero.");

            deduplicationKey = NormalizeDedup(deduplicationKey);
            await EnsureVariantAndLocationBelongToOrg(organizationId, variantId, locationId);

            var existingReservation = await _context.StockReservations
                .FirstOrDefaultAsync(r => r.OrganizationId == organizationId && r.DeduplicationKey == deduplicationKey);
            if (existingReservation != null)
            {
                var existingBalance = await GetBalanceAsync(organizationId, locationId, variantId);
                return StockMovementResult.Duplicated(null, existingBalance, existingReservation);
            }

            var existingLedger = await FindExistingLedger(organizationId, deduplicationKey);
            if (existingLedger != null)
            {
                var existingBalance = await GetBalanceAsync(organizationId, locationId, variantId);
                return StockMovementResult.Duplicated(existingLedger, existingBalance);
            }

            var now = DateTime.UtcNow;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var balance = await GetOrCreateBalance(organizationId, locationId, variantId, now);

                var available = balance.OnHand - balance.Reserved;
                if (available < quantity)
                    throw new InvalidOperationException("Saldo disponivel insuficiente para reservar.");

                balance.Reserved += quantity;
                balance.LastMovementAt = now;
                balance.UpdatedAt = now;

                var reservation = new StockReservation
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = locationId,
                    VariantId = variantId,
                    Quantity = quantity,
                    Status = (short)StockReservationStatus.Active,
                    ReservedAt = now,
                    ExpiresAt = expiresAt,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Metadata = "{}"
                };

                var ledger = new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = locationId,
                    VariantId = variantId,
                    DeltaOnHand = 0,
                    DeltaReserved = quantity,
                    MovementType = (short)StockMovementType.StockReserved,
                    Reason = "Reserva",
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    OccurredAt = now,
                    CreatedAt = now,
                    Metadata = "{}"
                };

                _context.StockReservations.Add(reservation);
                _context.StockLedgers.Add(ledger);

                AddOutboxEvent(organizationId, "stock_reservation", reservation.Id, "StockReserved", new
                {
                    reservationId = reservation.Id,
                    variantId,
                    locationId,
                    quantity,
                    expiresAt,
                    occurredAt = now,
                    sourceType,
                    sourceId
                }, now);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StockMovementResult.Success(balance, ledger, reservation: reservation);
            }
        }

        public async Task<StockMovementResult> ReleaseReservationAsync(Guid organizationId, Guid reservationId, string reason, string sourceType, string sourceId, string deduplicationKey)
        {
            deduplicationKey = NormalizeDedup(deduplicationKey);
            var reservation = await _context.StockReservations
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.OrganizationId == organizationId);

            if (reservation == null)
                throw new InvalidOperationException("Reserva nao encontrada.");

            if (reservation.Status != (short)StockReservationStatus.Active)
                throw new InvalidOperationException("Somente reservas ativas podem ser liberadas.");

            var existing = await FindExistingLedger(organizationId, deduplicationKey);
            if (existing != null)
            {
                var existingBalance = await GetBalanceAsync(organizationId, reservation.LocationId, reservation.VariantId);
                return StockMovementResult.Duplicated(existing, existingBalance, reservation);
            }

            var now = DateTime.UtcNow;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var balance = await GetOrCreateBalance(organizationId, reservation.LocationId, reservation.VariantId, now);

                balance.Reserved -= reservation.Quantity;
                balance.LastMovementAt = now;
                balance.UpdatedAt = now;

                reservation.Status = (short)StockReservationStatus.Cancelled;
                reservation.UpdatedAt = now;

                var ledger = new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = reservation.LocationId,
                    VariantId = reservation.VariantId,
                    DeltaOnHand = 0,
                    DeltaReserved = -reservation.Quantity,
                    MovementType = (short)StockMovementType.StockReleased,
                    Reason = reason,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    OccurredAt = now,
                    CreatedAt = now,
                    Metadata = "{}"
                };

                _context.StockLedgers.Add(ledger);

                AddOutboxEvent(organizationId, "stock_reservation", reservation.Id, "StockReleased", new
                {
                    reservationId,
                    reservation.VariantId,
                    reservation.LocationId,
                    quantity = reservation.Quantity,
                    occurredAt = now,
                    reason
                }, now);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StockMovementResult.Success(balance, ledger, reservation: reservation);
            }
        }

        public async Task<StockMovementResult> ShipAsync(Guid organizationId, Guid variantId, Guid locationId, decimal quantity, Guid? reservationId, string reason, string sourceType, string sourceId, string deduplicationKey)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("A quantidade deve ser maior que zero.");

            deduplicationKey = NormalizeDedup(deduplicationKey);
            await EnsureVariantAndLocationBelongToOrg(organizationId, variantId, locationId);

            var existing = await FindExistingLedger(organizationId, deduplicationKey);
            if (existing != null)
            {
                var existingBalance = await GetBalanceAsync(organizationId, locationId, variantId);
                return StockMovementResult.Duplicated(existing, existingBalance);
            }

            var now = DateTime.UtcNow;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var balance = await GetOrCreateBalance(organizationId, locationId, variantId, now);

                StockReservation reservation = null;
                decimal deltaReserved = 0;

                if (reservationId.HasValue)
                {
                    reservation = await _context.StockReservations
                        .FirstOrDefaultAsync(r => r.Id == reservationId.Value && r.OrganizationId == organizationId);

                    if (reservation == null)
                        throw new InvalidOperationException("Reserva informada nao foi encontrada.");
                    if (reservation.Status != (short)StockReservationStatus.Active)
                        throw new InvalidOperationException("A reserva informada nao esta ativa.");
                    if (reservation.Quantity < quantity)
                        throw new InvalidOperationException("A quantidade enviada excede a quantidade reservada.");

                    reservation.Status = (short)StockReservationStatus.Consumed;
                    reservation.UpdatedAt = now;
                    deltaReserved = -quantity;
                }

                var available = balance.OnHand - balance.Reserved + (deltaReserved * -1); // se consumir reserva, o disponivel aumenta
                if (available < quantity)
                    throw new InvalidOperationException("Saldo disponivel insuficiente.");

                balance.OnHand -= quantity;
                balance.Reserved += deltaReserved;
                balance.LastMovementAt = now;
                balance.UpdatedAt = now;

                var ledger = new StockLedger
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationId = locationId,
                    VariantId = variantId,
                    DeltaOnHand = -quantity,
                    DeltaReserved = deltaReserved,
                    MovementType = (short)StockMovementType.StockShipped,
                    Reason = reason,
                    SourceType = sourceType,
                    SourceId = sourceId,
                    DeduplicationKey = deduplicationKey,
                    OccurredAt = now,
                    CreatedAt = now,
                    Metadata = "{}"
                };

                _context.StockLedgers.Add(ledger);

                AddOutboxEvent(organizationId, "stock_balance", variantId, "StockShipped", new
                {
                    variantId,
                    locationId,
                    quantity,
                    reservationId,
                    occurredAt = now,
                    reason
                }, now);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StockMovementResult.Success(balance, ledger, reservation: reservation);
            }
        }

        private async Task<StockBalance> GetOrCreateBalance(Guid organizationId, Guid locationId, Guid variantId, DateTime now)
        {
            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.OrganizationId == organizationId && b.LocationId == locationId && b.VariantId == variantId);

            if (balance == null)
            {
                balance = new StockBalance
                {
                    OrganizationId = organizationId,
                    LocationId = locationId,
                    VariantId = variantId,
                    OnHand = 0,
                    Reserved = 0,
                    LastMovementAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.StockBalances.Add(balance);
            }

            return balance;
        }

        private async Task<StockBalance> GetBalanceAsync(Guid organizationId, Guid locationId, Guid variantId)
        {
            return await _context.StockBalances
                .FirstOrDefaultAsync(b => b.OrganizationId == organizationId && b.LocationId == locationId && b.VariantId == variantId);
        }

        private async Task EnsureVariantAndLocationBelongToOrg(Guid organizationId, Guid variantId, Guid locationId)
        {
            var variantOrgId = await _context.ProductVariants
                .Where(v => v.Id == variantId)
                .Select(v => v.OrganizationId)
                .FirstOrDefaultAsync();

            if (variantOrgId == Guid.Empty)
                throw new InvalidOperationException("Variante nao encontrada.");
            if (variantOrgId != organizationId)
                throw new InvalidOperationException("A variante nao pertence a organizacao selecionada.");

            var locationOrgId = await _context.StockLocations
                .Where(l => l.Id == locationId)
                .Select(l => l.OrganizationId)
                .FirstOrDefaultAsync();

            if (locationOrgId == Guid.Empty)
                throw new InvalidOperationException("Local de estoque nao encontrado.");
            if (locationOrgId != organizationId)
                throw new InvalidOperationException("O local de estoque nao pertence a organizacao selecionada.");
        }

        private async Task<StockLedger> FindExistingLedger(Guid organizationId, string deduplicationKey)
        {
            if (string.IsNullOrWhiteSpace(deduplicationKey))
                return null;

            return await _context.StockLedgers
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.OrganizationId == organizationId && l.DeduplicationKey == deduplicationKey);
        }

        private void AddOutboxEvent(Guid? organizationId, string aggregateType, Guid? aggregateId, string eventType, object payload, DateTime occurredAt)
        {
            var outbox = new StockOutboxEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                EventType = eventType,
                Payload = JsonSerializer.Serialize(payload),
                Topic = null,
                Key = null,
                OccurredAt = occurredAt,
                CreatedAt = DateTime.UtcNow,
                Status = 0,
                TraceId = null
            };

            _context.StockOutboxEvents.Add(outbox);
        }

        private static string NormalizeDedup(string deduplicationKey)
        {
            return string.IsNullOrWhiteSpace(deduplicationKey)
                ? $"auto:{Guid.NewGuid()}"
                : deduplicationKey.Trim();
        }
    }

    public interface IStockService
    {
        Task<List<StockLocation>> GetLocationsAsync(Guid organizationId, bool includeInactive = false);
        Task<StockLocation> SaveLocationAsync(Guid organizationId, StockLocation location);
        Task<List<StockBalance>> GetBalancesAsync(Guid organizationId, string search = null, Guid? locationId = null);
        Task<List<StockLedger>> GetRecentLedgerAsync(Guid organizationId, int take = 50);
        Task<List<StockReservation>> GetReservationsAsync(Guid organizationId, bool onlyActive = true);
        Task<StockMovementResult> ReceiveAsync(Guid organizationId, Guid variantId, Guid locationId, decimal quantity, string reason, string sourceType, string sourceId, string deduplicationKey);
        Task<StockMovementResult> AdjustAsync(Guid organizationId, Guid variantId, Guid locationId, decimal newOnHand, string reason, string sourceType, string sourceId, string deduplicationKey);
        Task<StockMovementResult> TransferAsync(Guid organizationId, Guid variantId, Guid originLocationId, Guid destinationLocationId, decimal quantity, string reason, string sourceType, string sourceId, string deduplicationKey);
        Task<StockMovementResult> ReserveAsync(Guid organizationId, Guid variantId, Guid locationId, decimal quantity, DateTime? expiresAt, string sourceType, string sourceId, string deduplicationKey);
        Task<StockMovementResult> ReleaseReservationAsync(Guid organizationId, Guid reservationId, string reason, string sourceType, string sourceId, string deduplicationKey);
        Task<StockMovementResult> ShipAsync(Guid organizationId, Guid variantId, Guid locationId, decimal quantity, Guid? reservationId, string reason, string sourceType, string sourceId, string deduplicationKey);
    }

    public class StockMovementResult
    {
        private StockMovementResult()
        {
        }

        public StockBalance Balance { get; private set; }
        public StockLedger Ledger { get; private set; }
        public StockLedger SecondaryLedger { get; private set; }
        public StockReservation Reservation { get; private set; }
        public bool IsDuplicate { get; private set; }

        public static StockMovementResult Success(StockBalance balance, StockLedger ledger, StockLedger secondaryLedger = null, StockReservation reservation = null)
        {
            return new StockMovementResult
            {
                Balance = balance,
                Ledger = ledger,
                SecondaryLedger = secondaryLedger,
                Reservation = reservation,
                IsDuplicate = false
            };
        }

        public static StockMovementResult Duplicated(StockLedger ledger, StockBalance balance, StockReservation reservation = null)
        {
            return new StockMovementResult
            {
                Ledger = ledger,
                Balance = balance,
                Reservation = reservation,
                IsDuplicate = true
            };
        }
    }
}
