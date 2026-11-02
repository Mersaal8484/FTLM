using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFuelTankIdFromPeriodicInventoryFuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PeriodicInventoryFuels_FuelTanks_FuelTankId",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropIndex(
                name: "IX_PeriodicInventoryFuels_FuelTankId",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "FuelTankId",
                table: "PeriodicInventoryFuels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FuelTankId",
                table: "PeriodicInventoryFuels",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodicInventoryFuels_FuelTankId",
                table: "PeriodicInventoryFuels",
                column: "FuelTankId");

            migrationBuilder.AddForeignKey(
                name: "FK_PeriodicInventoryFuels_FuelTanks_FuelTankId",
                table: "PeriodicInventoryFuels",
                column: "FuelTankId",
                principalTable: "FuelTanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
