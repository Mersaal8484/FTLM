using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Infrastructure.Migrations;

[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261125120000_AddTankCalibrationModeAndHeights")]
public class AddTankCalibrationModeAndHeights : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.AddColumn<int>(
            name: "CalibrationMode",
            table: "FuelTanks",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "LitresPerCm",
            table: "FuelTanks",
            type: "decimal(18,6)",
            precision: 18,
            scale: 6,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "CapacityHeightCm",
            table: "FuelTanks",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "MinAlertHeightCm",
            table: "FuelTanks",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ActualStockHeightCm",
            table: "FuelTanks",
            type: "decimal(18,4)",
            precision: 18,
            scale: 4,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CapacityInputUnit",
            table: "FuelTanks",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "litres");

        migrationBuilder.AddColumn<string>(
            name: "MinAlertInputUnit",
            table: "FuelTanks",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "litres");

        migrationBuilder.AddColumn<string>(
            name: "ActualStockInputUnit",
            table: "FuelTanks",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: false,
            defaultValue: "litres");

        migrationBuilder.AddColumn<int>(
            name: "CalibrationModeSnapshot",
            table: "TankLevelReadings",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            name: "LitresPerCmSnapshot",
            table: "TankLevelReadings",
            type: "decimal(18,6)",
            precision: 18,
            scale: 6,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.DropColumn(name: "LitresPerCmSnapshot", table: "TankLevelReadings");
        migrationBuilder.DropColumn(name: "CalibrationModeSnapshot", table: "TankLevelReadings");
        migrationBuilder.DropColumn(name: "ActualStockInputUnit", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "MinAlertInputUnit", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "CapacityInputUnit", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "ActualStockHeightCm", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "MinAlertHeightCm", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "CapacityHeightCm", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "LitresPerCm", table: "FuelTanks");
        migrationBuilder.DropColumn(name: "CalibrationMode", table: "FuelTanks");
    }
}
