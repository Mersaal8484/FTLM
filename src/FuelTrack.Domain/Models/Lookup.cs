using System;

namespace FuelTrack.Domain.Models
{
    public class Lookup
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = string.Empty; // e.g., "Oil", "Fuel", "Screen", "GeneratorModel"
        public string Code { get; set; } = string.Empty; // short code, e.g., DIESEL, LCD_42IN
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }
    }
}