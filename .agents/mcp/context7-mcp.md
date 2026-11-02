# Context7 MCP

## Capabilities

- **Library Documentation**: Fetch up-to-date docs for any library/framework
- **API Reference**: Look up specific API signatures, parameters, return types
- **Code Examples**: Get real code snippets from official documentation
- **Version-Specific Docs**: Query docs for specific library versions
- **Framework Guides**: Setup guides, migration guides, best practices
- **Library Discovery**: Resolve package names to Context7-compatible IDs

## Setup

No authentication required — Context7 is a free documentation service.

1. Verify it works:
   ```powershell
   npx -y @upstash/context7-mcp --help
   ```

2. MCP config already includes Context7 in `mcp-servers.json`

## Usage Examples

| Operation | MCP Tool Call |
|-----------|---------------|
| Resolve library | `mcp__context7__resolve-library-id` with `query`, `libraryName` |
| Query docs | `mcp__context7__query-docs` with `libraryId`, `query` |

## Common Lookups for FuelTrack

| Library | Query |
|---------|-------|
| ASP.NET Core | `mcp__context7__resolve-library-id` with `libraryName: "ASP.NET Core"` |
| Entity Framework Core | `mcp__context7__resolve-library-id` with `libraryName: "Entity Framework Core"` |
| React | `mcp__context7__resolve-library-id` with `libraryName: "React"` |
| Vite | `mcp__context7__resolve-library-id` with `libraryName: "Vite"` |
| SignalR | `mcp__context7__resolve-library-id` with `libraryName: "SignalR"` |
| FluentValidation | `mcp__context7__resolve-library-id` with `libraryName: "FluentValidation"` |
| SQL Server | `mcp__context7__resolve-library-id` with `libraryName: "Microsoft SQL Server"` |
| Docker | `mcp__context7__resolve-library-id` with `libraryName: "Docker"` |
| Kubernetes | `mcp__context7__resolve-library-id` with `libraryName: "Kubernetes"` |

## Workflow

1. **Resolve** the library ID first
2. **Query** with a specific question (not vague terms)
3. Context7 returns current docs with code snippets

```dot
resolve-library-id -> query-docs -> apply to code
```

## Configuration

No environment variables required. Context7 connects to its hosted API.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Library not found | Try alternate names (e.g., "EF Core" instead of "EntityFrameworkCore") |
| No docs returned | Be more specific in query (e.g., "EF Core migration with SQL Server" not "migration") |
| Version mismatch | Specify version in query if needed |
