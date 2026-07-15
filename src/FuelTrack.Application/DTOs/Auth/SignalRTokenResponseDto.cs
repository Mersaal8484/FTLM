namespace FuelTrack.Application.DTOs.Auth;

public class SignalRTokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
