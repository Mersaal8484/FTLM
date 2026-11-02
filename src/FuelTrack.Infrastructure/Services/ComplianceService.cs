using System;
using System.Linq;
using System.Threading.Tasks;
using FuelTrack.Application.DTOs.Compliance;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FuelTrack.Infrastructure.Services
{
    public class ComplianceService : IComplianceService
    {
        private readonly FuelTrackDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ComplianceService> _logger;

        public ComplianceService(
            FuelTrackDbContext db,
            UserManager<User> userManager,
            ILogger<ComplianceService> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<UserDataExportDto?> ExportUserDataAsync(int userId, string requestedBy)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;

            var roles = (await _userManager.GetRolesAsync(user)).ToList();

            var scopes = await _db.UserScopes
                .Where(s => s.UserId == userId && s.DeletedAt == null)
                .Select(s => new UserScopeExportDto
                {
                    ScopeType = s.SubStationId.HasValue ? "SubStation" : (s.StationId.HasValue ? "Station" : (s.BranchId.HasValue ? "Branch" : "Global")),
                    ScopeId = s.SubStationId ?? s.StationId ?? s.BranchId
                })
                .ToListAsync();

            var recentAudits = await _db.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .Select(a => new UserAuditLogExportDto
                {
                    Id = a.Id,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    IpAddress = a.IpAddress,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();

            _logger.LogInformation("Enterprise compliance export executed for user {UserId} by {RequestedBy}", userId, requestedBy);

            return new UserDataExportDto
            {
                UserId = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                TwoFactorEnabled = user.TwoFactorEnabled,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = roles,
                Scopes = scopes,
                RecentAuditLogs = recentAudits,
                ExportedAt = DateTime.UtcNow,
                ExportedBy = requestedBy
            };
        }

        public async Task<AnonymizeUserResultDto> AnonymizeUserPersonalDataAsync(int userId, string requestedBy)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new AnonymizeUserResultDto
                {
                    Success = false,
                    Message = $"User with ID {userId} was not found.",
                    UserId = userId
                };
            }

            var randomTag = Guid.NewGuid().ToString("N")[..8];
            user.FullName = $"Anonymized User {randomTag}";
            user.Email = $"anonymized_{randomTag}@compliance.local";
            user.NormalizedEmail = user.Email.ToUpperInvariant();
            user.PhoneNumber = null;
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            user.TwoFactorBackupCodes = null;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            user.DeletedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            _logger.LogWarning("Enterprise compliance data anonymization executed for user {UserId} by {RequestedBy}", userId, requestedBy);

            return new AnonymizeUserResultDto
            {
                Success = true,
                Message = $"User {userId} personal data has been successfully anonymized in compliance with data privacy regulations.",
                UserId = userId,
                AnonymizedAt = DateTime.UtcNow
            };
        }

        public async Task<ComplianceAuditSummaryDto> GetComplianceAuditSummaryAsync()
        {
            var totalAudits = await _db.AuditLogs.CountAsync();
            var totalActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive && u.DeletedAt == null);
            var total2FA = await _userManager.Users.CountAsync(u => u.TwoFactorEnabled && u.DeletedAt == null);
            var archivedAudits = await _db.AuditLogArchives.CountAsync();
            var archivedReadings = await _db.DailyReadingArchives.CountAsync();

            return new ComplianceAuditSummaryDto
            {
                TotalAuditLogs = totalAudits,
                TotalActiveUsers = totalActiveUsers,
                Total2FAUsers = total2FA,
                TotalArchivedAuditLogs = archivedAudits,
                TotalArchivedDailyReadings = archivedReadings,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }
}
