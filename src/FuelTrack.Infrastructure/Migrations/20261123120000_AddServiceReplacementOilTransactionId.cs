using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Infrastructure.Migrations;

[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261123120000_AddServiceReplacementOilTransactionId")]
public class AddServiceReplacementOilTransactionId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.AddColumn<int>(
            name: "OilTransactionId",
            table: "ServiceReplacementRecords",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ServiceReplacementRecords_OilTransactionId",
            table: "ServiceReplacementRecords",
            column: "OilTransactionId");

        migrationBuilder.AddForeignKey(
            name: "FK_ServiceReplacementRecords_OilTransactions_OilTransactionId",
            table: "ServiceReplacementRecords",
            column: "OilTransactionId",
            principalTable: "OilTransactions",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.DropForeignKey(
            name: "FK_ServiceReplacementRecords_OilTransactions_OilTransactionId",
            table: "ServiceReplacementRecords");

        migrationBuilder.DropIndex(
            name: "IX_ServiceReplacementRecords_OilTransactionId",
            table: "ServiceReplacementRecords");

        migrationBuilder.DropColumn(
            name: "OilTransactionId",
            table: "ServiceReplacementRecords");
    }
}
