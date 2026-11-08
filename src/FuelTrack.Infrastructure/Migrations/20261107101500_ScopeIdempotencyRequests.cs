using System;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations;

/// <summary>
/// Corrects the Phase 1 idempotency boundary: a client key is unique only
/// within the trusted tenant and authenticated-user scope that owns it.
/// </summary>
[DbContext(typeof(FuelTrackDbContext))]
[Migration("20261107101500_ScopeIdempotencyRequests")]
public sealed class ScopeIdempotencyRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_IdempotencyRequests_ClientRequestId",
            table: "IdempotencyRequests");

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "IdempotencyRequests",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<int>(
            name: "UserId",
            table: "IdempotencyRequests",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "UX_IdempotencyRequests_Tenant_User_ClientRequestId",
            table: "IdempotencyRequests",
            columns: new[] { "TenantId", "UserId", "ClientRequestId" },
            unique: true,
            filter: "[DeletedAt] IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_IdempotencyRequests_Tenant_User_ClientRequestId",
            table: "IdempotencyRequests");

        migrationBuilder.DropColumn(name: "TenantId", table: "IdempotencyRequests");
        migrationBuilder.DropColumn(name: "UserId", table: "IdempotencyRequests");

        migrationBuilder.CreateIndex(
            name: "UX_IdempotencyRequests_ClientRequestId",
            table: "IdempotencyRequests",
            column: "ClientRequestId",
            unique: true,
            filter: "[DeletedAt] IS NULL");
    }
}
