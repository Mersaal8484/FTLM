
namespace FuelTrack.Domain.Models
{
    public class Controller : BaseEntity
    {
        public int? GeneratorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string ControllerType { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public int? Port { get; set; }
        public int? ModbusUnitId { get; set; }
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? ConnectionStatus { get; set; }
        public double EnergyReading { get; set; }
        public double OperatingHours { get; set; }
        public string? RegisterType { get; set; }
        public bool AutoConnect { get; set; }
        public string? EnergyUnit { get; set; }

        public Generator? Generator { get; set; }
    }
}
