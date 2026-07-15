
namespace FuelTrack.Domain.Models
{
    public class LoginAttempt : BaseEntity
    {
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public bool IsSuccessful { get; set; }
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public string? FailureReason { get; set; }
        public DateTime AttemptedAt { get; set; }
    }
}
