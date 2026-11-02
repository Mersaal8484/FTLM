using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCriticalQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_SubStations_StationId",
                table: "SubStations",
                newName: "IX_SubStation_StationId");

            migrationBuilder.RenameIndex(
                name: "IX_Stations_BranchId",
                table: "Stations",
                newName: "IX_Station_BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_Oils_SubStationId",
                table: "Oils",
                newName: "IX_Oil_SubStationId");

            migrationBuilder.RenameIndex(
                name: "IX_Oils_StationId",
                table: "Oils",
                newName: "IX_Oil_StationId");

            migrationBuilder.RenameIndex(
                name: "IX_Meters_SubStationId",
                table: "Meters",
                newName: "IX_Meter_SubStationId");

            migrationBuilder.RenameIndex(
                name: "IX_Generators_SubStationId",
                table: "Generators",
                newName: "IX_Generator_SubStationId");

            migrationBuilder.RenameIndex(
                name: "IX_Controllers_GeneratorId",
                table: "Controllers",
                newName: "IX_Controller_GeneratorId");

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_EntityName",
                table: "AuditLogs",
                column: "EntityName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshToken_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_EntityName",
                table: "AuditLogs");

            migrationBuilder.RenameIndex(
                name: "IX_SubStation_StationId",
                table: "SubStations",
                newName: "IX_SubStations_StationId");

            migrationBuilder.RenameIndex(
                name: "IX_Station_BranchId",
                table: "Stations",
                newName: "IX_Stations_BranchId");

            migrationBuilder.RenameIndex(
                name: "IX_Oil_SubStationId",
                table: "Oils",
                newName: "IX_Oils_SubStationId");

            migrationBuilder.RenameIndex(
                name: "IX_Oil_StationId",
                table: "Oils",
                newName: "IX_Oils_StationId");

            migrationBuilder.RenameIndex(
                name: "IX_Meter_SubStationId",
                table: "Meters",
                newName: "IX_Meters_SubStationId");

            migrationBuilder.RenameIndex(
                name: "IX_Generator_SubStationId",
                table: "Generators",
                newName: "IX_Generators_SubStationId");

            migrationBuilder.RenameIndex(
                name: "IX_Controller_GeneratorId",
                table: "Controllers",
                newName: "IX_Controllers_GeneratorId");

            migrationBuilder.AlterColumn<string>(
                name: "EntityName",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
