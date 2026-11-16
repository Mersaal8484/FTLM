using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Infrastructure.Migrations;

[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261116120000_AddTankRoleAndHorizontalShape")]
public class AddTankRoleAndHorizontalShape : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        // TankRole column: nullable int, additive — no existing data affected.
        // NULL = enterprise (no role constraint).
        // 1 = Main, 2 = Daily (matches TankRole enum).
        migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.FuelTanks', 'TankRole') IS NULL
    ALTER TABLE dbo.FuelTanks ADD [TankRole] int NULL;");

        // TankShape already has column Shape (int?) in the existing schema.
        // Value 1 = HorizontalCylinderWithConicalEnds added to enum only.
        // No schema change required for TankShape — column already exists.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer") return;

        migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.FuelTanks', 'TankRole') IS NOT NULL
    ALTER TABLE dbo.FuelTanks DROP COLUMN [TankRole];");
    }
}
