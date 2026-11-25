using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Infrastructure.Migrations;

[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261126020000_FixFuelTankNonDailyUniqueIndexFilter")]
public class FixFuelTankNonDailyUniqueIndexFilter : Migration
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
            filter: "[DeletedAt] IS NULL AND ([TankRole] IS NULL OR [TankRole] <> 2)");
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
            unique: true,
            filter: "[TankRole] = 1 AND [DeletedAt] IS NULL");
    }
}
