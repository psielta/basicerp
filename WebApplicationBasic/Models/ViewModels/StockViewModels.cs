using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EntityFrameworkProject.Models;

namespace WebApplicationBasic.Models.ViewModels
{
    public class StockLocationOption
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsActive { get; set; }
        public string Display => string.IsNullOrWhiteSpace(Code) ? Name : $"{Code} - {Name}";
    }

    public class StockLocationFormViewModel
    {
        public Guid? Id { get; set; }

        [Required]
        [Display(Name = "Codigo")]
        public string Code { get; set; }

        [Required]
        [Display(Name = "Nome")]
        public string Name { get; set; }

        [Display(Name = "Descricao")]
        public string Description { get; set; }

        [Display(Name = "Local virtual")]
        public bool IsVirtual { get; set; }

        [Display(Name = "Padrao da organizacao")]
        public bool IsDefault { get; set; }

        [Display(Name = "Ativo")]
        public bool IsActive { get; set; } = true;
    }

    public class StockBalanceListItemViewModel
    {
        public Guid VariantId { get; set; }
        public Guid LocationId { get; set; }
        public string Sku { get; set; }
        public string VariantName { get; set; }
        public string ProductName { get; set; }
        public string LocationName { get; set; }
        public bool IsDefaultLocation { get; set; }
        public decimal OnHand { get; set; }
        public decimal Reserved { get; set; }
        public decimal Available => OnHand - Reserved;
        public DateTime LastMovementAt { get; set; }
    }

    public class StockDashboardViewModel
    {
        public string Search { get; set; }
        public Guid? LocationId { get; set; }
        public List<StockLocationOption> Locations { get; set; } = new List<StockLocationOption>();
        public List<StockBalanceListItemViewModel> Balances { get; set; } = new List<StockBalanceListItemViewModel>();
        public List<StockLedgerItemViewModel> RecentLedger { get; set; } = new List<StockLedgerItemViewModel>();
        public List<StockReservationListItemViewModel> ActiveReservations { get; set; } = new List<StockReservationListItemViewModel>();
    }

    public class StockLedgerItemViewModel
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public string VariantName { get; set; }
        public string ProductName { get; set; }
        public string LocationName { get; set; }
        public decimal DeltaOnHand { get; set; }
        public decimal DeltaReserved { get; set; }
        public StockMovementType MovementType { get; set; }
        public string Reason { get; set; }
        public string Source { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    public class StockReservationListItemViewModel
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public string VariantName { get; set; }
        public string ProductName { get; set; }
        public string LocationName { get; set; }
        public decimal Quantity { get; set; }
        public StockReservationStatus Status { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Source { get; set; }
    }

    public class StockVariantOption
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public string Name { get; set; }
        public string ProductName { get; set; }
        public string Display => string.IsNullOrWhiteSpace(Name)
            ? $"{Sku} - {ProductName}"
            : $"{Sku} - {Name}";
    }

    public class StockMovementFormViewModel
    {
        [Required]
        [Display(Name = "SKU")]
        public Guid VariantId { get; set; }

        [Required]
        [Display(Name = "Local")]
        public Guid LocationId { get; set; }

        [Display(Name = "Local de destino")]
        public Guid? TargetLocationId { get; set; }

        [Required]
        // Range usa cultura atual (pt-BR), por isso valores com vírgula
        [Range(typeof(decimal), "0,0001", "999999999")]
        [Display(Name = "Quantidade")]
        public decimal Quantity { get; set; }

        [Display(Name = "Reserva (opcional)")]
        public Guid? ReservationId { get; set; }

        [Display(Name = "Motivo")]
        public string Reason { get; set; }

        [Display(Name = "Fonte")]
        public string SourceType { get; set; }

        [Display(Name = "Identificador da fonte")]
        public string SourceId { get; set; }

        [Display(Name = "Validade da reserva")]
        public DateTime? ExpiresAt { get; set; }

        public StockMovementType MovementType { get; set; }

        public IList<StockLocationOption> Locations { get; set; } = new List<StockLocationOption>();
        public IList<StockVariantOption> Variants { get; set; } = new List<StockVariantOption>();
    }
}
