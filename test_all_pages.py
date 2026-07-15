"""
FuelTrack Full Page-by-Page Test
Tests every page in the application for loading errors, console errors, and visual issues.
"""
import os
import json
import time
import sys
import io

# Fix Windows console encoding
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')
from datetime import datetime
from playwright.sync_api import sync_playwright, TimeoutError as PwTimeout

RESULTS = []
CONSOLE_ERRORS = []
SCREENSHOT_DIR = r"C:\Users\User\Desktop\FuelTrack-Rebuild\test_screenshots"

os.makedirs(SCREENSHOT_DIR, exist_ok=True)

ADMIN_EMAIL = "admin@ft.com"
ADMIN_PASS = "Admin@123"
BASE_URL = "http://localhost:3000"

# Pages to test: (path, name, requires_params)
PAGES = [
    ("/login", "Login", False),
    ("/", "Dashboard", True),
    ("/notifications", "Notifications", True),
    ("/locations", "Locations", True),
    ("/meters", "Meters", True),
    ("/generators", "Generators", True),
    ("/setup-oils", "SetupOils", True),
    ("/tanks", "Tanks", True),
    ("/oils", "OilInventory", True),
    ("/users", "Users", True),
    ("/roles", "Roles", True),
    ("/controllers", "Controllers", True),
    ("/audits", "AuditLogs", True),
    ("/rollback", "Rollback", True),
    ("/fuel-movements", "FuelMovements", True),
    ("/inventory", "PeriodicInventory", True),
    ("/review-changes", "ReviewChanges", True),
    ("/reports/station-readings", "Report-StationReadings", True),
    ("/reports/fuel-transactions", "Report-FuelTransactions", True),
    ("/reports/oil-transactions", "Report-OilTransactions", True),
    ("/reports/generators", "Report-Generators", True),
    ("/reports/efficiency", "Report-Efficiency", True),
    ("/reports/periodic-inventory", "Report-PeriodicInventory", True),
    ("/my-generators", "MyGenerators", True),
]


def login(page):
    """Login and get to dashboard."""
    print("[LOGIN] Navigating to login page...")
    page.goto(f"{BASE_URL}/login", wait_until="networkidle", timeout=30000)
    page.wait_for_timeout(2000)

    # Screenshot login page
    page.screenshot(path=os.path.join(SCREENSHOT_DIR, "00_login_page.png"), full_page=True)

    # Fill email
    email_input = page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i], input[placeholder*="بريد" i]').first
    email_input.wait_for(state="visible", timeout=10000)
    email_input.fill(ADMIN_EMAIL)

    # Fill password
    pass_input = page.locator('input[type="password"]').first
    pass_input.fill(ADMIN_PASS)

    # Click login button
    login_btn = page.locator('button[type="submit"], button:has-text("Login"), button:has-text("تسجيل")').first
    login_btn.click()

    # Wait for navigation to dashboard
    try:
        page.wait_for_url("**/", timeout=15000)
        page.wait_for_load_state("networkidle", timeout=15000)
        page.wait_for_timeout(3000)
        print("[LOGIN] SUCCESS - Logged in and on dashboard")
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "01_dashboard.png"), full_page=True)
        return True
    except PwTimeout:
        print("[LOGIN] WARNING - Timeout waiting for dashboard redirect, checking current state...")
        page.screenshot(path=os.path.join(SCREENSHOT_DIR, "01_login_result.png"), full_page=True)
        return "login" not in page.url.lower()


