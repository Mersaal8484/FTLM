import asyncio
import sys
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
from playwright.async_api import async_playwright

async def main():
    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=False)
        context = await browser.new_context(viewport={"width": 1280, "height": 800})
        page = await context.new_page()

        api_calls = []
        def on_response(response):
            if "/api/" in response.url:
                api_calls.append({"url": response.url, "status": response.status})
        page.on("response", on_response)

        # Login
        await page.goto("http://localhost:3000/login", wait_until="networkidle", timeout=15000)
        await page.fill('input[type="email"]', "admin@ft.com")
        await page.fill('input[type="password"]', "Admin@123")
        await page.click('button[type="submit"]')
        await page.wait_for_load_state("networkidle", timeout=10000)
        await asyncio.sleep(2)
        print(f"[1] Login OK, URL: {page.url}")

        # Navigate to users
        api_calls.clear()
        await page.goto("http://localhost:3000/users", wait_until="networkidle", timeout=15000)
        await asyncio.sleep(3)

        print(f"[2] URL: {page.url}")
        print(f"[3] API calls during /users:")
        for c in api_calls:
            print(f"    {c['status']} {c['url']}")

        await page.screenshot(path="screenshots/07_users_debug.png")
        await browser.close()

asyncio.run(main())
