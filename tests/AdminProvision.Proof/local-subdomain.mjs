const base = 'http://127.0.0.1:31371';
const jar = new Map();
async function request(path, form) {
  const headers = { cookie: [...jar].map(([k, v]) => `${k}=${v}`).join('; ') };
  const options = { headers, redirect: 'manual' };
  if (form) { options.method = 'POST'; headers['content-type'] = 'application/x-www-form-urlencoded'; options.body = new URLSearchParams(form); }
  const response = await fetch(base + path, options);
  for (const header of response.headers.getSetCookie()) { const pair = header.split(';', 1)[0], n = pair.indexOf('='); jar.set(pair.slice(0, n), pair.slice(n + 1)); }
  return { response, html: await response.text() };
}
const token = html => html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/)?.[1] ?? (() => { throw Error('Missing antiforgery token'); })();
const subdomain = `company-${Date.now()}`;
let page = await request('/login');
let result = await request('/login', { Username: 'proof-admin', Password: 'Proof-Password-123!', __RequestVerificationToken: token(page.html) });
if (result.response.status !== 302) throw Error('Admin login failed');
page = await request('/');
result = await request('/', { Name: 'Local company', Host: subdomain, __RequestVerificationToken: token(page.html) });
if (result.response.status !== 302) throw Error('Local tenant creation failed');
page = await request('/?q=' + subdomain);
const row = page.html.match(new RegExp(`<tr>[^]*?${subdomain}\\.localhost[^]*?<\\/tr>`, 'i'))?.[0];
const id = row?.match(/name="id" value="([0-9a-f-]{36})"/i)?.[1];
if (!id) throw Error('Subdomain was not generated');
const form = new FormData();
form.set('id', id);
form.set('displayName', 'Updated company');
form.set('__RequestVerificationToken', token(page.html));
form.set('logo', new Blob([Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/aXcAAAAASUVORK5CYII=', 'base64')], { type: 'image/png' }), 'logo.png');
const brandingResponse = await fetch(base + '/?handler=Branding', { method: 'POST', redirect: 'manual', headers: { cookie: [...jar].map(([k, v]) => `${k}=${v}`).join('; ') }, body: form });
if (brandingResponse.status !== 302) throw Error('Branding update failed');
page = await request('/');
if (!page.html.includes('Updated company') || !page.html.includes('Local company')) throw Error('Display name or independent tenant name missing');
const invalid = new FormData();
invalid.set('id', id);
invalid.set('displayName', 'Invalid replacement');
invalid.set('__RequestVerificationToken', token(page.html));
invalid.set('logo', new Blob(['not-an-image'], { type: 'image/png' }), 'bad.png');
const invalidResponse = await fetch(base + '/?handler=Branding', { method: 'POST', redirect: 'manual', headers: { cookie: [...jar].map(([k, v]) => `${k}=${v}`).join('; ') }, body: invalid });
if (invalidResponse.status !== 200) throw Error('Invalid logo was accepted');
page = await request('/');
if (!page.html.includes('Updated company') || page.html.includes('Invalid replacement')) throw Error('Invalid logo changed branding');
result = await request('/?handler=Provision', { id, adminEmail: 'admin@example.test', adminPassword: 'Tenant-Admin-Password-123!', __RequestVerificationToken: token(page.html) });
if (result.response.status !== 302) throw Error('Local tenant provisioning failed');
page = await request('/');
if (!page.html.includes('handler=ActivateDemo')) throw Error('Local activation form missing');
result = await request('/?handler=ActivateDemo', { id, __RequestVerificationToken: token(page.html) });
if (result.response.status !== 200 || !result.html.includes('Provisioning')) throw Error('Tenant activated without an active subscription');
console.log('PASS: admin creates local subdomain, provisions tenant and requires subscription');
