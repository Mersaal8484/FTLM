# Migration Review Checklist

## Description
Verify EF Core migrations are safe, reversible, performant, and properly documented before applying to any environment.

## Sections

### 1. Schema Changes
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | Migration targets correct DbContext | Critical | Inspect `Up()` method references expected `DbContext` | [ ] |
| 1.2 | Column types match domain model properties | Critical | Compare migration columns to entity definitions | [ ] |
| 1.3 | Primary keys and foreign keys correctly defined | Critical | Verify `HasKey`, `HasForeignKey` in migration | [ ] |
| 1.4 | Required vs nullable columns match entity nullability | High | Check `nullable: true/false` in migration | [ ] |
| 1.5 | Default values specified where appropriate | Medium | Review `defaultValue` / `defaultValueSql` | [ ] |
| 1.6 | String lengths match `HasMaxLength` config | Medium | Compare `nvarchar(max)` vs specific lengths | [ ] |
| 1.7 | No breaking renames without data migration | Critical | Renames must use `RenameColumn` + data copy | [ ] |
| 1.8 | New columns added with safe defaults for existing rows | High | Verify `defaultValue` on non-nullable additions | [ ] |

### 2. Data Safety
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | No data-loss operations (DROP COLUMN, TRUNCATE) without backup | Critical | Review `Down()` for destructive reversals | [ ] |
| 2.2 | Rollback plan documented and tested | High | Verify `Down()` method restores prior state | [ ] |
| 2.3 | Large tables batch data migrations (1000-row chunks) | High | Check for `Take(1000)` in data migration loops | [ ] |
| 2.4 | Sensitive columns not logged in migration diffs | Medium | Verify no passwords/keys in plaintext migrations | [ ] |
| 2.5 | Seed data includes only non-production test records | Medium | Review `InsertData` calls for hardcoded IDs | [ ] |

### 3. Performance
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | Indexes added for new FK columns | High | Check `CreateIndex` calls in migration | [ ] |
| 3.2 | Composite indexes match common query patterns | Medium | Compare to service `Where`/`OrderBy` clauses | [ ] |
| 3.3 | No unnecessary indexes on high-cardinality columns | Medium | Review existing indexes via `SHOW INDEX` | [ ] |
| 3.4 | Column drops remove associated indexes first | Medium | Verify index drop precedes column drop | [ ] |
| 3.5 | `DISABLED`/`ENABLE` index operations for large data loads | Low | Check for `Rebuild` operations post-migration | [ ] |

### 4. Security
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | No sensitive data (PII, tokens) added as plain columns | Critical | Review new columns for encryption needs | [ ] |
| 4.2 | Row-Level Security policies updated if applicable | High | Check RLS policy references new tables/columns | [ ] |
| 4.3 | Connection string permissions unchanged | Medium | Verify migration doesn't alter DB permissions | [ ] |

### 5. Documentation
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 5.1 | Migration name is descriptive (verb + entity) | Medium | Check `Id` timestamp + descriptive suffix | [ ] |
| 5.2 | Migration includes `Down()` for rollback | High | Verify `Down()` reverses all `Up()` operations | [ ] |
| 5.3 | Commit message references migration purpose | Low | Check git log for related issue/task | [ ] |
| 5.4 | AGENTS.md updated if schema affects service layer | Low | Review AGENTS.md session summaries | [ ] |

## Quick Reference
- **Critical items:** 1.1, 1.2, 1.3, 1.7, 2.1, 4.1
- **Must-pass before merge:** All Critical + 2.2, 3.1, 5.2
- **Nice-to-have:** 3.5, 5.3, 5.4
