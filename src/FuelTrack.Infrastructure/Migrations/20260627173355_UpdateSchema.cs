using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_ExportEnergyMeterId",
                table: "SubStations");

            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_FuelIntakeMeterId",
                table: "SubStations");

            migrationBuilder.AddForeignKey(
                name: "FK_SubStations_Meters_ExportEnergyMeterId",
                table: "SubStations",
                column: "ExportEnergyMeterId",
                principalTable: "Meters",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_SubStations_Meters_FuelIntakeMeterId",
                table: "SubStations",
                column: "FuelIntakeMeterId",
                principalTable: "Meters",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_ExportEnergyMeterId",
                table: "SubStations");

            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_FuelIntakeMeterId",
                table: "SubStations");

            migrationBuilder.AddForeignKey(
                name: "FK_SubStations_Meters_ExportEnergyMeterId",
                table: "SubStations",
                column: "ExportEnergyMeterId",
                principalTable: "Meters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubStations_Meters_FuelIntakeMeterId",
                table: "SubStations",
                column: "FuelIntakeMeterId",
                principalTable: "Meters",
                principalColumn: "Id");
        }
    }
}
