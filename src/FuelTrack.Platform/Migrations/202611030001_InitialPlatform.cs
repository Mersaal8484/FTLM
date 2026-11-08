using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Platform.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("202611030001_InitialPlatform")]
public sealed class InitialPlatform : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("platform");
        migrationBuilder.CreateTable(
            name: "Tenants", schema: "platform",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Host = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Tenants", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Tenants_Host", schema: "platform", table: "Tenants", column: "Host", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "Tenants", schema: "platform");
}
