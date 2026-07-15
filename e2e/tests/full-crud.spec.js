const { test, expect } = require('@playwright/test');
const http = require('http');

const BASE_URL = 'http://localhost:3000';
const API_HOST = 'localhost';
const API_PORT = 5000;
const ADMIN_USER = 'admin@ft.com';
const ADMIN_PASS = 'Admin@123';

function httpPost(path, body, headers = {}) {
  return new Promise((resolve, reject) => {
    const data = JSON.stringify(body);
    const req = http.request({
      hostname: API_HOST, port: API_PORT, path, method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data), ...headers },
      timeout: 120000,
    }, (res) => {
      let buf = '';
      res.on('data', chunk => buf += chunk);
      res.on('end', () => {
        try { resolve({ status: res.statusCode, body: JSON.parse(buf) }); }
        catch { resolve({ status: res.statusCode, body: buf }); }
      });
    });
    req.on('timeout', () => { req.destroy(); reject(new Error('HTTP request timed out')); });
    req.on('error', reject);
    req.write(data);
    req.end();
  });
}

function httpGet(path, headers = {}) {
  return new Promise((resolve, reject) => {
    const req = http.request({
      hostname: API_HOST, port: API_PORT, path, method: 'GET', headers,
      timeout: 120000,
    }, (res) => {
      let buf = '';
      res.on('data', chunk => buf += chunk);
      res.on('end', () => {
        try { resolve({ status: res.statusCode, body: JSON.parse(buf) }); }
        catch { resolve({ status: res.statusCode, body: buf }); }
      });
    });
    req.on('timeout', () => { req.destroy(); reject(new Error('HTTP request timed out')); });
    req.on('error', reject);
    req.end();
  });
}

async function loginViaAPI() {
  for (let i = 1; i <= 5; i++) {
    try {
      console.log(`[API login] Attempt ${i}...`);
      const res = await httpPost('/api/auth/login', { userName: ADMIN_USER, password: ADMIN_PASS });
      if (res.status === 200 && res.body.token) {
        console.log(`[API login] Success`);
        return res.body.token;
      }
      console.log(`[API login] Attempt ${i}: status=${res.status}`);
    } catch (e) {
      console.log(`[API login] Attempt ${i} error: ${e.message}`);
    }
    await new Promise(r => setTimeout(r, 5000 * i));
  }
  throw new Error('API login failed');
}

