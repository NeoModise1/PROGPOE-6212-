// Confirm destructive actions: <form data-confirm="Are you sure?">
document.addEventListener('submit', function (e) {
    var message = e.target && e.target.getAttribute && e.target.getAttribute('data-confirm');
    if (message && !window.confirm(message)) e.preventDefault();
});

// Live preview for image pickers: <input type="file" data-preview="#imgId">
document.addEventListener('change', function (e) {
    var input = e.target;
    var selector = input && input.getAttribute && input.getAttribute('data-preview');
    if (!selector || !input.files || !input.files[0]) return;
    var img = document.querySelector(selector);
    if (!img) return;
    img.src = URL.createObjectURL(input.files[0]);
    img.classList.remove('d-none');
});

// Stop double submits.
document.addEventListener('submit', function (e) {
    var form = e.target;
    if (e.defaultPrevented || !form.querySelector) return;
    var btn = form.querySelector('button[type=submit][data-busy]');
    if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> ' + btn.getAttribute('data-busy'); }
});
