using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedReadingStatusLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add columns if they don't already exist (they may have been created in InitialSchema).
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'CorrectedFromId')
    ALTER TABLE OilTransactions ADD CorrectedFromId INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'CorrectionReason')
    ALTER TABLE OilTransactions ADD CorrectionReason NVARCHAR(MAX) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FuelTransactions') AND name = 'CorrectedFromId')
    ALTER TABLE FuelTransactions ADD CorrectedFromId INT NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FuelTransactions') AND name = 'CorrectionReason')
    ALTER TABLE FuelTransactions ADD CorrectionReason NVARCHAR(MAX) NULL;
");

            migrationBuilder.Sql(@"
SET IDENTITY_INSERT LookupCategories ON;
IF NOT EXISTS (SELECT * FROM LookupCategories WHERE Id = 19)
BEGIN
    INSERT INTO LookupCategories (Id, Name, DisplayName, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt)
    VALUES (19, 'ReadingStatus', N'حالة الموقع', N'Reading status: Draft, Pending, Submitted, Approved, Rejected', 1, '2026-01-01', 0, '2026-01-01', 0, NULL);
END
SET IDENTITY_INSERT LookupCategories OFF;

SET IDENTITY_INSERT Lookups ON;
MERGE Lookups AS target
USING (VALUES
    (55, N'Draft',     N'مسودة', 1),
    (56, N'Pending',   N'معلق',  2),
    (57, N'Submitted', N'مرسل',  3),
    (58, N'Approved',  N'معتمد', 4),
    (59, N'Rejected',  N'مرفوض', 5)
) AS src(Id, Code, Name, SortOrder)
ON target.Id = src.Id
WHEN NOT MATCHED THEN
    INSERT (Id, CategoryId, Code, Name, SortOrder, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt)
    VALUES (src.Id, 19, src.Code, src.Name, src.SortOrder, 1, '2026-01-01', 0, '2026-01-01', 0, NULL);
SET IDENTITY_INSERT Lookups OFF;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 19);
        }
    }
}
