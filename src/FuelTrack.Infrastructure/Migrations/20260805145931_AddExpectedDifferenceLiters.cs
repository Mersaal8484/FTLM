using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpectedDifferenceLiters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DifferenceLiters",
                table: "PeriodicInventoryOils",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryOils",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DifferenceLiters",
                table: "PeriodicInventoryFuels",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryFuels",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DifferenceLiters",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "DifferenceLiters",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryFuels");
        }
    }
}
