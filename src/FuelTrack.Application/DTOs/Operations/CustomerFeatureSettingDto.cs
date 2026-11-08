namespace FuelTrack.Application.DTOs.Operations;

public sealed record CustomerFeatureSettingDto(string Key, bool IsEnabled, string? Value);
public sealed record UpdateCustomerFeatureSettingDto(bool IsEnabled, string? Value);
