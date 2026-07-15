namespace FuelTrack.Domain.Models;

public class DailyReadingArchive
{
    public int Id { get; set; }
    public int GeneratorId { get; set; }
    public DateTime ReadingDate { get; set; }
    public double PrevEnergyKWh { get; set; }
    public double CurrentEnergyKWh { get; set; }
    public double PrevRunningHours { get; set; }
    public double CurrentRunningHours { get; set; }
    public int FuelLookupId { get; set; }
    public decimal StandardFuelRate { get; set; }
    public int OilLookupId { get; set; }
    public double OilUsedLiters { get; set; }
    public int ReadingStatusLookupId { get; set; }
    public DateTime ArchivedAt { get; set; }
}
