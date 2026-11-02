using System;
using System.Collections.Generic;

namespace FuelTrack.Application.DTOs.Compliance
{
    public class UserDataExportDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<UserScopeExportDto> Scopes { get; set; } = new();
        public List<UserAuditLogExportDto> RecentAuditLogs { get; set; } = new();
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public string ExportedBy { get; set; } = string.Empty;
    }

    public class UserScopeExportDto
    {
        public string ScopeType { get; set; } = string.Empty;
        public int? ScopeId { get; set; }
    }

    public class UserAuditLogExportDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ComplianceAuditSummaryDto
    {
        public int TotalAuditLogs { get; set; }
        public int TotalActiveUsers { get; set; }
        public int Total2FAUsers { get; set; }
        public int TotalArchivedAuditLogs { get; set; }
        public int TotalArchivedDailyReadings { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class AnonymizeUserResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime AnonymizedAt { get; set; } = DateTime.UtcNow;
    }
}
