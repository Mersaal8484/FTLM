namespace FuelTrack.Domain.MultiTenancy;

/// <summary>Rules for identifiers generated and resolved by the platform.</summary>
public static class TenantIdentity
{
    public static string SchemaName(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A tenant ID is required.", nameof(tenantId));
        return $"tenant_{tenantId:N}";
    }

    /// <summary>
    /// Normalizes a registered DNS host name, not an arbitrary URL or forwarded header.
    /// Callers must obtain the host through trusted proxy configuration and perform an
    /// exact lookup against the platform registry before using the tenant.
    /// </summary>
    public static string NormalizeHost(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized.Length > 253 || normalized.Contains(':') || normalized.Contains('/') ||
            normalized.Contains('*') || Uri.CheckHostName(normalized) != UriHostNameType.Dns)
            throw new ArgumentException("A valid DNS hostname is required.", nameof(host));
        return normalized;
    }
}
