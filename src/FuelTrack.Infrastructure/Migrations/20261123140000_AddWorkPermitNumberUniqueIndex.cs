using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Infrastructure.Migrations;

[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261123140000_AddWorkPermitNumberUniqueIndex")]
public class AddWorkPermitNumberUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.AlterColumn<string>(
            name: "PermitNumber",
            table: "WorkPermits",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)");

        migrationBuilder.CreateIndex(
            name: "UX_WorkPermits_PermitNumber",
            table: "WorkPermits",
            column: "PermitNumber",
            unique: true,
            filter: "[DeletedAt] IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.DropIndex(
            name: "UX_WorkPermits_PermitNumber",
            table: "WorkPermits");

        migrationBuilder.AlterColumn<string>(
            name: "PermitNumber",
            table: "WorkPermits",
            type: "nvarchar(max)",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100);
    }
}
