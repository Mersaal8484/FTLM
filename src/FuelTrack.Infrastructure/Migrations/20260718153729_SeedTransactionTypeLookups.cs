using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTransactionTypeLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LookupCategories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "Description", "DisplayName", "IsActive", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 100, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Transaction types for fuel and oil movements", "انواع الحركات", true, "TransactionType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 });

            migrationBuilder.InsertData(
                table: "Lookups",
                columns: new[] { "Id", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 200, 100, "Purchase", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "توريد", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 201, 100, "Sale", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "صادر", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 202, 100, "Transfer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "نقل", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 203, 100, "TransferOut", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "نقل صادر", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 204, 100, "TransferIn", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "نقل وارد", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 205, 100, "Loss", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "فاقد", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 206, 100, "Consumption", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "استهلاك", 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 207, 100, "Adjustment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "تسوية", 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 201);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 202);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 203);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 204);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 205);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 206);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 207);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 100);
        }
    }
}
