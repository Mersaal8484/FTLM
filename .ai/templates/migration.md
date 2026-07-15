# Template: EF Core Migration

## Metadata
- **Name**: migration
- **Purpose**: EF Core migration file
- **Layer**: Infrastructure
- **Path**: `src/FuelTrack.Infrastructure/Migrations/{Timestamp}_{Name}.cs`

## Template

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class {Name} : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema changes
            migrationBuilder.AddColumn<int>(
                name: "NewColumn",
                table: "Entities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Index additions
            migrationBuilder.CreateIndex(
                name: "IX_Entities_NewColumn",
                table: "Entities",
                column: "NewColumn");

            // FK additions
            migrationBuilder.AddForeignKey(
                name: "FK_Entities_RelatedEntities_RelatedEntityId",
                table: "Entities",
                column: "RelatedEntityId",
                principalTable: "RelatedEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse operations in opposite order
            migrationBuilder.DropForeignKey(
                name: "FK_Entities_RelatedEntities_RelatedEntityId",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_Entities_NewColumn",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "NewColumn",
                table: "Entities");
        }
    }
}
```

## Checklist
- [ ] `Up()` has all schema changes
- [ ] `Down()` reverses all changes in opposite order
- [ ] Index naming: `IX_{Table}_{Columns}`
- [ ] FK naming: `FK_{Table}_{RelatedTable}_{Column}`
- [ ] No data migration in schema migration (split if needed)
- [ ] Build succeeds after migration
- [ ] Tests pass after migration
