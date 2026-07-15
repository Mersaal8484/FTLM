namespace FuelTrack.Application.Common;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken, string frontendBaseUrl);
    Task<bool> SendWeeklyReportAsync(string toEmail, string userName, WeeklyReportEmailData data);
    Task<bool> SendFuelStockAlertAsync(string toEmail, string userName, FuelStockAlertEmailData data);
}

public class WeeklyReportEmailData
{
    public string ReportPeriod { get; set; } = string.Empty;
    public int TotalBranches { get; set; }
    public int TotalStations { get; set; }
    public int TotalSubStations { get; set; }
    public int TotalGenerators { get; set; }
    public int TotalFuelTanks { get; set; }
    public double TotalFuelConsumedLiters { get; set; }
    public double TotalEnergyProducedKWh { get; set; }
    public double OverallEfficiencyPercent { get; set; }
}

public class FuelStockAlertEmailData
{
    public string TankName { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public double CurrentLevelLiters { get; set; }
    public double CapacityLiters { get; set; }
    public double Percentage => CapacityLiters > 0 ? Math.Round(CurrentLevelLiters / CapacityLiters * 100, 1) : 0;
}
