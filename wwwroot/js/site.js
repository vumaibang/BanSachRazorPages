document.querySelector('[data-store-menu]')?.addEventListener('click', function () {
    const menu = document.getElementById('store-menu');
    const expanded = menu.classList.toggle('open');
    this.setAttribute('aria-expanded', String(expanded));
});
document.querySelectorAll('form[data-submit-once]').forEach(form => form.addEventListener('submit', function () {
    if (!this.checkValidity() || (window.jQuery?.fn?.valid && !window.jQuery(this).valid())) return;
    this.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = true; button.textContent = 'Đang xử lý…'; });
}));
