using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyticsInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: true),
                    StationId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InsightType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsInsights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogArchives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogArchives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsGeneration = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyReadingArchives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrevEnergyKWh = table.Column<double>(type: "float", nullable: false),
                    CurrentEnergyKWh = table.Column<double>(type: "float", nullable: false),
                    PrevRunningHours = table.Column<double>(type: "float", nullable: false),
                    CurrentRunningHours = table.Column<double>(type: "float", nullable: false),
                    FuelLookupId = table.Column<int>(type: "int", nullable: false),
                    StandardFuelRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OilLookupId = table.Column<int>(type: "int", nullable: false),
                    OilUsedLiters = table.Column<double>(type: "float", nullable: false),
                    ReadingStatusLookupId = table.Column<int>(type: "int", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReadingArchives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModuleDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Route = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Template = table.Column<int>(type: "int", nullable: false),
                    ScopeType = table.Column<int>(type: "int", nullable: false),
                    CustomActions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupportsAuditLog = table.Column<bool>(type: "bit", nullable: false),
                    SupportsGlobalSearch = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleDefinitions", x => x.Id);
                    table.UniqueConstraint("AK_ModuleDefinitions_Key", x => x.Key);
                    table.ForeignKey(
                        name: "FK_ModuleDefinitions_ModuleDefinitions_ParentKey",
                        column: x => x.ParentKey,
                        principalTable: "ModuleDefinitions",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorBackupCodes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsightFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalyticsInsightId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsHelpful = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsightFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsightFeedbacks_AnalyticsInsights_AnalyticsInsightId",
                        column: x => x.AnalyticsInsightId,
                        principalTable: "AnalyticsInsights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lookups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lookups_LookupCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "LookupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleDefinitionId = table.Column<int>(type: "int", nullable: false),
                    ActionKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagePermissions_ModuleDefinitions_ModuleDefinitionId",
                        column: x => x.ModuleDefinitionId,
                        principalTable: "ModuleDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapacityKW = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubStations_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PagePermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_PagePermissions_PagePermissionId",
                        column: x => x.PagePermissionId,
                        principalTable: "PagePermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FuelTanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    FuelLookupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapacityLiters = table.Column<double>(type: "float", nullable: false),
                    MinAlertLiters = table.Column<double>(type: "float", nullable: true),
                    DeadStockLiters = table.Column<double>(type: "float", nullable: true),
                    ActualStockLiters = table.Column<double>(type: "float", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelTanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelTanks_Lookups_FuelLookupId",
                        column: x => x.FuelLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTanks_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Generators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NominalPower = table.Column<double>(type: "float", nullable: false),
                    NominalPowerUnitLookupId = table.Column<int>(type: "int", nullable: true),
                    ActualPowerKW = table.Column<int>(type: "int", nullable: false),
                    FuelConsumptionRatePerHour = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    GeneratorTypeLookupId = table.Column<int>(type: "int", nullable: true),
                    VoltageLookupId = table.Column<int>(type: "int", nullable: true),
                    FrequencyLookupId = table.Column<int>(type: "int", nullable: true),
                    PowerFactor = table.Column<double>(type: "float", nullable: true),
                    OwnershipTypeLookupId = table.Column<int>(type: "int", nullable: true),
                    StatusLookupId = table.Column<int>(type: "int", nullable: true),
                    InstallationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Generators_Lookups_FrequencyLookupId",
                        column: x => x.FrequencyLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Generators_Lookups_GeneratorTypeLookupId",
                        column: x => x.GeneratorTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Generators_Lookups_NominalPowerUnitLookupId",
                        column: x => x.NominalPowerUnitLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Generators_Lookups_OwnershipTypeLookupId",
                        column: x => x.OwnershipTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Generators_Lookups_StatusLookupId",
                        column: x => x.StatusLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Generators_Lookups_VoltageLookupId",
                        column: x => x.VoltageLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Generators_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentById = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Oils",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OilLookupId = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: true),
                    SubStationId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oils", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Oils_Lookups_OilLookupId",
                        column: x => x.OilLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Oils_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Oils_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PeriodicInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    PeriodTypeLookupId = table.Column<int>(type: "int", nullable: false),
                    InventoryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodicInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodicInventories_Lookups_PeriodTypeLookupId",
                        column: x => x.PeriodTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeriodicInventories_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeriodicInventories_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserScopes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    StationId = table.Column<int>(type: "int", nullable: true),
                    SubStationId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserScopes_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserScopes_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserScopes_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserScopes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Controllers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ControllerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Port = table.Column<int>(type: "int", nullable: true),
                    ModbusUnitId = table.Column<int>(type: "int", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnergyReading = table.Column<double>(type: "float", nullable: false),
                    OperatingHours = table.Column<double>(type: "float", nullable: false),
                    RegisterType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoConnect = table.Column<bool>(type: "bit", nullable: false),
                    EnergyUnit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Controllers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Controllers_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DailyReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrevEnergyKWh = table.Column<double>(type: "float", nullable: false),
                    CurrentEnergyKWh = table.Column<double>(type: "float", nullable: false),
                    PrevRunningHours = table.Column<double>(type: "float", nullable: false),
                    CurrentRunningHours = table.Column<double>(type: "float", nullable: false),
                    FuelLookupId = table.Column<int>(type: "int", nullable: false),
                    StandardFuelRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OilLookupId = table.Column<int>(type: "int", nullable: false),
                    OilUsedLiters = table.Column<double>(type: "float", nullable: false),
                    ReadingStatusLookupId = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyReadings_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyReadings_Lookups_FuelLookupId",
                        column: x => x.FuelLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyReadings_Lookups_OilLookupId",
                        column: x => x.OilLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyReadings_Lookups_ReadingStatusLookupId",
                        column: x => x.ReadingStatusLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Meters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: true),
                    SubStationId = table.Column<int>(type: "int", nullable: true),
                    MeterType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MeterTypeLookupId = table.Column<int>(type: "int", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratorId = table.Column<int>(type: "int", nullable: true),
                    FuelTankId = table.Column<int>(type: "int", nullable: true),
                    HoursReading = table.Column<double>(type: "float", nullable: true),
                    EnergyReading = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meters_FuelTanks_FuelTankId",
                        column: x => x.FuelTankId,
                        principalTable: "FuelTanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Meters_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Meters_Lookups_MeterTypeLookupId",
                        column: x => x.MeterTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meters_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Meters_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRecipients_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationRecipients_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OilTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    OilLookupId = table.Column<int>(type: "int", nullable: false),
                    TransactionTypeLookupId = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityLiters = table.Column<double>(type: "float", nullable: false),
                    SupplierCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverIdCard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TruckNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvoiceQuantity = table.Column<double>(type: "float", nullable: true),
                    WarehouseReceiptNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WarehouseReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarehouseQuantity = table.Column<double>(type: "float", nullable: true),
                    SourceSubStationId = table.Column<int>(type: "int", nullable: true),
                    DestinationSubStationId = table.Column<int>(type: "int", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OilId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OilTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OilTransactions_Lookups_OilLookupId",
                        column: x => x.OilLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilTransactions_Lookups_TransactionTypeLookupId",
                        column: x => x.TransactionTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilTransactions_Oils_OilId",
                        column: x => x.OilId,
                        principalTable: "Oils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OilTransactions_SubStations_DestinationSubStationId",
                        column: x => x.DestinationSubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OilTransactions_SubStations_SourceSubStationId",
                        column: x => x.SourceSubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OilTransactions_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OilTransactions_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PeriodicInventoryFuels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodicInventoryId = table.Column<int>(type: "int", nullable: false),
                    FuelLookupId = table.Column<int>(type: "int", nullable: false),
                    ActualStockLiters = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodicInventoryFuels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodicInventoryFuels_Lookups_FuelLookupId",
                        column: x => x.FuelLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeriodicInventoryFuels_PeriodicInventories_PeriodicInventoryId",
                        column: x => x.PeriodicInventoryId,
                        principalTable: "PeriodicInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PeriodicInventoryOils",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodicInventoryId = table.Column<int>(type: "int", nullable: false),
                    OilLookupId = table.Column<int>(type: "int", nullable: false),
                    OilId = table.Column<int>(type: "int", nullable: true),
                    ActualStockLiters = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodicInventoryOils", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodicInventoryOils_Lookups_OilLookupId",
                        column: x => x.OilLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeriodicInventoryOils_Oils_OilId",
                        column: x => x.OilId,
                        principalTable: "Oils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PeriodicInventoryOils_PeriodicInventories_PeriodicInventoryId",
                        column: x => x.PeriodicInventoryId,
                        principalTable: "PeriodicInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuelTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    FuelLookupId = table.Column<int>(type: "int", nullable: false),
                    TransactionTypeLookupId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityLiters = table.Column<double>(type: "float", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupplierCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverIdCard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TruckNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvoiceQuantity = table.Column<double>(type: "float", nullable: true),
                    WarehouseReceiptNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WarehouseReceiptDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarehouseQuantity = table.Column<double>(type: "float", nullable: true),
                    MeterId = table.Column<int>(type: "int", nullable: true),
                    PrevMeterVisualReading = table.Column<double>(type: "float", nullable: true),
                    CurrentMeterVisualReading = table.Column<double>(type: "float", nullable: true),
                    PrevMeterStandardReading = table.Column<double>(type: "float", nullable: true),
                    CurrentMeterStandardReading = table.Column<double>(type: "float", nullable: true),
                    SourceSubStationId = table.Column<int>(type: "int", nullable: true),
                    DestinationSubStationId = table.Column<int>(type: "int", nullable: true),
                    CorrectedFromId = table.Column<int>(type: "int", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_Lookups_FuelLookupId",
                        column: x => x.FuelLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_Lookups_TransactionTypeLookupId",
                        column: x => x.TransactionTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_SubStations_DestinationSubStationId",
                        column: x => x.DestinationSubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_SubStations_SourceSubStationId",
                        column: x => x.SourceSubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelTransactions_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubStationReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    MeterId = table.Column<int>(type: "int", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrevExportedEnergyKWh = table.Column<double>(type: "float", nullable: false),
                    CurrentExportedEnergyKWh = table.Column<double>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadingStatusLookupId = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubStationReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubStationReadings_Lookups_ReadingStatusLookupId",
                        column: x => x.ReadingStatusLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubStationReadings_Meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "Meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubStationReadings_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LookupCategories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "Description", "DisplayName", "IsActive", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Active/Inactive status for soft-delete entities", "Active Status", true, "ActiveStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Voltage ratings: 400V, 11KV, 33KV", "الفولتية", true, "Voltage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Frequency: 50Hz, 60Hz", "التردد", true, "Frequency", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Generator operating modes: PRIME, CONTINUOUS, STANDBY", "انواع المولدات", true, "GeneratorType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Ownership: Owner (ملك), Tenant (مستأجر)", "نوع الملكية", true, "OwnershipType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Generator status: Running, Stopped, Under Maintenance", "حالة المولد", true, "GeneratorStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Meter types: Fuel Drain, Export Energy, Generator", "انواع العدادات", true, "MeterType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Fuel types: Diesel, Mazout", "انواع الوقود", true, "FuelType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 9, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Oil types: LOCK OIL, Engine Oil SAE 15W-40, GOLF, MOBIL, TOTAL", "انواع الزيوت", true, "OilType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "System roles: SystemAdmin, Manager, Supervisor, FuelOfficer, Operator, Visitor", "الأدوار الوظيفية", true, "SystemRole", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 11, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Inventory period type: First Half, Second Half", "نوع الفترة", true, "PeriodType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 12, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Shift duration in hours: 8, 12, 24", "مدة الوردية", true, "ShiftDuration", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Shift type: Morning, Night", "نوع الوردية", true, "ShiftType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Controller/Display model brands", "موديل الميتر", true, "ControllerModel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Register type: KWh, KVarh, KVAh", "نوع العداد", true, "RegisterType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 16, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Energy unit: Watt-hour, Joule", "وحدة الطاقة", true, "EnergyUnit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 17, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Sub-station status: Active, Under Maintenance, Decommissioned", "حالة المحطة الفرعية", true, "SubStationStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 18, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Item type for reports: Fuel, Oil", "نوع الصنف", true, "ItemType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 }
                });

            migrationBuilder.InsertData(
                table: "Lookups",
                columns: new[] { "Id", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, 1, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "فعال", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 2, 1, "Inactive", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "غير فعال", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 3, 2, "400V", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "400V", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 4, 2, "11KV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "11KV", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 5, 2, "33KV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "33KV", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 6, 3, "50Hz", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "50Hz", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 7, 3, "60Hz", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "60Hz", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 8, 4, "PRIME", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "PRIME", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 9, 4, "CONTINUOUS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "CONTINUOUS", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 10, 4, "STANDBY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "STANDBY", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 11, 5, "Owner", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ملك", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 12, 5, "Tenant", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مستأجر", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 13, 6, "Running", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "شغال", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 14, 6, "Stopped", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "واقف", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 15, 6, "UnderMaintenance", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "تحت الصيانة", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 16, 7, "FuelDrain", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "عداد وقود", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 17, 7, "ExportEnergy", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "عداد طاقة", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 18, 7, "GeneratorMeter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "عداد مولدات", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 19, 8, "Diesel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ديزل", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 20, 8, "Mazout", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مازوت", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 21, 9, "LockOil", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "LOCK OIL", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 22, 9, "EngineOil15W40", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "Engine Oil SAE 15W-40", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 23, 9, "Golf", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "GOLF", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 24, 9, "Mobil", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "MOBIL", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 25, 9, "Total", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "TOTAL", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 26, 10, "SystemAdmin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مسئول النظام", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 27, 10, "Manager", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مدير", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 28, 10, "Supervisor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مشرف", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 29, 10, "FuelOfficer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مسئول الوقود", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 30, 10, "Operator", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مشغل", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 31, 10, "Visitor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "زائر", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 32, 11, "FirstHalf", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "النصف الأول", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 33, 11, "SecondHalf", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "النصف الثاني", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 34, 12, "8", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "8 ساعات", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 35, 12, "12", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "12 ساعة", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 36, 12, "24", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "24 ساعة", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 37, 13, "Morning", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "صباحي", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 38, 13, "Night", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ليلي", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 39, 14, "SCT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "SCT", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 40, 14, "ComAp", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ComAp", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 41, 14, "DeepSea", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "Deep Sea", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 42, 14, "Densei", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "Densei", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 43, 14, "Lovato", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "Lovato", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 44, 15, "KWh", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "kWh", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 45, 15, "KVarh", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "kVarh", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 46, 15, "KVAh", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "kVAh", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 47, 16, "WattHour", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "Wh", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 48, 16, "Joule", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "J", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 49, 17, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "نشطة", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 50, 17, "UnderMaintenance", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "تحت الصيانة", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 51, 17, "Decommissioned", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "متوقفة", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 52, 18, "Fuel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "وقود", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 53, 18, "Oil", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "زيت", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 54, 8, "Petrol", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "بنزين", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogArchive_ArchivedAt",
                table: "AuditLogArchives",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogArchive_EntityName",
                table: "AuditLogArchives",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogArchive_Timestamp",
                table: "AuditLogArchives",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Controllers_GeneratorId",
                table: "Controllers",
                column: "GeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadingArchive_ArchivedAt",
                table: "DailyReadingArchives",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadingArchive_GeneratorId",
                table: "DailyReadingArchives",
                column: "GeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReading_Date_Generator",
                table: "DailyReadings",
                columns: new[] { "ReadingDate", "GeneratorId" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyReading_Status",
                table: "DailyReadings",
                column: "ReadingStatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadings_FuelLookupId",
                table: "DailyReadings",
                column: "FuelLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadings_GeneratorId_ReadingDate",
                table: "DailyReadings",
                columns: new[] { "GeneratorId", "ReadingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadings_OilLookupId",
                table: "DailyReadings",
                column: "OilLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTanks_FuelLookupId",
                table: "FuelTanks",
                column: "FuelLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTanks_SubStationId_FuelLookupId",
                table: "FuelTanks",
                columns: new[] { "SubStationId", "FuelLookupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransaction_SubStation_Date",
                table: "FuelTransactions",
                columns: new[] { "SubStationId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_ApprovedById",
                table: "FuelTransactions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_DestinationSubStationId",
                table: "FuelTransactions",
                column: "DestinationSubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_FuelLookupId",
                table: "FuelTransactions",
                column: "FuelLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_MeterId",
                table: "FuelTransactions",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_SourceSubStationId",
                table: "FuelTransactions",
                column: "SourceSubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_TransactionDate",
                table: "FuelTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_TransactionTypeLookupId",
                table: "FuelTransactions",
                column: "TransactionTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_FrequencyLookupId",
                table: "Generators",
                column: "FrequencyLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_GeneratorTypeLookupId",
                table: "Generators",
                column: "GeneratorTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_NominalPowerUnitLookupId",
                table: "Generators",
                column: "NominalPowerUnitLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_OwnershipTypeLookupId",
                table: "Generators",
                column: "OwnershipTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_StatusLookupId",
                table: "Generators",
                column: "StatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_SubStationId",
                table: "Generators",
                column: "SubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Generators_VoltageLookupId",
                table: "Generators",
                column: "VoltageLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_InsightFeedbacks_AnalyticsInsightId_UserId",
                table: "InsightFeedbacks",
                columns: new[] { "AnalyticsInsightId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Lookups_CategoryId_Code",
                table: "Lookups",
                columns: new[] { "CategoryId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_FuelTankId",
                table: "Meters",
                column: "FuelTankId");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_GeneratorId",
                table: "Meters",
                column: "GeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_MeterTypeLookupId",
                table: "Meters",
                column: "MeterTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_StationId",
                table: "Meters",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_SubStationId",
                table: "Meters",
                column: "SubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleDefinitions_Key",
                table: "ModuleDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleDefinitions_ParentKey",
                table: "ModuleDefinitions",
                column: "ParentKey");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleDefinitions_SortOrder",
                table: "ModuleDefinitions",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_NotificationId_UserId",
                table: "NotificationRecipients",
                columns: new[] { "NotificationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_UserId",
                table: "NotificationRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_SubStation_Created",
                table: "Notifications",
                columns: new[] { "SubStationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Oils_OilLookupId",
                table: "Oils",
                column: "OilLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_Oils_StationId",
                table: "Oils",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Oils_SubStationId",
                table: "Oils",
                column: "SubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransaction_SubStation_Date",
                table: "OilTransactions",
                columns: new[] { "SubStationId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_ApprovedById",
                table: "OilTransactions",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_DestinationSubStationId",
                table: "OilTransactions",
                column: "DestinationSubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_OilId",
                table: "OilTransactions",
                column: "OilId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_OilLookupId",
                table: "OilTransactions",
                column: "OilLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_SourceSubStationId",
                table: "OilTransactions",
                column: "SourceSubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_TransactionDate",
                table: "OilTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_TransactionTypeLookupId",
                table: "OilTransactions",
                column: "TransactionTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_PagePermissions_ModuleDefinitionId_ActionKey",
                table: "PagePermissions",
                columns: new[] { "ModuleDefinitionId", "ActionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventories_ApprovedById",
                table: "PeriodicInventories",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventories_PeriodTypeLookupId",
                table: "PeriodicInventories",
                column: "PeriodTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventory_SubStation_Period",
                table: "PeriodicInventories",
                columns: new[] { "SubStationId", "PeriodTypeLookupId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryFuels_FuelLookupId",
                table: "PeriodicInventoryFuels",
                column: "FuelLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryFuels_PeriodicInventoryId",
                table: "PeriodicInventoryFuels",
                column: "PeriodicInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryOils_OilId",
                table: "PeriodicInventoryOils",
                column: "OilId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryOils_OilLookupId",
                table: "PeriodicInventoryOils",
                column: "OilLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryOils_PeriodicInventoryId",
                table: "PeriodicInventoryOils",
                column: "PeriodicInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PagePermissionId",
                table: "RolePermissions",
                column: "PagePermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PagePermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PagePermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_BranchId",
                table: "Stations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SubStationReadings_MeterId",
                table: "SubStationReadings",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_SubStationReadings_ReadingStatusLookupId",
                table: "SubStationReadings",
                column: "ReadingStatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_SubStationReadings_SubStationId_ReadingDate",
                table: "SubStationReadings",
                columns: new[] { "SubStationId", "ReadingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubStations_StationId",
                table: "SubStations",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_BranchId",
                table: "UserScopes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_StationId",
                table: "UserScopes",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_SubStationId",
                table: "UserScopes",
                column: "SubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_UserId_BranchId_StationId_SubStationId",
                table: "UserScopes",
                columns: new[] { "UserId", "BranchId", "StationId", "SubStationId" },
                unique: true,
                filter: "[BranchId] IS NOT NULL AND [StationId] IS NOT NULL AND [SubStationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogArchives");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Controllers");

            migrationBuilder.DropTable(
                name: "DailyReadingArchives");

            migrationBuilder.DropTable(
                name: "DailyReadings");

            migrationBuilder.DropTable(
                name: "FuelTransactions");

            migrationBuilder.DropTable(
                name: "InsightFeedbacks");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "NotificationRecipients");

            migrationBuilder.DropTable(
                name: "OilTransactions");

            migrationBuilder.DropTable(
                name: "PeriodicInventoryFuels");

            migrationBuilder.DropTable(
                name: "PeriodicInventoryOils");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SubStationReadings");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserScopes");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "AnalyticsInsights");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Oils");

            migrationBuilder.DropTable(
                name: "PeriodicInventories");

            migrationBuilder.DropTable(
                name: "PagePermissions");

            migrationBuilder.DropTable(
                name: "Meters");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ModuleDefinitions");

            migrationBuilder.DropTable(
                name: "FuelTanks");

            migrationBuilder.DropTable(
                name: "Generators");

            migrationBuilder.DropTable(
                name: "Lookups");

            migrationBuilder.DropTable(
                name: "SubStations");

            migrationBuilder.DropTable(
                name: "LookupCategories");

            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}
