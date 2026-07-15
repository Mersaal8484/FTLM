# Docker MCP

## Capabilities

- **Container Management**: List, start, stop, remove, inspect containers
- **Image Operations**: Pull, build, list, remove Docker images
- **Compose**: Run docker-compose up/down, manage multi-container stacks
- **Logs**: Stream container logs, filter by time/stream
- **Exec**: Run commands inside running containers
- **Volume/Network**: Manage volumes, networks, and their configurations
- **Dockerfile Analysis**: Parse and validate Dockerfiles

## Setup

1. Ensure Docker Desktop is running on Windows
2. Verify Docker is available:
   ```powershell
   docker --version
   docker ps
   ```
3. No additional auth needed for local Docker daemon

## Usage Examples

| Operation | MCP Tool Call |
|-----------|---------------|
| List running containers | `mcp__docker__list_containers` |
| Build image | `mcp__docker__build_image` with `context`, `dockerfile` |
| Run container | `mcp__docker__run_container` with `image`, `command` |
| Get logs | `mcp__docker__get_container_logs` with `container_id` |
| Execute command | `mcp__docker__exec_in_container` with `container_id`, `command` |
| List images | `mcp__docker__list_images` |

## FuelTrack Specific

```powershell
# Build and run FuelTrack backend
docker build -t fueltrack-api -f src/FuelTrack.API/Dockerfile .
docker run -e "ConnectionStrings__DefaultConnection=..." -p 5000:8080 fueltrack-api

# Run full stack
docker-compose -f docker-compose.yml up -d

# View logs
docker logs -f fueltrack-api
```

## Configuration

| Variable | Required | Description |
|----------|----------|-------------|
| `DOCKER_HOST` | No | Docker daemon socket (default: `npipe:////./pipe/docker_engine`) |
| `DOCKER_TLS_VERIFY` | No | Enable TLS verification |

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Docker daemon not running | Start Docker Desktop |
| Permission denied | Run PowerShell as admin, or add user to `docker-users` group |
| Build context too large | Add `.dockerignore` to exclude `node_modules`, `bin`, `obj` |
