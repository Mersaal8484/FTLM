# FuelTrack-PEC Rebuild

Rebuild of FuelTrack-PEC system using ASP.NET Core, SQL Server, React frontend, and Flutter mobile.

## Architecture Decisions

See `docs/` for Architecture Decision Records (ADRs).

## Getting Started

1. Clone repository
2. Setup development environment (see DEVELOPMENT.md)
3. Run `dotnet restore` and `npm install`
4. Configure connection strings in appsettings.Development.json
5. Run migrations: `dotnet ef database update`
6. Start API and frontend
