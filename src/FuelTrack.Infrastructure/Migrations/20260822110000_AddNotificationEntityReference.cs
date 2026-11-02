using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations;

public partial class AddNotificationEntityReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RelatedEntityType",
            table: "Notifications",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "RelatedEntityId",
            table: "Notifications",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_Alert_Deduplication",
            table: "Notifications",
            columns: new[] { "NotificationType", "RelatedEntityType", "RelatedEntityId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Notifications_Alert_Deduplication", table: "Notifications");
        migrationBuilder.DropColumn(name: "RelatedEntityType", table: "Notifications");
        migrationBuilder.DropColumn(name: "RelatedEntityId", table: "Notifications");
    }
}
