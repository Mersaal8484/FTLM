// Kept independent from DOM/image APIs so the operator's photo stamp is testable.
export function formatPhotoStamp(capturedAt, subjectCode, locale = 'en-GB') {
  const stampTime = new Intl.DateTimeFormat(locale, {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
  }).format(new Date(capturedAt));
  return `${stampTime} · ${String(subjectCode || '').trim()}`.trim();
}

export function getPhotoStampLayout(width, height, text) {
  const stripHeight = Math.max(42, Math.round(height * 0.09));
  return { x: 0, y: Math.max(0, height - stripHeight), width, height: stripHeight, text, fontSize: Math.max(14, Math.round(stripHeight * 0.42)) };
}

export function drawPhotoStamp(context, width, height, capturedAt, subjectCode, locale) {
  const text = formatPhotoStamp(capturedAt, subjectCode, locale);
  const layout = getPhotoStampLayout(width, height, text);
  context.fillStyle = 'rgba(0, 0, 0, 0.78)';
  context.fillRect(layout.x, layout.y, layout.width, layout.height);
  context.fillStyle = '#ffffff';
  context.font = `600 ${layout.fontSize}px sans-serif`;
  context.textBaseline = 'middle';
  context.direction = 'ltr';
  context.fillText(text, 14, layout.y + layout.height / 2);
  return layout;
}
