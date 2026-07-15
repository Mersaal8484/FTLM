using FuelTrack.Application.DTOs.Auth;

namespace FuelTrack.Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponseDto> LoginAsync(string userName, string password, string? ipAddress);
    Task<TokenResponseDto> RefreshAsync();
    Task RevokeTokensAsync(int userId);
    Task<SignalRTokenResponseDto> GenerateSignalRTokenAsync(int userId);
}
