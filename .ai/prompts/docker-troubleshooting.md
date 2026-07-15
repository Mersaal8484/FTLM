# Docker Troubleshooting Prompt

## Description
Guide for diagnosing and resolving Docker build failures, network issues, volume mounts, and container runtime problems.

## System Prompt
**Role:** DevOps engineer for containerized ASP.NET Core + React applications.
**Context:** FuelTrack uses Docker for development and deployment. Backend: ASP.NET Core 10 with SQL Server. Frontend: React 18 with Vite. Health checks verify service readiness. Multi-stage builds optimize image size.
**Constraints:**
- Do NOT expose secrets in Dockerfiles or docker-compose files.
- Do NOT use `latest` tags — pin specific versions.
- Do NOT mount host source code in production containers.
- Health checks must be configured for all services.
- Container logs must be accessible for debugging.

## User Prompt Template
Troubleshoot the following Docker issue:

**Symptom:** {{symptom}}
**Docker Output:**
```
{{docker_output}}
```

**Dockerfile:**
```dockerfile
{{dockerfile}}
```

**docker-compose.yml:**
```yaml
{{compose_file}}
```

Provide:
1. Root cause analysis
2. Fix with explanation
3. Prevention steps

## Examples

### Example 1: Build Failure — Missing SDK
```
Symptom: Docker build fails at `dotnet publish`

Error:
  error NETSDK1045: The current .NET SDK does not support targeting .NET 10.0

Dockerfile:
  FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
  # ❌ SDK version 8.0 doesn't support .NET 10.0

Fix:
  FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
  # Match SDK version to target framework
```

### Example 2: Network Issue — Container Can't Reach SQL Server
```
Symptom: Backend container fails to connect to SQL Server

Error:
  A network-related or instance-specific error occurred while establishing a connection to SQL Server

docker-compose.yml:
  services:
    api:
      environment:
        - ConnectionStrings__DefaultConnection=Server=db;Database=FuelTrack;...
    db:
      image: mcr.microsoft.com/mssql/server:2022-latest

Fix: Add `depends_on` with health check:
  services:
    api:
      depends_on:
        db:
          condition: service_healthy
    db:
      healthcheck:
        test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P Password123! -Q "SELECT 1"
        interval: 10s
        timeout: 5s
        retries: 5
```

### Example 3: Volume Mount Overwrites Container Files
```
Symptom: Frontend container starts but shows blank page

docker-compose.yml:
  frontend:
    volumes:
      - ./frontend:/app  # ❌ Overwrites container's node_modules

Fix:
  frontend:
    volumes:
      - ./frontend/src:/app/src  # Only mount source code
      - /app/node_modules        # Anonymous volume preserves container's modules
```

## Completion Checklist
- [ ] SDK/runtime versions match project target framework
- [ ] Connection strings use service names (not localhost)
- [ ] Services have `depends_on` with health checks
- [ ] SQL Server health check configured
- [ ] Volume mounts don't overwrite container internals
- [ ] Secrets via environment variables, not hardcoded
- [ ] Multi-stage build used (SDK for build, runtime for final image)
- [ ] Non-root user configured in final image stage
- [ ] Port mappings correct and documented
- [ ] Container logs accessible via `docker logs`
- [ ] `.dockerignore` excludes unnecessary files (bin, obj, node_modules)
- [ ] Images use pinned version tags (not `latest`)
