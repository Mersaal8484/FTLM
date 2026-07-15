"""Debug: Check login page structure"""
from playwright.sync_api import sync_playwright
import os

SCREENSHOT_DIR = r"C:\Users\User\Desktop\FuelTrack-Rebuild\test_screenshots"
os.makedirs(SCREENSHOT_DIR, exist_ok=True)

with sync_playwright() as p:
    browser = p.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 1920, "height": 1080})

    # Capture console
    page.on("console", lambda msg: print(f"  [CONSOLE {msg.type}] {msg.text[:120]}"))
    page.on("response", lambda r: print(f"  [HTTP {r.status}] {r.url[:120]}") if r.status >= 400 else None)

    print("[1] Going to login page...")
    page.goto("http://localhost:3000/login", wait_until="networkidle", timeout=30000)
    page.wait_for_timeout(3000)

    print(f"[2] Current URL: {page.url}")
    page.screenshot(path=os.path.join(SCREENSHOT_DIR, "debug_login.png"), full_page=True)

    # Find all inputs
    print("[3] All inputs on page:")
    inputs = page.locator("input").all()
    for i, inp in enumerate(inputs):
        try:
            inp_type = inp.get_attribute("type") or "text"
            inp_name = inp.get_attribute("name") or ""
            inp_placeholder = inp.get_attribute("placeholder") or ""
            print(f"    Input {i}: type={inp_type}, name={inp_name}, placeholder={inp_placeholder}")
        except:
            pass

    # Find all buttons
    print("[4] All buttons on page:")
    buttons = page.locator("button").all()
    for i, btn in enumerate(buttons):
        try:
            btn_text = btn.text_content()[:50]
            btn_type = btn.get_attribute("type") or ""
            print(f"    Button {i}: type={btn_type}, text='{btn_text}'")
        except:
            pass

    # Try filling and submitting
    print("[5] Attempting login...")
    email_inputs = page.locator('input[type="email"]').all()
    text_inputs = page.locator('input[type="text"]').all()
    password_inputs = page.locator('input[type="password"]').all()

    print(f"    Email inputs: {len(email_inputs)}, Text inputs: {len(text_inputs)}, Password inputs: {len(password_inputs)}")

    # Try to fill whatever we find
    if email_inputs:
        email_inputs[0].fill("admin@ft.com")
        print("    Filled email (email input)")
    elif text_inputs:
        text_inputs[0].fill("admin@ft.com")
        print("    Filled email (text input)")

    if password_inputs:
        password_inputs[0].fill("Admin@123")
        print("    Filled password")

    page.screenshot(path=os.path.join(SCREENSHOT_DIR, "debug_login_filled.png"), full_page=True)

    # Click submit
    submit_btns = page.locator('button[type="submit"]').all()
    if submit_btns:
        submit_btns[0].click()
        print("    Clicked submit button")
    else:
        # Try any button with login text
        login_btns = page.locator('button:has-text("Login"), button:has-text("login"), button:has-text("دخول"), button:has-text("تسجيل")').all()
        if login_btns:
            login_btns[0].click()
            print("    Clicked login button by text")
        else:
            # Just press Enter
            page.keyboard.press("Enter")
            print("    Pressed Enter")

    page.wait_for_timeout(5000)
    print(f"[6] After login URL: {page.url}")
    page.screenshot(path=os.path.join(SCREENSHOT_DIR, "debug_after_login.png"), full_page=True)

    # Check page content
    body_text = page.locator("body").text_content()[:500]
    print(f"[7] Page text (first 500 chars): {body_text[:300]}")

    browser.close()
