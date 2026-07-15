import DOMPurify from 'dompurify';

export const sanitize = (dirty) => DOMPurify.sanitize(dirty);
export const sanitizeHtml = (html) => DOMPurify.sanitize(html, { USE_PROFILES: { html: true } });
