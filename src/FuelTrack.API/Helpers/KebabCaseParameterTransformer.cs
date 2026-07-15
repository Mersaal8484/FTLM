using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace FuelTrack.API.Helpers;

/// <summary>
/// Transforms controller/action names from PascalCase to kebab-case.
/// E.g. SubStations → sub-stations, FuelTanks → fuel-tanks
/// </summary>
public class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is null) return null;
        return Regex.Replace(value.ToString()!, "([a-z])([A-Z])", "$1-$2").ToLowerInvariant();
    }
}
