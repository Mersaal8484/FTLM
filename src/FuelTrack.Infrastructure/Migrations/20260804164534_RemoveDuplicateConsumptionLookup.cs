using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateConsumptionLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Lookups WHERE Id = 59 AND CategoryId = 100 AND Code = 'Consumption')
    DELETE FROM Lookups WHERE Id = 59 AND CategoryId = 100;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Lookups",
                columns: new[] { "Id", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 59, 100, "Consumption", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "استهلاك", 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 });
        }
    }
}
