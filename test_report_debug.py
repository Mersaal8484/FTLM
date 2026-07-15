"""Debug report page loading - capture all network activity"""
import sys, io, os, time
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')
from playwright.sync_api import sync_playwright

SCREENSHOT_DIR = r"C:\Users\User\Desktop\FuelTrack-Rebuild\test_screenshots"
os.makedirs(SCREENSHOT_DIR, exist_ok=True)

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    context = browser.new_context(viewport={"width": 1920, "height": 1080})
    page = context.new_page()

    # Capture ALL network requests and responses
    requests_log = []
    responses_log = []

    def on_request(req):
        requests_log.append(f"[REQ] {req.method} {req.url}")

    def on_response(resp):
        status = resp.status
        url = resp.url
        responses_log.append(f"[RES] {status} {url}")
        if status >= 400:
            print(f"  HTTP ERROR: {status} {url}")

    def on_requestfailed(req):
        print(f"  REQ FAILED: {req.method} {req.url} - {req.failure}")

    page.on("request", on_request)
    page.on("response", on_response)
    page.on("requestfailed", on_requestfailed)

    # Login
    print("=== LOGIN ===")
    page.goto("http://localhost:3000/login", wait_until="networkidle", timeout=30000)
    page.wait_for_timeout(2000)
    page.locator('input[type="email"]').first.fill("admin@ft.com")
    page.locator('input[type="password"]').first.fill("Admin@123")
    page.locator('button[type="submit"]').first.click()
    page.wait_for_url("**/", timeout=15000)
    page.wait_for_load_state("networkidle", timeout=15000)
    page.wait_for_timeout(3000)
    print(f"  Logged in. Requests so far: {len(requests_log)}")

    # Clear logs before navigating to report
    requests_log.clear()
    responses_log.clear()

    # Navigate to a report page
    print("\n=== NAVIGATE TO Report-StationReadings ===")
    print("  (should be first page after dashboard)")

    # Try going to a report directly from dashboard
    page.wait_for_timeout(2000)

    # Capture console output
    console_msgs = []
    def on_console(msg):
        console_msgs.append(f"[{msg.type}] {msg.text}")
    page.on("console", on_console)

    start = time.time()
    try:
        page.goto("http://localhost:3000/reports/station-readings",
                   wait_until="domcontentloaded", timeout=15000)
        print(f"  DOM loaded in {time.time()-start:.1f}s")

        # Wait up to 20s for networkidle
        try:
            page.wait_for_load_state("networkidle", timeout=20000)
            print(f"  networkidle reached in {time.time()-start:.1f}s")
        except:
            print(f"  TIMEOUT waiting for networkidle after {time.time()-start:.1f}s")

    except Exception as e:
        print(f"  NAVIGATION ERROR: {e}")

    page.wait_for_timeout(5000)

    # Print all captured network activity
    print(f"\n=== NETWORK ACTIVITY ({len(requests_log)} requests, {len(responses_log)} responses) ===")
    for r in requests_log:
        print(f"  {r}")
    print("  --- responses ---")
    for r in responses_log:
        print(f"  {r}")

    # Print console messages
    print(f"\n=== CONSOLE ({len(console_msgs)} messages) ===")
    for m in console_msgs[:20]:
        print(f"  {m}")

    page.screenshot(path=os.path.join(SCREENSHOT_DIR, "debug_report_direct.png"), full_page=True)
    print(f"\nScreenshot saved to {SCREENSHOT_DIR}")

    # Now try a NON-report page for comparison
    requests_log.clear()
    responses_log.clear()
    console_msgs.clear()

    print("\n=== NAVIGATE TO Locations (non-report) ===")
    page.wait_for_timeout(2000)
    try:
        page.goto("http://localhost:3000/locations",
                   wait_until="domcontentloaded", timeout=15000)
        print(f"  DOM loaded in {time.time()-start:.1f}s")
        page.wait_for_load_state("networkidle", timeout=15000)
        print(f"  networkidle reached in {time.time()-start:.1f}s")
    except Exception as e:
        print(f"  ERROR: {e}")

    page.wait_for_timeout(3000)
    print(f"\n=== NETWORK ACTIVITY ({len(requests_log)} requests, {len(responses_log)} responses) ===")
    for r in requests_log:
        print(f"  {r}")
    print("  --- responses ---")
    for r in responses_log:
        print(f"  {r}")
    print(f"\n=== CONSOLE ({len(console_msgs)} messages) ===")
    for m in console_msgs[:20]:
        print(f"  {m}")

    browser.close()
