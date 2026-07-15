# SQL MCP

## Capabilities

- **Query Execution**: Run SELECT, INSERT, UPDATE, DELETE against SQL Server
- **Schema Inspection**: List tables, columns, indexes, constraints, foreign keys
- **Migration Management**: View EF Core migrations, check migration history
- **Data Exploration**: Browse table contents, row counts, column types
- **Stored Procedures**: Execute and inspect stored procedures
- **Index Analysis**: Review indexes, detect missing indexes, check fragmentation

## Setup

1. Set connection string environment variable:
   ```powershell
   $env:MSSQL_CONNECTION_STRING = "Server=tcp:LAPTOP-HAITHAM,8749;Database=FuelTrackDb;Trusted_Connection=True;TrustServerCertificate=True"
   ```

2. For persistence, add to PowerShell profile:
   ```powershell
   [Environment]::SetEnvironmentVariable("MSSQL_CONNECTION_STRING", "Server=tcp:LAPTOP-HAITHAM,8749;Database=FuelTrackDb;Trusted_Connection=True;TrustServerCertificate=True", "User")
   ```

3. Verify SQL Server connectivity:
   ```powershell
   sqlcmd -S "tcp:LAPTOP-HAITHAM,8749" -Q "SELECT 1"
   ```

## Usage Examples

| Operation | MCP Tool Call |
|-----------|---------------|
| List databases | `mcp__sql__list_databases` |
| List tables | `mcp__sql__list_tables` with `database` |
| Query data | `mcp__sql__query` with `sql` (SELECT statement) |
| Get table schema | `mcp__sql__get_schema` with `table` |
| Check migrations | Query `__EFMigrationsHistory` table |
| Row count | `mcp__sql__query` with `SELECT COUNT(*) FROM [table]` |

## FuelTrack Specific Queries

```sql
-- List all migrations applied
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;

-- Table row counts
SELECT t.NAME AS TableName, p.rows AS RowCounts
FROM sys.tables t INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE p.index_id IN (0,1) ORDER BY p.rows DESC;

-- Check FK relationships
SELECT
    fk.name AS FKName,
    tp.name AS ParentTable,
    cp.name AS ParentColumn,
    tr.name AS ReferenceTable,
    cr.name AS ReferenceColumn
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id;
```

## Configuration

| Variable | Required | Description |
|----------|----------|-------------|
| `MSSQL_CONNECTION_STRING` | Yes | Full SQL Server connection string |

## Troubleshooting

| Issue | Fix |
|-------|-----|
| TCP connection refused | Verify port (8749 for this SQL Server), check `HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL` |
| Login failed | Use Windows Auth (`Trusted_Connection=True`) or check SQL auth credentials |
| Trust server cert error | Add `TrustServerCertificate=True` to connection string |
