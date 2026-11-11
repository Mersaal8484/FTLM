namespace FuelTrack.Domain.Enums;

public enum ServiceDueStatus
{
    NotBaselined = 0,
    Ok = 1,
    DueSoon = 2,
    Due = 3,
    Overdue = 4,
    NotConfigured = 5
}
