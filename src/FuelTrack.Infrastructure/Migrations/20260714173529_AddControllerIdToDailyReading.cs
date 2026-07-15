using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControllerIdToDailyReading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Controllers_GeneratorId",
                table: "Controllers");

            migrationBuilder.AddColumn<int>(
                name: "ControllerId",
                table: "DailyReadings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReading_ControllerId",
                table: "DailyReadings",
                column: "ControllerId");

            migrationBuilder.CreateIndex(
                name: "IX_Controllers_GeneratorId",
                table: "Controllers",
                column: "GeneratorId",
                unique: true,
                filter: "[GeneratorId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyReadings_Controllers_ControllerId",
                table: "DailyReadings",
                column: "ControllerId",
                principalTable: "Controllers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyReadings_Controllers_ControllerId",
                table: "DailyReadings");

            migrationBuilder.DropIndex(
                name: "IX_DailyReading_ControllerId",
                table: "DailyReadings");

            migrationBuilder.DropIndex(
                name: "IX_Controllers_GeneratorId",
                table: "Controllers");

            migrationBuilder.DropColumn(
                name: "ControllerId",
                table: "DailyReadings");

            migrationBuilder.CreateIndex(
                name: "IX_Controllers_GeneratorId",
                table: "Controllers",
                column: "GeneratorId");
        }
    }
}
