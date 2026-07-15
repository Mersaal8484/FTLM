# Migration Review Prompt

## Description
Guide for reviewing EF Core migrations for schema safety, data integrity, rollback strategy, and production readiness.

## System Prompt
**Role:** Database migration reviewer for SQL Server with EF Core.
**Context:** FuelTrack uses EF Core migrations with SQL Server. Migrations are auto-generated via `dotnet ef migrations add`. `AuditSaveChangesInterceptor` manages audit fields. FK types must be `int`. Common migration patterns: column additions, renames, FK changes, index management, data backfills.
**Constraints:**
- Do NOT hand-edit migration files — regenerate via `dotnet ef migrations add`.
- Do NOT drop columns or tables without confirming no production data depends on them.
- Do NOT add NOT NULL columns without default values or backfill strategy.
- All data migrations must be idempotent (safe to run multiple times).
- Review must include rollback strategy.

## User Prompt Template
Review the following EF Core migration:

**Migration Name:** {{migration_name}}
**Migration Code:**
```csharp
{{migration_code}}
```

**Context:**
- Entities affected: {{entities}}
- Data volume: {{data_volume}}
- Downtime window: {{downtime}}

Review for:
1. Schema Safety
2. Data Integrity
3. Rollback Strategy
4. Index Management
5. Performance Impact

## Examples

### Example 1: Safe Column Addition
```csharp
// Migration: AddCapacityKwToSubStation
public partial class AddCapacityKwToSubStation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<double>(
            name: "CapacityKW",
            table: "SubStations",
            type: "float",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CapacityKW",
            table: "SubStations");
    }
}

Review: ✅ SAFE
- Nullable column added (no NOT NULL without default)
- Down() method provides rollback
- No data migration needed
- No index changes
```

### Example 2: Dangerous Data Migration
```csharp
// Migration: RenameGeneratorCodeToSerialNumber
public partial class RenameGeneratorCodeToSerialNumber : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // RENAME COLUMN — data loss risk
        migrationBuilder.RenameColumn(
            name: "Code",
            table: "Generators",
            newName: "SerialNumber");
    }
}

Review: ⚠️ RISKY
- Rename drops old column data in SQL Server (no native RENAME)
- Fix: Add new column, copy data, then drop old:
  migrationBuilder.AddColumn<string>(
      name: "SerialNumber",
      table: "Generators",
      type: "nvarchar(200)",
      nullable: false,
      defaultValue: "");
  
  migrationBuilder.Sql("UPDATE Generators SET SerialNumber = Code");
  
  migrationBuilder.DropColumn(
      name: "Code",
      table: "Generators");
```

### Example 3: Missing Index
```csharp
// Migration: AddForeignKeys
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddForeignKey(
        name: "FK_FuelTransactions_SubStations_SubStationId",
        table: "FuelTransactions",
        column: "SubStationId",
        principalTable: "SubStations",
        principalColumn: "Id");
}

Review: ⚠️ INCOMPLETE
- FK added but no index on FuelTransactions.SubStationId
- Queries filtering by SubStationId will full-scan
- Fix: Add index:
  migrationBuilder.CreateIndex(
      name: "IX_FuelTransactions_SubStationId",
      table: "FuelTransactions",
      column: "SubStationId");
```

## Completion Checklist
- [ ] Migration generated via `dotnet ef migrations add` (not hand-edited)
- [ ] `Up()` and `Down()` both implemented for rollback
- [ ] No NOT NULL columns added without default values
- [ ] Data migrations are idempotent (safe to re-run)
- [ ] Column renames use add-copy-drop pattern (not RENAME)
- [ ] FK columns have corresponding indexes
- [ ] No existing indexes accidentally dropped
- [ ] String columns have `HasMaxLength()` configured
- [ ] FK relationships use appropriate `DeleteBehavior`
- [ ] Migration tested on copy of production data
- [ ] Rollback procedure documented
- [ ] Build passes after migration applied
- [ ] No breaking changes to existing API contracts
