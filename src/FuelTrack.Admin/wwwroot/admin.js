document.querySelectorAll('form[method="post"]').forEach(form => {
  form.addEventListener('submit', event => {
    if (form.dataset.submitting === 'true') {
      event.preventDefault();
      return;
    }
    form.dataset.submitting = 'true';
    const button = event.submitter;
    if (button) {
      button.dataset.originalText = button.textContent;
      button.disabled = true;
      button.textContent = 'جارٍ التنفيذ...';
    }
  });
});

window.addEventListener('pageshow', () => {
  document.querySelectorAll('form[data-submitting="true"]').forEach(form => {
    form.dataset.submitting = 'false';
    form.querySelectorAll('button[data-original-text]').forEach(button => {
      button.disabled = false;
      button.textContent = button.dataset.originalText;
      delete button.dataset.originalText;
    });
  });
});

document.querySelectorAll('[data-branding-form]').forEach(form => {
  const name = form.querySelector('[data-branding-input]');
  const output = form.querySelector('[data-branding-name]');
  const file = form.querySelector('[data-branding-file]');
  const image = form.querySelector('[data-branding-image]');
  const initial = form.querySelector('[data-branding-initial]');
  const remove = form.querySelector('[data-branding-remove]');
  const originalSrc = image.getAttribute('src');
  let previewUrl = null;
  image.addEventListener('error', () => { image.hidden = true; initial.hidden = false; });
  const updateName = () => {
    const value = name.value.trim();
    output.textContent = value || 'اسم المستأجر';
    initial.textContent = value ? Array.from(value)[0].toUpperCase() : '•';
    image.alt = value ? `شعار ${value}` : 'شعار المستأجر';
  };
  const updateImage = () => {
    if (previewUrl) { URL.revokeObjectURL(previewUrl); previewUrl = null; }
    if (file.files?.[0]) previewUrl = URL.createObjectURL(file.files[0]);
    const src = previewUrl || (!remove?.checked ? originalSrc : '');
    if (src) image.src = src;
    else image.removeAttribute('src');
    image.hidden = !src;
    initial.hidden = Boolean(src);
  };
  name.addEventListener('input', updateName);
  file.addEventListener('change', () => { if (file.files?.length && remove) remove.checked = false; updateImage(); });
  remove?.addEventListener('change', () => { if (remove.checked) file.value = ''; updateImage(); });
  updateName();
});

document.querySelectorAll('[data-subscription-form]').forEach(form => {
  const tenant = form.querySelector('[data-subscription-tenant]');
  const plan = form.querySelector('[data-subscription-plan]');
  const start = form.querySelector('[data-subscription-start]');
  const end = form.querySelector('[data-subscription-end]');
  const update = () => {
    form.querySelector('[data-summary-tenant]').textContent = tenant.value ? tenant.selectedOptions[0].textContent : 'لم يُحدد';
    form.querySelector('[data-summary-plan]').textContent = plan.value ? plan.selectedOptions[0].textContent : 'لم تُحدد';
    form.querySelector('[data-summary-period]').textContent = start.value && end.value ? `${start.value} → ${end.value}` : '—';
    end.setCustomValidity(start.value && end.value && end.value <= start.value ? 'يجب أن يكون تاريخ النهاية بعد البداية.' : '');
  };
  [tenant, plan, start, end].forEach(field => field.addEventListener('change', update));
  update();
});
