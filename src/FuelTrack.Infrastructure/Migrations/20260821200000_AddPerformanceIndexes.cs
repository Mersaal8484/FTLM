using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FuelTransactions_Date_Approved",
                table: "FuelTransactions",
                columns: new[] { "TransactionDate", "IsApproved" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_Date_Approved",
                table: "OilTransactions",
                columns: new[] { "TransactionDate", "IsApproved" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadings_Gen_Date_Status",
                table: "DailyReadings",
                columns: new[] { "GeneratorId", "ReadingDate", "ReadingStatusLookupId" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubStationReadings_Date_Status",
                table: "SubStationReadings",
                columns: new[] { "ReadingDate", "ReadingStatusLookupId" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SubStationReadings_SubStation_Date_Status",
                table: "SubStationReadings",
                columns: new[] { "SubStationId", "ReadingDate", "ReadingStatusLookupId" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventories_SubStation_Date",
                table: "PeriodicInventories",
                columns: new[] { "SubStationId", "InventoryDate" },
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FuelTransactions_Date_Approved",
                table: "FuelTransactions");

            migrationBuilder.DropIndex(
                name: "IX_OilTransactions_Date_Approved",
                table: "OilTransactions");

            migrationBuilder.DropIndex(
                name: "IX_DailyReadings_Gen_Date_Status",
                table: "DailyReadings");

            migrationBuilder.DropIndex(
                name: "IX_SubStationReadings_Date_Status",
                table: "SubStationReadings");

            migrationBuilder.DropIndex(
                name: "IX_SubStationReadings_SubStation_Date_Status",
                table: "SubStationReadings");

            migrationBuilder.DropIndex(
                name: "IX_PeriodicInventories_SubStation_Date",
                table: "PeriodicInventories");
        }
    }
}
