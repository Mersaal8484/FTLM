using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkPermits_Users_ApprovedById",
                table: "WorkPermits");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkPermits_Users_AssignedToId",
                table: "WorkPermits");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkPermits_Users_ApprovedById",
                table: "WorkPermits",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkPermits_Users_AssignedToId",
                table: "WorkPermits",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkPermits_Users_ApprovedById",
                table: "WorkPermits");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkPermits_Users_AssignedToId",
                table: "WorkPermits");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkPermits_Users_ApprovedById",
                table: "WorkPermits",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkPermits_Users_AssignedToId",
                table: "WorkPermits",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