def test_page(page, path, name, requires_auth):
    """Test a single page and return result dict."""
    result = {
        "name": name,
        "path": path,
        "status": "UNKNOWN",
        "load_time_ms": 0,
        "console_errors": [],
        "http_errors": [],
        "screenshot": "",
        "details": ""
    }

    url = f"{BASE_URL}{path}"
    console_errors = []

    # Capture console errors
    def on_console(msg):
        if msg.type in ("error",):
            console_errors.append(f"[{msg.type}] {msg.text}")

    page.on("console", on_console)

    # Capture HTTP errors
    http_errors = []
    def on_response(response):
        if response.status >= 400:
            http_errors.append(f"{response.status} {response.url}")

    page.on("response", on_response)

    try:
        # Delay between pages to avoid 429 rate limiting
        page.wait_for_timeout(2000)
        start = time.time()
        page.goto(url, wait_until="networkidle", timeout=30000)
        page.wait_for_timeout(3000)
        elapsed = int((time.time() - start) * 1000)

        # Take screenshot
        ss_path = os.path.join(SCREENSHOT_DIR, f"{name}.png")
        page.screenshot(path=ss_path, full_page=True)

        # Check for visible error indicators
        visible_errors = []
        try:
            for sel in ['.error', '.alert-danger', '[class*="error"]']:
                for el in page.locator(sel).all():
                    try:
                        if el.is_visible():
                            visible_errors.append(el.text_content()[:100])
                    except:
                        pass
            for text_match in ['Error', '404', 'Not Found']:
                for el in page.get_by_text(text_match, exact=False).all():
                    try:
                        if el.is_visible():
                            t = el.text_content()[:100]
                            if t not in visible_errors:
                                visible_errors.append(t)
                    except:
                        pass
        except:
            pass

        result["load_time_ms"] = elapsed
        result["console_errors"] = console_errors
        result["http_errors"] = http_errors
        result["screenshot"] = ss_path

        if visible_errors:
            result["status"] = "WARN"
            result["details"] = f"Visible errors: {'; '.join(visible_errors[:3])}"
        elif http_errors:
            result["status"] = "FAIL"
            result["details"] = f"HTTP errors: {'; '.join(http_errors[:3])}"
        elif console_errors:
            result["status"] = "WARN"
            result["details"] = f"Console errors: {'; '.join([e[:80] for e in console_errors[:3]])}"
        else:
            result["status"] = "PASS"
            result["details"] = f"Loaded in {elapsed}ms"

    except PwTimeout:
        result["status"] = "TIMEOUT"
        result["details"] = f"Page did not load within 30s"
        try:
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, f"{name}_timeout.png"), full_page=True)
        except:
            pass
    except Exception as e:
        result["status"] = "ERROR"
        result["details"] = str(e)[:200]
        try:
            page.screenshot(path=os.path.join(SCREENSHOT_DIR, f"{name}_error.png"), full_page=True)
        except:
            pass
    finally:
        page.remove_listener("console", on_console)
        page.remove_listener("response", on_response)

    return result


def run_tests():
    print(f"\n{'='*70}")
    print(f"  FuelTrack Page-by-Page Test")
    print(f"  Started: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"  Target: {BASE_URL}")
    print(f"{'='*70}\n")

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(
            viewport={"width": 1920, "height": 1080},
            locale="en-US"
        )
        page = context.new_page()

        # Step 1: Login
        print("="*50)
        print("PHASE 1: Authentication")
        print("="*50)

        login_ok = login(page)
        if not login_ok:
            print("[FATAL] Could not login. Aborting tests.")
            browser.close()
            return

        # Step 2: Test each page
        print("\n" + "="*50)
        print("PHASE 2: Page-by-Page Testing")
        print("="*50)

        pass_count = 0
        warn_count = 0
        fail_count = 0
        total_count = 0

        for path, name, requires_auth in PAGES:
            if path == "/login":
                continue  # Already tested

            total_count += 1
            print(f"\n[{total_count}/{len(PAGES)-1}] Testing: {name} ({path})")

            result = test_page(page, path, name, requires_auth)
            RESULTS.append(result)

            status = result["status"]
            if status == "PASS":
                pass_count += 1
                icon = "[OK]"
            elif status == "WARN":
                warn_count += 1
                icon = "[!!]"
            else:
                fail_count += 1
                icon = "[XX]"

            print(f"  {icon} {status}: {result['details']}")
            if result["http_errors"]:
                print(f"    HTTP: {result['http_errors'][:3]}")
            if result["console_errors"]:
                print(f"    Console: {[e[:60] for e in result['console_errors'][:3]]}")

        browser.close()

    # Step 3: Summary
    print("\n" + "="*70)
    print("  TEST RESULTS SUMMARY")
    print("="*70)
    print(f"  Total Pages Tested: {total_count}")
    print(f"  [OK] PASS: {pass_count}")
    print(f"  [!!] WARN: {warn_count}")
    print(f"  [XX] FAIL: {fail_count + (1 if not login_ok else 0)}")
    print(f"  Screenshots: {SCREENSHOT_DIR}")
    print("="*70)

    # Detailed table
    print(f"\n{'Page':<30} {'Status':<8} {'Time':<10} {'Details'}")
    print("-"*90)
    for r in RESULTS:
        print(f"{r['name']:<30} {r['status']:<8} {r['load_time_ms']:<10} {r['details'][:50]}")

    # Save JSON report
    report = {
        "timestamp": datetime.now().isoformat(),
        "summary": {
            "total": total_count,
            "pass": pass_count,
            "warn": warn_count,
            "fail": fail_count
        },
        "results": RESULTS
    }
    report_path = os.path.join(SCREENSHOT_DIR, "test_report.json")
    with open(report_path, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    print(f"\nFull report saved to: {report_path}")


if __name__ == "__main__":
    run_tests()
