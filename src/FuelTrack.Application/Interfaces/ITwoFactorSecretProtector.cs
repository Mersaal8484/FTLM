namespace FuelTrack.Application.Interfaces;

public interface ITwoFactorSecretProtector
{
    Task<string> ProtectAsync(int userId, string secret, CancellationToken cancellationToken = default);
    Task<string> UnprotectAsync(int userId, string value, CancellationToken cancellationToken = default);
}
