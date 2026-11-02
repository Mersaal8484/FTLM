using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferFieldsToOilTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationSubStationId",
                table: "OilTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceSubStationId",
                table: "OilTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_DestinationSubStationId",
                table: "OilTransactions",
                column: "DestinationSubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_SourceSubStationId",
                table: "OilTransactions",
                column: "SourceSubStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OilTransactions_SubStations_DestinationSubStationId",
                table: "OilTransactions",
                column: "DestinationSubStationId",
                principalTable: "SubStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OilTransactions_SubStations_SourceSubStationId",
                table: "OilTransactions",
                column: "SourceSubStationId",
                principalTable: "SubStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OilTransactions_SubStations_DestinationSubStationId",
                table: "OilTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_OilTransactions_SubStations_SourceSubStationId",
                table: "OilTransactions");

            migrationBuilder.DropIndex(
                name: "IX_OilTransactions_DestinationSubStationId",
                table: "OilTransactions");

            migrationBuilder.DropIndex(
                name: "IX_OilTransactions_SourceSubStationId",
                table: "OilTransactions");

            migrationBuilder.DropColumn(
                name: "DestinationSubStationId",
                table: "OilTransactions");

            migrationBuilder.DropColumn(
                name: "SourceSubStationId",
                table: "OilTransactions");
        }
    }
}
