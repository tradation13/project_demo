// Shared confirmation dialog helper.
// Add data-confirm on forms that need Yes/No before submit (delete / edit).
// Create forms should NOT include data-confirm.

window.appConfirm = function () {
    return new Promise(function (resolve) {
        window.dispatchEvent(new CustomEvent('app-confirm', {
            detail: { resolve: resolve }
        }));
    });
};

document.addEventListener('submit', function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement) || !form.hasAttribute('data-confirm')) {
        return;
    }

    if (form.dataset.confirmed === 'true') {
        delete form.dataset.confirmed;
        return;
    }

    e.preventDefault();
    e.stopPropagation();

    window.appConfirm().then(function (ok) {
        if (!ok) {
            return;
        }

        form.dataset.confirmed = 'true';
        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
        } else {
            form.submit();
        }
    });
}, true);
