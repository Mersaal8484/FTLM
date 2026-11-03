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
