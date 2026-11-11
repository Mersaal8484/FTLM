using System.Text.RegularExpressions;

namespace FuelTrack.Domain.Services;

/// <summary>
/// Normalizes generator model names for consistent lookup across service interval definitions.
/// Collapses repeated whitespace, trims ends, and converts to uppercase invariant.
/// </summary>
public static class GeneratorModelKey
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return string.Empty;
        }

        var trimmed = model.Trim();
        var collapsed = WhitespaceRegex.Replace(trimmed, " ");
        return collapsed.ToUpperInvariant();
    }
}
