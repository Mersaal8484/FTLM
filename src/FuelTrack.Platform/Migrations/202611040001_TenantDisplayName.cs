using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Platform.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("202611040001_TenantDisplayName")]
public sealed class TenantDisplayName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "DisplayName", schema: "platform", table: "Tenants", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.Sql("UPDATE platform.\"Tenants\" SET \"DisplayName\" = \"Name\" WHERE \"DisplayName\" IS NULL");
        migrationBuilder.AlterColumn<string>(name: "DisplayName", schema: "platform", table: "Tenants", type: "character varying(200)", maxLength: 200, nullable: false, oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200, oldNullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "DisplayName", schema: "platform", table: "Tenants");
}
