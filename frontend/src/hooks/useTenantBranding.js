import { useEffect, useState } from 'react';

export default function useTenantBranding() {
  const [branding, setBranding] = useState({ name: 'Bitbus', logoUrl: null });
  useEffect(() => {
    const controller = new AbortController();
    fetch('/api/tenant-branding', { credentials: 'same-origin', cache: 'no-store', signal: controller.signal })
      .then(response => response.ok ? response.json() : null)
      .then(data => { if (data?.name && !controller.signal.aborted) setBranding({ name: data.name, logoUrl: data.logoUrl || null }); })
      .catch(() => {});
    return () => controller.abort();
  }, []);
  return branding;
}
