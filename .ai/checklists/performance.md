# Performance Checklist

## Description
Identify and resolve performance bottlenecks across backend, frontend, database, and network layers.

## Sections

### 1. Backend
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 1.1 | No synchronous blocking on async calls (`.Result`, `.Wait()`) | Critical | Search for `.Result` and `.Wait()` in service code | [ ] |
| 1.2 | `AsNoTracking()` on read-only queries | High | Verify read methods don't track entities | [ ] |
| 1.3 | Pagination implemented on all list endpoints | High | Check `Skip`/`Take` or cursor-based pagination | [ ] |
| 1.4 | Response caching for static/reference data | Medium | Check `[ResponseCache]` or in-memory cache | [ ] |
| 1.5 | No large object allocations in hot paths | Medium | Review for `new List<T>()` in tight loops | [ ] |
| 1.6 | `CancellationToken` propagated through async chains | Medium | Verify token passed to `DbSet` methods | [ ] |

### 2. Frontend
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 2.1 | Bundle size < 500KB gzipped | High | Run `npm run build` and check output size | [ ] |
| 2.2 | Lazy loading for route components | Medium | Check `React.lazy()` or dynamic imports | [ ] |
| 2.3 | `useMemo`/`useCallback` for expensive computations | Medium | Review component re-renders | [ ] |
| 2.4 | Images optimized (WebP, lazy loading, srcset) | Low | Check image formats and `loading="lazy"` | [ ] |
| 2.5 | React Query `staleTime` configured appropriately | Medium | Verify `staleTime` in query config | [ ] |
| 2.6 | No unnecessary re-renders (React DevTools Profiler) | Medium | Profile component render frequency | [ ] |

### 3. Database
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 3.1 | Composite indexes for common WHERE + ORDER BY patterns | High | Check `IX_` indexes in migration history | [ ] |
| 3.2 | No `SELECT *` — only needed columns queried | Medium | Review LINQ projections (`.Select()`) | [ ] |
| 3.3 | Query execution plans reviewed for slow queries | High | Use SQL Server Management Studio execution plans | [ ] |
| 3.4 | Connection pooling configured | Medium | Verify `Max Pool Size` in connection string | [ ] |
| 3.5 | No missing foreign key indexes (FK without index) | High | Check all FK columns have associated indexes | [ ] |
| 3.6 | Large text/LOB columns stored efficiently | Low | Review `nvarchar(max)` vs `nvarchar(n)` usage | [ ] |

### 4. Network
| # | Item | Severity | Verification | Status |
|---|------|----------|--------------|--------|
| 4.1 | Response compression enabled (Brotli/Gzip) | Medium | Check `app.UseResponseCompression()` | [ ] |
| 4.2 | API responses use minimal payload (no over-fetching) | Medium | Verify DTOs return only needed fields | [ ] |
| 4.3 | Static assets served with cache headers | Medium | Check `Cache-Control` headers on frontend assets | [ ] |
| 4.4 | SignalR messages minimized (delta updates) | Low | Review hub message payload sizes | [ ] |

## Quick Reference
- **Critical items:** 1.1
- **Must-pass before merge:** All Critical + 1.2, 1.3, 2.1, 3.1, 3.3, 3.5
- **Nice-to-have:** 1.5, 2.4, 2.6, 3.6, 4.4
