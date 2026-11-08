const base = process.env.PROOF_BASE_URL || 'http://admin:8080';
const jar = new Map();
const request = async (path, body) => {
  const options = { redirect: 'manual', headers: { cookie: [...jar].map(([k, v]) => `${k}=${v}`).join('; ') } };
  if (body) {
    options.method = 'POST';
    options.headers['content-type'] = 'application/x-www-form-urlencoded';
    options.body = new URLSearchParams(body);
  }
  const response = await fetch(base + path, options);
  for (const header of response.headers.getSetCookie()) {
    const pair = header.split(';', 1)[0];
    const separator = pair.indexOf('=');
    jar.set(pair.slice(0, separator), pair.slice(separator + 1));
  }
  return { response, html: await response.text() };
};
const token = html => {
  const match = html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
  if (!match) throw new Error('Antiforgery token not found');
  return match[1];
};
let page;
for (let attempt = 0; attempt < 60; attempt++) {
  try { page = await request('/login'); if (page.response.ok) break; } catch { /* still starting */ }
  await new Promise(resolve => setTimeout(resolve, 1000));
}
if (!page?.response.ok) throw new Error('Admin did not start');
let result = await request('/login', { Username: 'proof-admin', Password: 'Proof-Password-123!', __RequestVerificationToken: token(page.html) });
if (result.response.status !== 302) throw new Error('Login failed');
page = await request('/');
result = await request('/', { Name: 'Proof tenant', Host: 'proof.example.test', __RequestVerificationToken: token(page.html) });
if (result.response.status !== 302) throw new Error('Tenant registration failed');
page = await request('/');
const id = page.html.match(/name="id" value="([0-9a-f-]{36})"/i)?.[1];
if (!id) throw new Error('Provisioning button not rendered');
const noMatches = await request('/?q=does-not-exist');
if (noMatches.html.includes('name="id" value="' + id + '"')) throw new Error('Tenant search did not filter results');
result = await request('/?handler=Provision', { id, adminEmail: 'customer-admin@example.test', adminPassword: 'weak-password', __RequestVerificationToken: token(page.html) });
const decodedHtml = result.html.replace(/&#x([0-9a-f]+);/gi, (_, hex) => String.fromCodePoint(parseInt(hex, 16)));
if (result.response.status !== 200 || !decodedHtml.includes('يجب أن تتكون كلمة مرور المدير')) throw new Error('Password policy error was not shown');
page = result;
for (let attempt = 0; attempt < 2; attempt++) {
  const credentials = attempt === 0 ? { adminEmail: 'customer-admin@example.test', adminPassword: 'Tenant-Admin-Password-123!' } : {};
  result = await request('/?handler=Provision', { id, ...credentials, __RequestVerificationToken: token(page.html) });
  if (result.response.status !== 302) throw new Error('Provisioning or retry failed');
  page = await request('/');
}
if (!page.html.includes('Provisioning')) throw new Error('Tenant was activated prematurely');
if (page.html.includes('name="adminPassword"')) throw new Error('A provisioned tenant still requests a new administrator password');
result = await request('/?handler=Resume', { id, __RequestVerificationToken: token(page.html) });
if (result.response.status !== 200 || !result.html.includes('Provisioning')) throw new Error('Provisioning tenant could be resumed or readiness gate was bypassed');
result = await request('/?handler=Suspend', { id });
if (result.response.status !== 400) throw new Error('Tenant lifecycle action accepted a request without an antiforgery token');
console.log('PASS: admin login, registration, provisioning, retry and inactive status');
