"""Debug: Check backend API health"""
from playwright.sync_api import sync_playwright
import urllib.request
import json

# Test backend directly
print("[1] Testing backend API directly...")

# Test /api/auth/me
try:
    req = urllib.request.Request("http://localhost:5000/api/auth/me")
    resp = urllib.request.urlopen(req, timeout=5)
    print(f"  /api/auth/me: {resp.status}")
except urllib.error.HTTPError as e:
    body = e.read().decode("utf-8", errors="replace")[:500]
    print(f"  /api/auth/me: {e.code} - {body}")
except Exception as e:
    print(f"  /api/auth/me: ERROR - {e}")

# Test /api/auth/login
try:
    data = json.dumps({"email": "admin@ft.com", "password": "Admin@123"}).encode("utf-8")
    req = urllib.request.Request("http://localhost:5000/api/auth/login", data=data, headers={"Content-Type": "application/json"})
    resp = urllib.request.urlopen(req, timeout=10)
    print(f"  /api/auth/login: {resp.status}")
    body = resp.read().decode("utf-8")[:500]
    print(f"  Response: {body}")
except urllib.error.HTTPError as e:
    body = e.read().decode("utf-8", errors="replace")[:1000]
    print(f"  /api/auth/login: {e.code} - {body}")
except Exception as e:
    print(f"  /api/auth/login: ERROR - {e}")
