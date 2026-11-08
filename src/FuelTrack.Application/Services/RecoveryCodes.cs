using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FuelTrack.Application.Services;

/// <summary>High-entropy recovery codes are stored as one-way hashes.</summary>
public static class RecoveryCodes
{
    public static string[] Generate() => Enumerable.Range(0, 10)
        .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()).ToArray();

    public static string Store(IEnumerable<string> codes) => JsonSerializer.Serialize(codes.Select(Hash).ToArray());

    public static string? Consume(string? stored, string? candidate)
    {
        if (string.IsNullOrEmpty(stored) || candidate is null || candidate.Length != 32 ||
            candidate.Any(c => !Uri.IsHexDigit(c))) return null;
        string[]? hashes;
        try { hashes = JsonSerializer.Deserialize<string[]>(stored); }
        catch (JsonException) { return null; }
        if (hashes is null || hashes.Any(h => h is null || h.Length != 64 || h.Any(c => !Uri.IsHexDigit(c)))) return null;
        var hash = Hash(candidate);
        var index = Array.FindIndex(hashes, h => CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(h), Encoding.ASCII.GetBytes(hash)));
        if (index < 0) return null;
        return JsonSerializer.Serialize(hashes.Where((_, i) => i != index).ToArray());
    }

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes("fueltrack:recovery:v1:" + code.ToLowerInvariant()))).ToLowerInvariant();
}
