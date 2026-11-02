using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FuelTrack.Platform.Migrations;

[DbContext(typeof(PlatformDbContext))]
[Migration("202611030002_Subscriptions")]
public sealed class Subscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "Plans", schema: "platform", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
            Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Plans", x => x.Id));
        migrationBuilder.CreateTable(name: "Subscriptions", schema: "platform", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            TenantId = table.Column<Guid>(type: "uuid", nullable: false),
            PlanId = table.Column<Guid>(type: "uuid", nullable: false),
            StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Subscriptions", x => x.Id);
            table.ForeignKey(name: "FK_Subscriptions_Tenants_TenantId", column: x => x.TenantId,
                principalSchema: "platform", principalTable: "Tenants", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey(name: "FK_Subscriptions_Plans_PlanId", column: x => x.PlanId,
                principalSchema: "platform", principalTable: "Plans", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        });
        migrationBuilder.CreateIndex("IX_Subscriptions_PlanId", "Subscriptions", "PlanId", schema: "platform");
        migrationBuilder.CreateIndex("IX_Subscriptions_TenantId_StartsAt", "Subscriptions", new[] { "TenantId", "StartsAt" }, schema: "platform");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Subscriptions", "platform");
        migrationBuilder.DropTable("Plans", "platform");
    }
}
