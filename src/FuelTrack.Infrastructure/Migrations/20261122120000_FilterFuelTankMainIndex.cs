using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Infrastructure.Migrations;

[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261122120000_FilterFuelTankMainIndex")]
public class FilterFuelTankMainIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.DropIndex(
            name: "IX_FuelTanks_SubStationId_FuelLookupId",
            table: "FuelTanks");

        migrationBuilder.CreateIndex(
            name: "IX_FuelTanks_SubStationId_FuelLookupId",
            table: "FuelTanks",
            columns: new[] { "SubStationId", "FuelLookupId" },
            unique: true,
            filter: "[DeletedAt] IS NULL AND [GeneratorId] IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.DropIndex(
            name: "IX_FuelTanks_SubStationId_FuelLookupId",
            table: "FuelTanks");

        migrationBuilder.CreateIndex(
            name: "IX_FuelTanks_SubStationId_FuelLookupId",
            table: "FuelTanks",
            columns: new[] { "SubStationId", "FuelLookupId" },
            unique: true);
    }
}
