using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropMeterUnusedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meters_FuelTanks_FuelTankId",
                table: "Meters");

            migrationBuilder.DropForeignKey(
                name: "FK_Meters_Stations_StationId",
                table: "Meters");

            migrationBuilder.DropIndex(
                name: "IX_Meters_FuelTankId",
                table: "Meters");

            migrationBuilder.DropIndex(
                name: "IX_Meters_StationId",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "FuelTankId",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "MeterType",
                table: "Meters");

            migrationBuilder.DropColumn(
                name: "StationId",
                table: "Meters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Meters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FuelTankId",
                table: "Meters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeterType",
                table: "Meters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StationId",
                table: "Meters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meters_FuelTankId",
                table: "Meters",
                column: "FuelTankId");

            migrationBuilder.CreateIndex(
                name: "IX_Meters_StationId",
                table: "Meters",
                column: "StationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meters_FuelTanks_FuelTankId",
                table: "Meters",
                column: "FuelTankId",
                principalTable: "FuelTanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Meters_Stations_StationId",
                table: "Meters",
                column: "StationId",
                principalTable: "Stations",
                principalColumn: "Id");
        }
    }
}