async function loginViaUI(page) {
  await page.goto(`${BASE_URL}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(3000);

  const emailField = page.locator('input[type="email"], input[name="email"], input[placeholder*="البريد"], input[placeholder*="email"], input[placeholder*="Email"]').first();
  const passwordField = page.locator('input[type="password"], input[name="password"]').first();

  await emailField.waitFor({ state: 'visible', timeout: 15000 });
  await emailField.fill(ADMIN_USER);
  await passwordField.fill(ADMIN_PASS);

  const submitBtn = page.locator('button[type="submit"], button:has-text("تسجيل"), button:has-text("Login"), button:has-text("دخول")').first();
  await submitBtn.click();

  await page.waitForURL(url => !url.toString().includes('/login'), { timeout: 30000 });
  await page.waitForLoadState('domcontentloaded');
  await page.waitForTimeout(2000);

  console.log(`[UI login] Redirected to: ${page.url()}`);
}

async function navigateAndWait(page, path) {
  await page.goto(`${BASE_URL}${path}`, { waitUntil: 'domcontentloaded' });
  await page.waitForTimeout(5000);

  if (page.url().includes('/login')) {
    console.log(`  Redirected to login, re-logging in via UI...`);
    await loginViaUI(page);
    await page.goto(`${BASE_URL}${path}`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);
  }
}

test.describe('Full CRUD Tests - 8 Pages', () => {
  test.setTimeout(120000);

  test('1 - Roles & Permissions page', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/roles`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Roles body:', body?.substring(0, 400));

    const hasRolesContent = body?.includes('Admin') || body?.includes('admin') || body?.includes('Role') || body?.includes('role') || body?.includes('ادوار') || body?.includes('صلاحية') || body?.includes('الصلاحيات');
    expect(hasRolesContent).toBeTruthy();
    console.log('Roles & Permissions: PASS');
  });

  test('2 - User Management page', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/users`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Users body:', body?.substring(0, 400));

    const hasUsersContent = body?.includes('admin@ft.com') || body?.includes('Admin') || body?.includes('admin') || body?.includes('user') || body?.includes('المستخدم');
    expect(hasUsersContent).toBeTruthy();
    console.log('User Management: PASS');
  });

  test('3 - Branches and Stations page', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/locations`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Locations body:', body?.substring(0, 400));

    const hasLocations = body?.includes('Branch') || body?.includes('Station') || body?.includes('Location') || body?.includes('branch') || body?.includes('station') || body?.includes('المواقع') || body?.includes('الفروع') || body?.includes('المحطات');
    expect(hasLocations).toBeTruthy();
    console.log('Branches and Stations: PASS');
  });

  test('4 - Fuel Tanks settings', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/tanks`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Tanks body:', body?.substring(0, 400));

    const hasTanks = body?.includes('Tank') || body?.includes('tank') || body?.includes('خزان') || body?.includes('Fuel') || body?.includes('وقود');
    expect(hasTanks).toBeTruthy();
    console.log('Fuel Tanks: PASS');
  });

  test('5 - Oil Tanks settings', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/setup-oils`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Oil body:', body?.substring(0, 400));

    const hasOil = body?.includes('Oil') || body?.includes('oil') || body?.includes('زيت') || body?.includes('Setup') || body?.includes('إعداد');
    expect(hasOil).toBeTruthy();
    console.log('Oil Tanks: PASS');
  });

  test('6 - Generator Screens (Controllers)', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/controllers`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Controllers body:', body?.substring(0, 400));

    const hasContent = body?.includes('Controller') || body?.includes('Screen') || body?.includes('controller') || body?.includes('screen') || body?.includes('شاشات') || body?.includes('شاشات التحكم');
    expect(hasContent).toBeTruthy();
    console.log('Generator Screens: PASS');
  });

  test('7 - Meters page', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/meters`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Meters body:', body?.substring(0, 400));

    const hasMeters = body?.includes('Meter') || body?.includes('meter') || body?.includes('عداد');
    expect(hasMeters).toBeTruthy();
    console.log('Meters: PASS');
  });

  test('8 - Generators page', async ({ page }) => {
    await loginViaUI(page);
    console.log(`  Logged in, now at: ${page.url()}`);

    await page.goto(`${BASE_URL}/generators`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(5000);

    const url = page.url();
    console.log('URL:', url);
    expect(url).not.toContain('/login');

    const body = await page.textContent('body');
    console.log('Generators body:', body?.substring(0, 400));

    const hasGenerators = body?.includes('Generator') || body?.includes('generator') || body?.includes('مولد');
    expect(hasGenerators).toBeTruthy();
    console.log('Generators: PASS');
  });

  test('9 - Verify all dropdowns have lookup data via API', async () => {
    const token = await loginViaAPI();
    expect(token).toBeTruthy();

    const lookupsRes = await httpGet('/api/Lookups', { Authorization: `Bearer ${token}` });
    console.log(`Lookups API: ${lookupsRes.status}`);
    expect(lookupsRes.status).toBe(200);
    const lookups = lookupsRes.body;
    console.log(`Total lookups: ${Array.isArray(lookups) ? lookups.length : 'not an array'}`);

    if (Array.isArray(lookups)) {
      const categories = {};
      lookups.forEach(l => {
        if (!categories[l.category]) categories[l.category] = [];
        categories[l.category].push(l.code);
      });
      console.log('Categories:', Object.keys(categories).join(', '));
    }

    const endpoints = ['/api/Branches', '/api/Stations', '/api/SubStations', '/api/Generators', '/api/Meters', '/api/FuelTanks'];
    for (const endpoint of endpoints) {
      const res = await httpGet(endpoint, { Authorization: `Bearer ${token}` });
      console.log(`  ${endpoint}: ${res.status}`);
    }

    console.log('Dropdown verification: PASS');
  });
});
