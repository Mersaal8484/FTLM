# ADR-0002: Modbus Communication Strategy

## Status
Accepted

## Context
FuelTrack-PEC uses a Node.js HTTP poller to read Modbus RTU/TCP registers from fuel tanks and generators. The rebuild requires a decision on whether to keep this Node.js service or rewrite it as a .NET service for stack unification.

## Decision
Rewrite the Modbus polling service as a .NET Worker Service (`FuelTrack.ModbusGateway`). This service will:

1. Run as a background `BackgroundService` / `IHostedService` within the FuelTrack solution
2. Use `EasyModbus` or `NModbus4` .NET library for Modbus RTU/TCP communication
3. Poll configured Modbus registers on a configurable interval (default 5 seconds)
4. Push readings to the API via SignalR hub or direct EF Core writes
5. Share the `FuelTrack.Domain` and `FuelTrack.Infrastructure` projects for models and data access

## Consequences

### Positive
- Unified .NET stack — one language, one build system, one deployment model
- Shared domain models and DbContext with the API (no HTTP bridge needed)
- Native SignalR integration for real-time data push
- Better observability via .NET logging, OpenTelemetry, and Aspire dashboard

### Negative
- Existing Node.js poller code cannot be directly reused (must be rewritten)
- .NET Modbus libraries have a smaller ecosystem than Node.js alternatives

## Alternatives Considered
1. **Keep Node.js poller** — rejected due to stack fragmentation, HTTP bridge complexity, and deployment overhead
2. **Rust poller** — rejected as over-engineering for a CRUD polling service
3. **Azure IoT Edge module** — rejected due to offline-capability requirements at remote substations

## Implementation Plan
1. Create `src/FuelTrack.ModbusGateway/FuelTrack.ModbusGateway.csproj` (Worker SDK, net10.0)
2. Add `EasyModbus` package reference
3. Implement `ModbusPollingService : BackgroundService` with configurable device registry
4. Configure polling intervals, register maps, and error handling per device type (tank gauge, generator controller)
5. Publish readings via SignalR hub for real-time dashboard updates
6. Persist readings to `ModbusReading` ledger table via Infrastructure layer

## Dependencies
- ADR-0001: ASP.NET Core 10.0 LTS tech stack (this service uses the same runtime)
- ADR-0003 (future): Modbus register mapping specification
