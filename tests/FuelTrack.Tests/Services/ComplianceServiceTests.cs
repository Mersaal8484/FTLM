using System;
using System.Threading.Tasks;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FuelTrack.Tests.Services
{
    public class ComplianceServiceTests : IDisposable
    {
        private readonly FuelTrackDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ComplianceService _service;

        public ComplianceServiceTests()
        {
            var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
                .UseInMemoryDatabase(databaseName: $"ComplianceTestDb_{Guid.NewGuid()}")
                .Options;
            _db = new FuelTrackDbContext(options);

            var userStore = new UserStore<User, Role, FuelTrackDbContext, int>(_db);
            var passwordHasher = new PasswordHasher<User>();
            _userManager = new UserManager<User>(userStore, null, passwordHasher, null, null, null, null, null, null);

            _service = new ComplianceService(_db, _userManager, NullLogger<ComplianceService>.Instance);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
            _userManager.Dispose();
        }

        [Fact]
        public async Task ExportUserDataAsync_ExistingUser_ReturnsComprehensiveData()
        {
            // Arrange
            var user = new User
            {
                UserName = "compliance_user",
                FullName = "Compliance Test User",
                Email = "compliance@test.com",
                PhoneNumber = "1234567890",
                IsActive = true,
                TwoFactorEnabled = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow
            };
            await _userManager.CreateAsync(user);

            // Add Scope
            _db.UserScopes.Add(new UserScope
            {
                UserId = user.Id,
                BranchId = 1,
                StationId = 2,
                SubStationId = null
            });

            // Add Audit log
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "LOGIN",
                EntityName = "User",
                EntityId = user.Id,
                Timestamp = DateTime.UtcNow.AddHours(-2),
                IpAddress = "192.168.1.100"
            });
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.ExportUserDataAsync(user.Id, "AdminUser");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal("compliance_user", result.Username);
            Assert.Equal("compliance@test.com", result.Email);
            Assert.Equal("Compliance Test User", result.FullName);
            Assert.True(result.TwoFactorEnabled);
            Assert.Single(result.Scopes);
            Assert.Equal("Station", result.Scopes[0].ScopeType);
            Assert.Single(result.RecentAuditLogs);
            Assert.Equal("LOGIN", result.RecentAuditLogs[0].Action);
            Assert.Equal("AdminUser", result.ExportedBy);
        }

        [Fact]
        public async Task ExportUserDataAsync_NonExistingUser_ReturnsNull()
        {
            // Act
            var result = await _service.ExportUserDataAsync(99999, "AdminUser");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AnonymizeUserPersonalDataAsync_ExistingUser_ErasesPersonalData()
        {
            // Arrange
            var user = new User
            {
                UserName = "john_doe",
                FullName = "John Doe",
                Email = "john.doe@company.org",
                PhoneNumber = "+1234567890",
                IsActive = true,
                TwoFactorEnabled = true,
                TwoFactorSecret = "SECRET_123",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };
            await _userManager.CreateAsync(user);

            // Act
            var result = await _service.AnonymizeUserPersonalDataAsync(user.Id, "PrivacyOfficer");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(user.Id, result.UserId);

            var updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
            Assert.NotNull(updatedUser);
            Assert.False(updatedUser.IsActive);
            Assert.NotNull(updatedUser.DeletedAt);
            Assert.Contains("Anonymized User", updatedUser.FullName);
            Assert.Contains("@compliance.local", updatedUser.Email);
            Assert.Null(updatedUser.PhoneNumber);
            Assert.False(updatedUser.TwoFactorEnabled);
            Assert.Null(updatedUser.TwoFactorSecret);
        }

        [Fact]
        public async Task GetComplianceAuditSummaryAsync_ReturnsSummaryCounts()
        {
            // Arrange
            var user = new User
            {
                UserName = "audit_user",
                FullName = "Audit User",
                Email = "audit@company.org",
                IsActive = true,
                TwoFactorEnabled = true
            };
            await _userManager.CreateAsync(user);

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                Action = "CREATE",
                EntityName = "FuelTank",
                EntityId = 1,
                Timestamp = DateTime.UtcNow
            });
            _db.AuditLogArchives.Add(new AuditLogArchive
            {
                UserId = user.Id,
                Action = "ARCHIVED_ACTION",
                EntityName = "DailyReading",
                EntityId = 1,
                Timestamp = DateTime.UtcNow.AddDays(-100),
                ArchivedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            // Act
            var summary = await _service.GetComplianceAuditSummaryAsync();

            // Assert
            Assert.NotNull(summary);
            Assert.True(summary.TotalActiveUsers >= 1);
            Assert.True(summary.Total2FAUsers >= 1);
            Assert.True(summary.TotalAuditLogs >= 1);
            Assert.True(summary.TotalArchivedAuditLogs >= 1);
        }
    }
}
