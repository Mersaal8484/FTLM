# GitHub MCP

## Capabilities

- **Issue Management**: Create, update, search, and comment on issues
- **Pull Request Operations**: Create PRs, review diffs, merge, manage reviews
- **Repository Access**: Browse files, read contents, search code
- **GitHub Actions**: Trigger workflows, check run status, view logs
- **Branch Management**: Create/delete branches, list refs
- **Release Management**: Create releases, upload assets
- **Code Search**: Search repositories, find code patterns
- **Team/Org Operations**: Manage teams, members, permissions

## Setup

1. Generate a GitHub Personal Access Token:
   - Go to Settings > Developer settings > Personal access tokens > Fine-grained tokens
   - Select scopes: `repo`, `read:org`, `workflow` (as needed)
   - Copy the token

2. Set environment variable:
   ```powershell
   $env:GITHUB_TOKEN = "ghp_your_token_here"
   ```

3. Add to your PowerShell profile for persistence:
   ```powershell
   [Environment]::SetEnvironmentVariable("GITHUB_TOKEN", "ghp_your_token_here", "User")
   ```

## Usage Examples

| Operation | MCP Tool Call |
|-----------|---------------|
| List repo issues | `mcp__github__list_issues` with `owner`, `repo` |
| Create issue | `mcp__github__create_issue` with `owner`, `repo`, `title`, `body` |
| Get PR diff | `mcp__github__get_pull_request_diff` with `owner`, `repo`, `pull_number` |
| Search code | `mcp__github__search_code` with `q` (query string) |
| List repo files | `mcp__github__get_file_contents` with path `"/"` |
| Create branch | `mcp__github__create_ref` with `ref`, `sha` |

## Configuration

| Variable | Required | Description |
|----------|----------|-------------|
| `GITHUB_TOKEN` | Yes | GitHub Personal Access Token |
| `GITHUB_REPO` | No | Default `owner/repo` (avoids repeated params) |

## Troubleshooting

| Issue | Fix |
|-------|-----|
| 401 Unauthorized | Token expired or missing scopes — regenerate with correct permissions |
| Rate limited | GitHub API rate limit hit — wait or use authenticated requests (5000/hr) |
| Not found | Check repo name, owner, and token has access to the repo |
