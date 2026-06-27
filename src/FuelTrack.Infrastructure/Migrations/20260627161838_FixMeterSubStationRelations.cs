using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMeterSubStationRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meters_SubStations_SubStationId",
                table: "Meters");

            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_ExportEnergyMeterId1",
                table: "SubStations");

            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_FuelIntakeMeterId1",
                table: "SubStations");

            migrationBuilder.DropIndex(
                name: "IX_SubStations_ExportEnergyMeterId1",
                table: "SubStations");

            migrationBuilder.DropIndex(
                name: "IX_SubStations_FuelIntakeMeterId1",
                table: "SubStations");

            migrationBuilder.DropColumn(
                name: "ExportEnergyMeterId1",
                table: "SubStations");

            migrationBuilder.DropColumn(
                name: "FuelIntakeMeterId1",
                table: "SubStations");

            migrationBuilder.CreateIndex(
                name: "IX_SubStations_ExportEnergyMeterId",
                table: "SubStations",
                column: "ExportEnergyMeterId");

            migrationBuilder.CreateIndex(
                name: "IX_SubStations_FuelIntakeMeterId",
                table: "SubStations",
                column: "FuelIntakeMeterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meters_SubStations_SubStationId",
                table: "Meters",
                column: "SubStationId",
                principalTable: "SubStations",
                principalColumn: "Id");

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
                name: "FK_Meters_SubStations_SubStationId",
                table: "Meters");

            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_ExportEnergyMeterId",
                table: "SubStations");

            migrationBuilder.DropForeignKey(
                name: "FK_SubStations_Meters_FuelIntakeMeterId",
                table: "SubStations");

            migrationBuilder.DropIndex(
                name: "IX_SubStations_ExportEnergyMeterId",
                table: "SubStations");

            migrationBuilder.DropIndex(
                name: "IX_SubStations_FuelIntakeMeterId",
                table: "SubStations");

            migrationBuilder.AddColumn<int>(
                name: "ExportEnergyMeterId1",
                table: "SubStations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FuelIntakeMeterId1",
                table: "SubStations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubStations_ExportEnergyMeterId1",
                table: "SubStations",
                column: "ExportEnergyMeterId1");

            migrationBuilder.CreateIndex(
                name: "IX_SubStations_FuelIntakeMeterId1",
                table: "SubStations",
                column: "FuelIntakeMeterId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Meters_SubStations_SubStationId",
                table: "Meters",
                column: "SubStationId",
                principalTable: "SubStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SubStations_Meters_ExportEnergyMeterId1",
                table: "SubStations",
                column: "ExportEnergyMeterId1",
                principalTable: "Meters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubStations_Meters_FuelIntakeMeterId1",
                table: "SubStations",
                column: "FuelIntakeMeterId1",
                principalTable: "Meters",
                principalColumn: "Id");
        }
    }
}
