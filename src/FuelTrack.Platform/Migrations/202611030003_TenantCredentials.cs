using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Platform.Migrations;
[DbContext(typeof(PlatformDbContext))]
[Migration("202611030003_TenantCredentials")]
public sealed class TenantCredentials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "TenantCredentials", schema: "platform", columns: table => new
        {
            TenantId = table.Column<Guid>(type: "uuid", nullable: false),
            KeyId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Nonce = table.Column<byte[]>(type: "bytea", nullable: false),
            Ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
            Tag = table.Column<byte[]>(type: "bytea", nullable: false),
            UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_TenantCredentials", x => x.TenantId);
            table.ForeignKey(name: "FK_TenantCredentials_Tenants_TenantId", column: x => x.TenantId,
                principalSchema: "platform", principalTable: "Tenants", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        });
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("TenantCredentials", "platform");
}
