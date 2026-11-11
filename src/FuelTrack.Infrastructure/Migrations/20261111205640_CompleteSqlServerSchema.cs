using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSqlServerSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TankLevelReadings_Tank_Captured",
                table: "TankLevelReadings");

            migrationBuilder.DropIndex(
                name: "UX_OperationalGeneratorReadings_Generator_Captured",
                table: "OperationalGeneratorReadings");

            migrationBuilder.DropIndex(
                name: "UX_MeterReadings_Subject_Captured",
                table: "MeterReadings");

            migrationBuilder.DropIndex(
                name: "UX_FeederLoadReadings_Feeder_Captured",
                table: "FeederLoadReadings");





            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "TankLevelReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "TankLevelReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BeforeComputedLitres",
                table: "TankLevelReadings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BeforeLiquidLevelCm",
                table: "TankLevelReadings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientLineId",
                table: "TankLevelReadings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "DeadStockLitresSnapshot",
                table: "TankLevelReadings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryNoteLitres",
                table: "TankLevelReadings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventType",
                table: "TankLevelReadings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LinkedEventId",
                table: "TankLevelReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "TankLevelReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "TankLevelReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "TankLevelReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "TankLevelReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageLoadWarningTolerancePercent",
                table: "StationOperationalSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelReconciliationWarningThresholdPercent",
                table: "StationOperationalSettings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InternalConsumptionMeterId",
                table: "StationOperationalSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MissingDataIntervalMinutes",
                table: "StationOperationalSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OverloadHysteresisReadings",
                table: "StationOperationalSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OverloadThresholdPercent",
                table: "StationOperationalSettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "RepeatedValueWarningCount",
                table: "StationOperationalSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Tier1RequiresApproval",
                table: "StationOperationalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClientSentAtUtc",
                table: "OperationalSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmissionCapturedAtUtc",
                table: "OperationalSubmissions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "OperationalSubmissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AirPressureBar",
                table: "OperationalGeneratorReadings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "OperationalGeneratorReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "OperationalGeneratorReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientLineId",
                table: "OperationalGeneratorReadings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "PeakLoadKw",
                table: "OperationalGeneratorReadings",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "OperationalGeneratorReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "OperationalGeneratorReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "OperationalGeneratorReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "OperationalGeneratorReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "MeterReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "MeterReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientLineId",
                table: "MeterReadings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "MeterReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "MeterReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "MeterReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "MeterReadings",
                type: "nvarchar(max)",
                nullable: true);




            migrationBuilder.AddColumn<string>(
                name: "DeadStockInputUnit",
                table: "FuelTanks",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "litres");

            migrationBuilder.AddColumn<decimal>(
                name: "DeadStockLevelCm",
                table: "FuelTanks",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);


            migrationBuilder.AddColumn<int>(
                name: "GeneratorId",
                table: "FuelTanks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentTankId",
                table: "FuelTanks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "FeederLoadReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "FeederLoadReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClientLineId",
                table: "FeederLoadReadings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "FeederLoadReadings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "FeederLoadReadings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "FeederLoadReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "FeederLoadReadings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GeneratorOperatingLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: false),
                    Parameter = table.Column<int>(type: "int", nullable: false),
                    Minimum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Maximum = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LastEvaluatedReadingId = table.Column<int>(type: "int", nullable: true),
                    LastEvaluatedCapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratorOperatingLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratorOperatingLimits_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OverloadEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: false),
                    TriggerReadingId = table.Column<int>(type: "int", nullable: false),
                    LatestReadingId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeakPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ConsecutiveNormalReadings = table.Column<int>(type: "int", nullable: false),
                    LatestReadingCapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OverloadEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OverloadEvents_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OverloadEvents_OperationalGeneratorReadings_TriggerReadingId",
                        column: x => x.TriggerReadingId,
                        principalTable: "OperationalGeneratorReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SuggestedIntervalHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TankDeadStockEpisodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FuelTankId = table.Column<int>(type: "int", nullable: false),
                    TriggerReadingId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TankDeadStockEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TankDeadStockEpisodes_FuelTanks_FuelTankId",
                        column: x => x.FuelTankId,
                        principalTable: "FuelTanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TankDeadStockEpisodes_TankLevelReadings_TriggerReadingId",
                        column: x => x.TriggerReadingId,
                        principalTable: "TankLevelReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperatingLimitEpisodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorOperatingLimitId = table.Column<int>(type: "int", nullable: false),
                    TriggerReadingId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObservedValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingLimitEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatingLimitEpisodes_GeneratorOperatingLimits_GeneratorOperatingLimitId",
                        column: x => x.GeneratorOperatingLimitId,
                        principalTable: "GeneratorOperatingLimits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperatingLimitEpisodes_OperationalGeneratorReadings_TriggerReadingId",
                        column: x => x.TriggerReadingId,
                        principalTable: "OperationalGeneratorReadings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceIntervalDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorModelKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ServiceItemId = table.Column<int>(type: "int", nullable: false),
                    IntervalHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Importance = table.Column<int>(type: "int", nullable: false),
                    WarningLeadHours = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceIntervalDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceIntervalDefinitions_ServiceItems_ServiceItemId",
                        column: x => x.ServiceItemId,
                        principalTable: "ServiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceReplacementRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: false),
                    ServiceItemId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    HoursAtService = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkPermitId = table.Column<int>(type: "int", nullable: true),
                    IsEarly = table.Column<bool>(type: "bit", nullable: false),
                    EarlyReplacementReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IntervalHoursSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedByUserId = table.Column<int>(type: "int", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceReplacementRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceReplacementRecords_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceReplacementRecords_ServiceItems_ServiceItemId",
                        column: x => x.ServiceItemId,
                        principalTable: "ServiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceReplacementRecords_WorkPermits_WorkPermitId",
                        column: x => x.WorkPermitId,
                        principalTable: "WorkPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDueStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: false),
                    ServiceItemId = table.Column<int>(type: "int", nullable: false),
                    CycleRecordId = table.Column<int>(type: "int", nullable: true),
                    HighestNotifiedStatus = table.Column<int>(type: "int", nullable: true),
                    LastEvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDueStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDueStates_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDueStates_ServiceItems_ServiceItemId",
                        column: x => x.ServiceItemId,
                        principalTable: "ServiceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDueStates_ServiceReplacementRecords_CycleRecordId",
                        column: x => x.CycleRecordId,
                        principalTable: "ServiceReplacementRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // The notification migration was never registered in EF's migration history.
            migrationBuilder.Sql(@"IF COL_LENGTH('dbo.Notifications', 'RelatedEntityId') IS NULL ALTER TABLE dbo.Notifications ADD RelatedEntityId int NULL;
IF COL_LENGTH('dbo.Notifications', 'RelatedEntityType') IS NULL ALTER TABLE dbo.Notifications ADD RelatedEntityType nvarchar(64) NULL;");
            // Assign a distinct identifier to legacy rows before creating unique indexes.
            migrationBuilder.Sql(@"UPDATE dbo.TankLevelReadings SET ClientLineId = NEWID() WHERE ClientLineId = '00000000-0000-0000-0000-000000000000';
UPDATE dbo.OperationalGeneratorReadings SET ClientLineId = NEWID() WHERE ClientLineId = '00000000-0000-0000-0000-000000000000';
UPDATE dbo.MeterReadings SET ClientLineId = NEWID() WHERE ClientLineId = '00000000-0000-0000-0000-000000000000';
UPDATE dbo.FeederLoadReadings SET ClientLineId = NEWID() WHERE ClientLineId = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateIndex(
                name: "IX_TankLevelReadings_LinkedEventId",
                table: "TankLevelReadings",
                column: "LinkedEventId");

            migrationBuilder.CreateIndex(
                name: "UX_TankLevelReadings_ClientLine",
                table: "TankLevelReadings",
                column: "ClientLineId",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TankLevelReadings_Tank_Captured",
                table: "TankLevelReadings",
                columns: new[] { "FuelTankId", "CapturedAtUtc" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [Status] <> 2 AND [Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "UX_OperationalGeneratorReadings_ClientLine",
                table: "OperationalGeneratorReadings",
                column: "ClientLineId",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_OperationalGeneratorReadings_Generator_Captured",
                table: "OperationalGeneratorReadings",
                columns: new[] { "GeneratorId", "CapturedAtUtc" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [Status] <> 2 AND [Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "UX_MeterReadings_ClientLine",
                table: "MeterReadings",
                column: "ClientLineId",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_MeterReadings_Subject_Captured",
                table: "MeterReadings",
                columns: new[] { "SubjectType", "SubjectId", "CapturedAtUtc" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [Status] <> 2 AND [Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTanks_ParentTankId",
                table: "FuelTanks",
                column: "ParentTankId");

            migrationBuilder.CreateIndex(
                name: "UX_FuelTanks_ActiveDailyGenerator",
                table: "FuelTanks",
                column: "GeneratorId",
                unique: true,
                filter: "[DeletedAt] IS NULL AND [GeneratorId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FeederLoadReadings_ClientLine",
                table: "FeederLoadReadings",
                column: "ClientLineId",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FeederLoadReadings_Feeder_Captured",
                table: "FeederLoadReadings",
                columns: new[] { "FeederId", "CapturedAtUtc" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [Status] <> 2 AND [Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "UX_GeneratorOperatingLimits_Generator_Parameter",
                table: "GeneratorOperatingLimits",
                columns: new[] { "GeneratorId", "Parameter" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperatingLimitEpisodes_Limit_Open",
                table: "OperatingLimitEpisodes",
                columns: new[] { "GeneratorOperatingLimitId", "ResolvedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatingLimitEpisodes_TriggerReadingId",
                table: "OperatingLimitEpisodes",
                column: "TriggerReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_OverloadEvents_Generator_Open",
                table: "OverloadEvents",
                columns: new[] { "GeneratorId", "ResolvedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OverloadEvents_TriggerReadingId",
                table: "OverloadEvents",
                column: "TriggerReadingId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDueStates_CycleRecordId",
                table: "ServiceDueStates",
                column: "CycleRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDueStates_ServiceItemId",
                table: "ServiceDueStates",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceDueStates_Generator_Item",
                table: "ServiceDueStates",
                columns: new[] { "GeneratorId", "ServiceItemId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceIntervalDefinitions_ServiceItemId",
                table: "ServiceIntervalDefinitions",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceIntervalDefinitions_Model_Item",
                table: "ServiceIntervalDefinitions",
                columns: new[] { "GeneratorModelKey", "ServiceItemId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceItems_Code",
                table: "ServiceItems",
                column: "Code",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReplacementRecords_Gen_Item_Hours",
                table: "ServiceReplacementRecords",
                columns: new[] { "GeneratorId", "ServiceItemId", "HoursAtService" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReplacementRecords_ServiceItemId",
                table: "ServiceReplacementRecords",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReplacementRecords_Voided",
                table: "ServiceReplacementRecords",
                columns: new[] { "GeneratorId", "ServiceItemId", "VoidedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReplacementRecords_WorkPermitId",
                table: "ServiceReplacementRecords",
                column: "WorkPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_TankDeadStockEpisodes_Tank_Open",
                table: "TankDeadStockEpisodes",
                columns: new[] { "FuelTankId", "ResolvedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TankDeadStockEpisodes_TriggerReading",
                table: "TankDeadStockEpisodes",
                column: "TriggerReadingId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelTanks_FuelTanks_ParentTankId",
                table: "FuelTanks",
                column: "ParentTankId",
                principalTable: "FuelTanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelTanks_Generators_GeneratorId",
                table: "FuelTanks",
                column: "GeneratorId",
                principalTable: "Generators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TankLevelReadings_TankLevelReadings_LinkedEventId",
                table: "TankLevelReadings",
                column: "LinkedEventId",
                principalTable: "TankLevelReadings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration preserves legacy data and cannot be safely reversed automatically.");
        }
    }
}
