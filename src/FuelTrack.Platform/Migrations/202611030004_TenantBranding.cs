using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Platform.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("202611030004_TenantBranding")]
public sealed class TenantBranding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(name: "Logo", schema: "platform", table: "Tenants", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LogoContentType", schema: "platform", table: "Tenants", type: "character varying(40)", maxLength: 40, nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Logo", schema: "platform", table: "Tenants");
        migrationBuilder.DropColumn(name: "LogoContentType", schema: "platform", table: "Tenants");
    }
}
