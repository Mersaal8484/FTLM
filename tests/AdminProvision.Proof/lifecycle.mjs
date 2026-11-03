const base = process.env.PROOF_BASE_URL || 'http://127.0.0.1:31371';
const jar = new Map();
async function request(path, form) {
  const headers = { cookie: [...jar].map(([key, value]) => `${key}=${value}`).join('; ') };
  const options = { headers, redirect: 'manual' };
  if (form) { options.method = 'POST'; headers['content-type'] = 'application/x-www-form-urlencoded'; options.body = new URLSearchParams(form); }
  const response = await fetch(base + path, options);
  for (const header of response.headers.getSetCookie()) {
    const pair = header.split(';', 1)[0], split = pair.indexOf('=');
    jar.set(pair.slice(0, split), pair.slice(split + 1));
  }
  return { response, html: await response.text() };
}
const token = html => html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/)?.[1] ?? (() => { throw Error('Missing antiforgery token'); })();
let page = await request('/login');
let result = await request('/login', { Username: 'proof-admin', Password: 'Proof-Password-123!', __RequestVerificationToken: token(page.html) });
if (result.response.status !== 302) throw Error('Login failed');
page = await request('/');
const id = page.html.match(/name="id" value="([0-9a-f-]{36})"/i)?.[1];
if (!id || !page.html.includes('handler=Suspend')) throw Error('Suspend form missing');
result = await request('/?handler=Suspend', { id, __RequestVerificationToken: token(page.html) });
if (result.response.status !== 302) throw Error('Suspend failed');
page = await request('/');
if (!page.html.includes('Suspended') || !page.html.includes('handler=Resume')) throw Error('Suspended state or resume form missing');
result = await request('/?handler=Resume', { id, __RequestVerificationToken: token(page.html) });
if (result.response.status !== 200 || !result.html.includes('Suspended')) throw Error('Readiness gate was bypassed');
console.log('PASS: admin suspension form and readiness-gated resume');
