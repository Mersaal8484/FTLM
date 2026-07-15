# MCP Setup — FuelTrack-Rebuild

## Overview

Model Context Protocol (MCP) servers extend AI capabilities by providing tools for GitHub, Docker, SQL Server, Playwright, and documentation lookup. This document covers configuration, setup, and usage.

## Table of Contents

- [Available Servers](#available-servers)
- [Setup Instructions](#setup-instructions)
- [Usage Examples](#usage-examples)
- [Troubleshooting](#troubleshooting)
- [Quick Reference](#quick-reference)

---

## Available Servers

**Config file:** `.ai/mcp/mcp-servers.json`

| Server | Package | Purpose |
|--------|---------|---------|
| `github` | `@modelcontextprotocol/server-github` | Issues, PRs, code search, Actions |
| `docker` | `@modelcontextprotocol/server-docker` | Container management |
| `sql` | `@modelcontextprotocol/server-mssql` | SQL Server queries |
| `playwright` | `@anthropic/mcp-playwright` | Browser automation, E2E testing |
| `context7` | `@upstash/context7-mcp` | Live library documentation lookup |

---

## Setup Instructions

### Prerequisites

- Node.js 18+ installed
- npm or npx available
- PowerShell (Windows)

### 1. GitHub MCP

```powershell
# Generate token at: GitHub → Settings → Developer settings → Personal access tokens
# Required scopes: repo, read:org, workflow

# Set for current session
$env:GITHUB_TOKEN = "ghp_your_token_here"

# Set permanently (PowerShell profile)
[Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "ghp_your_token_here", "User")
```

### 2. SQL Server MCP

```powershell
# Set connection string for current session
$env:MSSQL_CONNECTION_STRING = "Server=tcp:SERVERNAME,PORT;Database=FuelTrack;Integrated Security=true;TrustServerCertificate=true"

# Set permanently
[Environment]::SetEnvironmentVariable("MSSQL_CONNECTION_STRING", "your_connection_string", "User")
```

**Current project connection:** `Server=tcp:LAPTOP-HAITHAM,8749;Database=FuelTrack;Integrated Security=true;TrustServerCertificate=true`

### 3. Docker MCP

No environment variables required. Docker Desktop must be running.

### 4. Playwright MCP

No environment variables required. First run downloads browser binaries.

### 5. Context7 MCP

No environment variables required. Provides live documentation for libraries.

---

## Usage Examples

### GitHub

| Operation | Tool | Parameters |
|-----------|------|------------|
| List issues | `mcp__github__list_issues` | `owner`, `repo` |
| Create issue | `mcp__github__create_issue` | `owner`, `repo`, `title`, `body` |
| Get PR diff | `mcp__github__get_pull_request_diff` | `owner`, `repo`, `pull_number` |
| Search code | `mcp__github__search_code` | `q` (query string) |
| List files | `mcp__github__get_file_contents` | `path` = `"/"` |
| Create branch | `mcp__github__create_ref` | `ref`, `sha` |

### SQL Server

| Operation | Tool | Parameters |
|-----------|------|------------|
| Query data | `mcp__sql__query` | `connection_string`, `query` |
| List tables | `mcp__sql__list_tables` | `connection_string` |
| Describe table | `mcp__sql__describe_table` | `connection_string`, `table_name` |

### Playwright

| Operation | Tool | Parameters |
|-----------|------|------------|
| Navigate | `mcp__playwright__navigate` | `url` |
| Click element | `mcp__playwright__click` | `selector` |
| Take screenshot | `mcp__playwright__screenshot` | `path` |
| Fill form | `mcp__playwright__fill` | `selector`, `value` |

### Context7

| Operation | Tool | Parameters |
|-----------|------|------------|
| Resolve library | `resolve-library-id` | `libraryName`, `query` |
| Query docs | `query-docs` | `libraryId`, `query` |

---

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| 401 Unauthorized | Token expired/missing | Regenerate GitHub token with correct scopes |
| Rate limited | API rate limit hit | Wait or use authenticated requests (5000/hr GitHub) |
| Not found | Wrong repo/owner | Check repo name and token access |
| SQL connection failed | Wrong port/protocol | Use `tcp:SERVERNAME,PORT` format |
| Docker not running | Docker Desktop stopped | Start Docker Desktop |
| Playwright timeout | Browser not installed | Run `npx playwright install` |
| Context7 empty | Library not indexed | Try different query or check library name |

---

## Quick Reference

| Need | Server | Tool Pattern |
|------|--------|--------------|
| Check GitHub issues | `github` | `mcp__github__list_issues` |
| Create PR | `github` | `mcp__github__create_pull_request` |
| Query database | `sql` | `mcp__sql__query` |
| Check table schema | `sql` | `mcp__sql__describe_table` |
| Run browser test | `playwright` | `mcp__playwright__navigate` + `mcp__playwright__click` |
| Look up library docs | `context7` | `resolve-library-id` → `query-docs` |
| Check container status | `docker` | `mcp__docker__list_containers` |

## Related Files

- `.ai/mcp/mcp-servers.json` — Server configuration
- `.ai/mcp/github-mcp.md` — GitHub MCP detailed docs
- `AI_WORKFLOW.md` — When to use MCP tools in workflow
