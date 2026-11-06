using System;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations;

/// <summary>Adds raw Phase 2 capture only. No approved balances, deltas, or review state are created here.</summary>
[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261107120000_AddOperationalCapture")]
public partial class AddOperationalCapture : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "Shape", table: "FuelTanks", type: "int", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "DiameterCm", table: "FuelTanks", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "CylinderHeightCm", table: "FuelTanks", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "ConeHeightCm", table: "FuelTanks", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "MultiplicationFactor", table: "FuelTanks", type: "decimal(18,6)", precision: 18, scale: 6, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "ComputedCapacityLitres", table: "FuelTanks", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);

        migrationBuilder.CreateTable(
            name: "OperationalSubmissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SubStationId = table.Column<int>(type: "int", nullable: false),
                ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<int>(type: "int", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<int>(type: "int", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OperationalSubmissions", x => x.Id);
                table.ForeignKey(name: "FK_OperationalSubmissions_SubStations_SubStationId", column: x => x.SubStationId, principalTable: "SubStations", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Feeders",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                SubStationId = table.Column<int>(type: "int", nullable: false),
                Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                MeterMultiplier = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                MeterId = table.Column<int>(type: "int", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<int>(type: "int", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<int>(type: "int", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Feeders", x => x.Id);
                table.ForeignKey(name: "FK_Feeders_Meters_MeterId", column: x => x.MeterId, principalTable: "Meters", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "FK_Feeders_SubStations_SubStationId", column: x => x.SubStationId, principalTable: "SubStations", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "OperationalGeneratorReadings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OperationalSubmissionId = table.Column<int>(type: "int", nullable: false), GeneratorId = table.Column<int>(type: "int", nullable: false),
                CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), BusinessDate = table.Column<DateOnly>(type: "date", nullable: false), Status = table.Column<int>(type: "int", nullable: false), IsRunning = table.Column<bool>(type: "bit", nullable: false),
                LineVoltageVolts = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), CurrentAmps = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), WaterTemperatureCelsius = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), OilPressure = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), OilTemperatureCelsius = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), ActiveLoadKw = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), ApparentLoadKva = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), ReactiveLoadKvar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), FrequencyHz = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), OilAddedLitres = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), PowerFactor = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true), WarningsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), CreatedBy = table.Column<int>(type: "int", nullable: false), UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), UpdatedBy = table.Column<int>(type: "int", nullable: false), DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table => { table.PrimaryKey("PK_OperationalGeneratorReadings", x => x.Id); table.ForeignKey(name: "FK_OperationalGeneratorReadings_Generators_GeneratorId", column: x => x.GeneratorId, principalTable: "Generators", principalColumn: "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey(name: "FK_OperationalGeneratorReadings_OperationalSubmissions_OperationalSubmissionId", column: x => x.OperationalSubmissionId, principalTable: "OperationalSubmissions", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateTable(
            name: "TankLevelReadings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), OperationalSubmissionId = table.Column<int>(type: "int", nullable: false), FuelTankId = table.Column<int>(type: "int", nullable: false), CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), BusinessDate = table.Column<DateOnly>(type: "date", nullable: false), Status = table.Column<int>(type: "int", nullable: false), LiquidLevelCm = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false), ComputedLitres = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false), ShapeSnapshot = table.Column<int>(type: "int", nullable: false), DiameterCmSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false), CylinderHeightCmSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false), ConeHeightCmSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false), MultiplicationFactorSnapshot = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false), WarningsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true), IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), CreatedBy = table.Column<int>(type: "int", nullable: false), UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), UpdatedBy = table.Column<int>(type: "int", nullable: false), DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table => { table.PrimaryKey("PK_TankLevelReadings", x => x.Id); table.ForeignKey(name: "FK_TankLevelReadings_FuelTanks_FuelTankId", column: x => x.FuelTankId, principalTable: "FuelTanks", principalColumn: "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey(name: "FK_TankLevelReadings_OperationalSubmissions_OperationalSubmissionId", column: x => x.OperationalSubmissionId, principalTable: "OperationalSubmissions", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateTable(
            name: "FeederLoadReadings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), OperationalSubmissionId = table.Column<int>(type: "int", nullable: false), FeederId = table.Column<int>(type: "int", nullable: false), CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), BusinessDate = table.Column<DateOnly>(type: "date", nullable: false), Status = table.Column<int>(type: "int", nullable: false), ActiveLoadKw = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), ApparentLoadKva = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true), MeterMultiplierSnapshot = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false), WarningsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true), IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), CreatedBy = table.Column<int>(type: "int", nullable: false), UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), UpdatedBy = table.Column<int>(type: "int", nullable: false), DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table => { table.PrimaryKey("PK_FeederLoadReadings", x => x.Id); table.ForeignKey(name: "FK_FeederLoadReadings_Feeders_FeederId", column: x => x.FeederId, principalTable: "Feeders", principalColumn: "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey(name: "FK_FeederLoadReadings_OperationalSubmissions_OperationalSubmissionId", column: x => x.OperationalSubmissionId, principalTable: "OperationalSubmissions", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateTable(
            name: "MeterReadings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"), OperationalSubmissionId = table.Column<int>(type: "int", nullable: false), SubjectType = table.Column<int>(type: "int", nullable: false), SubjectId = table.Column<int>(type: "int", nullable: false), MeterId = table.Column<int>(type: "int", nullable: true), Kind = table.Column<int>(type: "int", nullable: false), CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), BusinessDate = table.Column<DateOnly>(type: "date", nullable: false), Status = table.Column<int>(type: "int", nullable: false), RawValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false), MultiplierSnapshot = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false), IsMeterReset = table.Column<bool>(type: "bit", nullable: false), ResetNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true), WarningsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true), IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), CreatedBy = table.Column<int>(type: "int", nullable: false), UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), UpdatedBy = table.Column<int>(type: "int", nullable: false), DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table => { table.PrimaryKey("PK_MeterReadings", x => x.Id); table.ForeignKey(name: "FK_MeterReadings_Meters_MeterId", column: x => x.MeterId, principalTable: "Meters", principalColumn: "Id", onDelete: ReferentialAction.Restrict); table.ForeignKey(name: "FK_MeterReadings_OperationalSubmissions_OperationalSubmissionId", column: x => x.OperationalSubmissionId, principalTable: "OperationalSubmissions", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateIndex(name: "IX_Feeders_MeterId", table: "Feeders", column: "MeterId");
        migrationBuilder.CreateIndex(name: "UX_Feeders_Site_Code", table: "Feeders", columns: new[] { "SubStationId", "Code" }, unique: true, filter: "[DeletedAt] IS NULL");
        migrationBuilder.CreateIndex(name: "IX_OperationalSubmissions_Site_Received", table: "OperationalSubmissions", columns: new[] { "SubStationId", "ReceivedAtUtc" });
        migrationBuilder.CreateIndex(name: "UX_OperationalSubmissions_Site_User_Request", table: "OperationalSubmissions", columns: new[] { "SubStationId", "CreatedBy", "ClientRequestId" }, unique: true, filter: "[DeletedAt] IS NULL");
        migrationBuilder.CreateIndex(name: "IX_OperationalGeneratorReadings_OperationalSubmissionId", table: "OperationalGeneratorReadings", column: "OperationalSubmissionId");
        migrationBuilder.CreateIndex(name: "IX_OperationalGeneratorReadings_Generator_Date_Status", table: "OperationalGeneratorReadings", columns: new[] { "GeneratorId", "BusinessDate", "Status" });
        migrationBuilder.CreateIndex(name: "UX_OperationalGeneratorReadings_Generator_Captured", table: "OperationalGeneratorReadings", columns: new[] { "GeneratorId", "CapturedAtUtc" }, unique: true, filter: "[DeletedAt] IS NULL AND [Status] <> 2");
        migrationBuilder.CreateIndex(name: "IX_TankLevelReadings_OperationalSubmissionId", table: "TankLevelReadings", column: "OperationalSubmissionId");
        migrationBuilder.CreateIndex(name: "IX_TankLevelReadings_Tank_Date_Status", table: "TankLevelReadings", columns: new[] { "FuelTankId", "BusinessDate", "Status" });
        migrationBuilder.CreateIndex(name: "UX_TankLevelReadings_Tank_Captured", table: "TankLevelReadings", columns: new[] { "FuelTankId", "CapturedAtUtc" }, unique: true, filter: "[DeletedAt] IS NULL AND [Status] <> 2");
        migrationBuilder.CreateIndex(name: "IX_FeederLoadReadings_OperationalSubmissionId", table: "FeederLoadReadings", column: "OperationalSubmissionId");
        migrationBuilder.CreateIndex(name: "IX_FeederLoadReadings_Feeder_Date_Status", table: "FeederLoadReadings", columns: new[] { "FeederId", "BusinessDate", "Status" });
        migrationBuilder.CreateIndex(name: "UX_FeederLoadReadings_Feeder_Captured", table: "FeederLoadReadings", columns: new[] { "FeederId", "CapturedAtUtc" }, unique: true, filter: "[DeletedAt] IS NULL AND [Status] <> 2");
        migrationBuilder.CreateIndex(name: "IX_MeterReadings_OperationalSubmissionId", table: "MeterReadings", column: "OperationalSubmissionId");
        migrationBuilder.CreateIndex(name: "IX_MeterReadings_MeterId", table: "MeterReadings", column: "MeterId");
        migrationBuilder.CreateIndex(name: "UX_MeterReadings_Subject_Captured", table: "MeterReadings", columns: new[] { "SubjectType", "SubjectId", "CapturedAtUtc" }, unique: true, filter: "[DeletedAt] IS NULL AND [Status] <> 2");
        migrationBuilder.CreateIndex(name: "UX_MeterReadings_DailyClosing", table: "MeterReadings", columns: new[] { "SubjectType", "SubjectId", "BusinessDate", "Kind" }, unique: true, filter: "[DeletedAt] IS NULL AND [Status] <> 2 AND [Kind] = 1");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FeederLoadReadings");
        migrationBuilder.DropTable(name: "MeterReadings");
        migrationBuilder.DropTable(name: "OperationalGeneratorReadings");
        migrationBuilder.DropTable(name: "TankLevelReadings");
        migrationBuilder.DropTable(name: "Feeders");
        migrationBuilder.DropTable(name: "OperationalSubmissions");
        migrationBuilder.DropColumn(name: "Shape", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "DiameterCm", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "CylinderHeightCm", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "ConeHeightCm", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "MultiplicationFactor", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "ComputedCapacityLitres", table: "FuelTanks");
    }
}
