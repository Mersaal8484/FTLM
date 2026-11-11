using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Infrastructure.Migrations;

[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261111211500_RepairOilTransactionReference")]
public class RepairOilTransactionReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.OilTransactions', 'Reference') IS NULL
    ALTER TABLE dbo.OilTransactions ADD [Reference] nvarchar(max) NULL;
IF COL_LENGTH('dbo.OilTransactions', 'InvoiceNumber') IS NOT NULL
    EXEC(N'UPDATE dbo.OilTransactions SET [Reference] = [InvoiceNumber] WHERE [Reference] IS NULL AND [InvoiceNumber] IS NOT NULL');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Reverting this repair could discard transaction references.");
    }
}
