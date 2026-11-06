using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Models
{
    public class FuelTank : BaseEntity
    {
        public int SubStationId { get; set; }
        public int FuelLookupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public double CapacityLiters { get; set; }
        public double? MinAlertLiters { get; set; }
        public double? DeadStockLiters { get; set; }
        public double? ActualStockLiters { get; set; }
        public string? Notes { get; set; }

        // Calibration is additive. Historical observations retain an immutable
        // snapshot and are never recalculated if the setup changes later.
        public TankShape? Shape { get; set; }
        public decimal? DiameterCm { get; set; }
        public decimal? CylinderHeightCm { get; set; }
        public decimal? ConeHeightCm { get; set; }
        public decimal? MultiplicationFactor { get; set; }
        public decimal? ComputedCapacityLitres { get; set; }

        public SubStation SubStation { get; set; } = null!;
        public Lookup FuelLookup { get; set; } = null!;
        public ICollection<TankLevelReading> LevelReadings { get; set; } = new List<TankLevelReading>();
    }
}
