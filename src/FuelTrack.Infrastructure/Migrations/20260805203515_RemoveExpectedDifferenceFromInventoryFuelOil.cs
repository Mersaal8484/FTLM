using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExpectedDifferenceFromInventoryFuelOil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "DifferenceLiters",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PeriodicInventoryOils");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "DifferenceLiters",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PeriodicInventoryFuels");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PeriodicInventoryFuels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PeriodicInventoryOils",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "PeriodicInventoryOils",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PeriodicInventoryOils",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DifferenceLiters",
                table: "PeriodicInventoryOils",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryOils",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PeriodicInventoryOils",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PeriodicInventoryOils",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "PeriodicInventoryOils",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PeriodicInventoryFuels",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "PeriodicInventoryFuels",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PeriodicInventoryFuels",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DifferenceLiters",
                table: "PeriodicInventoryFuels",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedStockLiters",
                table: "PeriodicInventoryFuels",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PeriodicInventoryFuels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PeriodicInventoryFuels",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "PeriodicInventoryFuels",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
