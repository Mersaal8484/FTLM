# ADR-0001: Technology Stack Selection

## Status
Accepted

## Context
Rebuild FuelTrack-PEC from scratch with unified backend.

## Decision
- Backend: ASP.NET Core 10.0 LTS
- Database: SQL Server 2022
- ORM: Entity Framework Core 10.0
- Realtime: SignalR 10.0
- Auth: Microsoft.AspNetCore.Authentication.JwtBearer 10.0.x
- Frontend: React (existing codebase)
- Mobile: Flutter (existing codebase)

## Consequences
- Single backend technology stack
- Unified realtime communication
- SQL Server licensing considerations