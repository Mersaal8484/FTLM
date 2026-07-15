# Playwright MCP

## Capabilities

- **Browser Automation**: Navigate pages, click elements, fill forms, take screenshots
- **E2E Testing**: Write and run end-to-end tests against web applications
- **Visual Regression**: Capture and compare page screenshots for visual changes
- **Network Interception**: Mock API responses, intercept requests
- **Accessibility Testing**: Run axe-core audits, check WCAG compliance
- **Multi-browser**: Test across Chromium, Firefox, WebKit
- **PDF Generation**: Export pages as PDFs

## Setup

1. Install Playwright browsers (first time only):
   ```powershell
   npx playwright install
   ```

2. For FuelTrack frontend testing, ensure the dev server is running:
   ```powershell
   cd frontend
   npm run dev
   ```

## Usage Examples

| Operation | MCP Tool Call |
|-----------|---------------|
| Navigate to URL | `mcp__playwright__navigate` with `url` |
| Click element | `mcp__playwright__click` with `selector` |
| Fill input | `mcp__playwright__fill` with `selector`, `value` |
| Take screenshot | `mcp__playwright__screenshot` with optional `selector` |
| Get page content | `mcp__playwright__get_content` |
| Wait for selector | `mcp__playwright__wait_for_selector` with `selector` |

## FuelTrack Specific

```powershell
# Test login flow (admin@ft.com / Admin@123)
# 1. Navigate to http://localhost:5173/login
# 2. Fill email field
# 3. Fill password field
# 4. Click login button
# 5. Assert redirect to dashboard

# Test CRUD flow for Stations
# 1. Navigate to http://localhost:5173/stations
# 2. Click "Add Station" button
# 3. Fill form fields
# 4. Submit and verify success toast
```

## Configuration

| Variable | Required | Description |
|----------|----------|-------------|
| `PLAYWRIGHT_BROWSERS_PATH` | No | Custom browser install path |
| `PLAYWRIGHT_HEADLESS` | No | Run headless (`true`/`false`, default: `true`) |

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Browser not found | Run `npx playwright install` |
| Timeout on navigation | Check if frontend dev server is running on port 5173 |
| Element not visible | Use `wait_for_selector` before interacting |
| Screenshot blank | Ensure page is fully loaded before capture |
