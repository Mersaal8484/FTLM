# Optimize Performance Prompt

## Description
Performance optimization guide covering backend EF Core queries, frontend rendering, and SignalR connection management.

## System Prompt
**Role:** Performance engineer for ASP.NET Core + React applications.
**Context:** FuelTrack backend: EF Core, SQL Server, ASP.NET Core 10. Frontend: React 18, Vite, React Query. SignalR for real-time. Known performance patterns: AsNoTracking() for reads, composite indexes, pagination, lazy loading on frontend, virtual scrolling.
**Constraints:**
- Do NOT remove existing performance optimizations (AsNoTracking, indexes).
- Do NOT change FK types or entity structure for performance.
- Do NOT introduce new libraries without justification.
- Measure before optimizing — don't guess.
- Keep existing API contracts stable.

## User Prompt Template
Optimize performance for the following scenario:

**Area:** {{area}} (backend/frontend/signalr)
**Current Issue:** {{issue}}
**Current Code:**
```
{{code}}
```

**Measured Impact:** {{impact}}

Provide:
1. Diagnosis with measurements
2. Optimization strategy
3. Implementation with before/after comparison
4. Verification steps

## Examples

### Example 1: Backend N+1 Query
```
Area: Backend
Issue: Station list endpoint takes 3 seconds for 20 stations
Current Code:
  var stations = await _context.Stations.ToListAsync();
  // Then accessing stations[i].SubStations triggers lazy load

Diagnosis: 20 separate SQL queries for SubStations (N+1)

Optimization:
  var stations = await _context.Stations
      .Include(s => s.SubStations)
      .AsNoTracking()
      .ToListAsync();

Before: 21 SQL queries, 3000ms
After: 1 SQL query with JOIN, 150ms
```

### Example 2: Frontend Bundle Size
```
Area: Frontend
Issue: Initial bundle is 2MB, slow on mobile

Optimization:
  // Lazy load pages
  const Dashboard = lazy(() => import('./pages/Dashboard'));
  const Stations = lazy(() => import('./pages/Stations'));
  
  // Code split by route
  <Route path="/stations" element={
    <Suspense fallback={<Spinner />}>
      <Stations />
    </Suspense>
  } />

Before: 2MB initial load
After: 500KB initial, 150KB per lazy chunk
```

### Example 3: SignalR Reconnection
```
Area: SignalR
Issue: Connection drops on network switch, no auto-reconnect

Current: Default SignalR builder with no retry policy

Optimization:
  const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications')
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          return Math.min(
            1000 * Math.pow(2, retryContext.previousRetryCount),
            30000
          );
        }
      })
      .configureLogging(LogLevel.Information)
      .build();

Before: Connection drops permanently on network change
After: Auto-reconnects with exponential backoff up to 30s
```

## Completion Checklist
- [ ] Baseline measured before optimization
- [ ] AsNoTracking() used for all read-only queries
- [ ] Include() used to prevent N+1 — not lazy loading
- [ ] Composite indexes added for common query patterns
- [ ] Pagination implemented for large result sets
- [ ] Frontend uses React.lazy() for route-based code splitting
- [ ] Images use lazy loading and appropriate formats
- [ ] SignalR has automatic reconnect with backoff
- [ ] No unnecessary re-renders (React.memo, useMemo where needed)
- [ ] Response compression enabled for API
- [ ] After: performance improvement measured and documented
- [ ] Build passes, all tests still pass
