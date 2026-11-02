using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWarehouseReceiptColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarehouseReceiptDate",
                table: "OilTransactions");

            migrationBuilder.DropColumn(
                name: "WarehouseReceiptNumber",
                table: "OilTransactions");

            migrationBuilder.DropColumn(
                name: "WarehouseQuantity",
                table: "FuelTransactions");

            migrationBuilder.DropColumn(
                name: "WarehouseReceiptDate",
                table: "FuelTransactions");

            migrationBuilder.DropColumn(
                name: "WarehouseReceiptNumber",
                table: "FuelTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WarehouseReceiptNumber",
                table: "FuelTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarehouseReceiptDate",
                table: "FuelTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WarehouseQuantity",
                table: "FuelTransactions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseReceiptNumber",
                table: "OilTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarehouseReceiptDate",
                table: "OilTransactions",
                type: "datetime2",
                nullable: true);
        }
    }
}
