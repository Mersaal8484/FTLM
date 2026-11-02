using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockSummaryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeriodicInventoryFuels_PeriodicInventoryId",
                table: "PeriodicInventoryFuels");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryFuel_Lookup",
                table: "PeriodicInventoryFuels",
                columns: new[] { "PeriodicInventoryId", "FuelLookupId" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventory_LastApproved",
                table: "PeriodicInventories",
                columns: new[] { "SubStationId", "InventoryDate", "IsApproved" },
                filter: "[DeletedAt] IS NULL AND [IsApproved] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_StockSummary",
                table: "OilTransactions",
                columns: new[] { "SubStationId", "OilLookupId", "TransactionDate", "IsApproved" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_StockSummary",
                table: "FuelTransactions",
                columns: new[] { "SubStationId", "FuelLookupId", "TransactionDate", "IsApproved" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadings_FuelConsumption",
                table: "DailyReadings",
                columns: new[] { "GeneratorId", "ReadingDate", "FuelLookupId" },
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeriodicInventoryFuel_Lookup",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropIndex(
                name: "IX_PeriodicInventory_LastApproved",
                table: "PeriodicInventories");

            migrationBuilder.DropIndex(
                name: "IX_OilTransactions_StockSummary",
                table: "OilTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FuelTransactions_StockSummary",
                table: "FuelTransactions");

            migrationBuilder.DropIndex(
                name: "IX_DailyReadings_FuelConsumption",
                table: "DailyReadings");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryFuels_PeriodicInventoryId",
                table: "PeriodicInventoryFuels",
                column: "PeriodicInventoryId");
        }
    }
}
