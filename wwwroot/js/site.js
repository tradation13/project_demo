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

// Chat session isolation: clear SessionId before logout so the next identity gets a new UUID.
(function () {
    var CHAT_SESSION_KEY = "physiotech_n8n_chat_session";

    function clearChatSessionId() {
        try {
            sessionStorage.removeItem(CHAT_SESSION_KEY);
        } catch (e) { /* ignore */ }
    }

    window.PhysioTechClearChatSession = clearChatSessionId;

    document.addEventListener(
        "submit",
        function (e) {
            var form = e.target;
            if (!(form instanceof HTMLFormElement)) return;
            var action = (form.getAttribute("action") || "").toString();
            if (/\/Auth\/Logout/i.test(action) || /action=Logout/i.test(action)) {
                clearChatSessionId();
            }
        },
        true
    );
})();

// Print Report: disable the button and show a blocking overlay until the PDF is ready.
(function () {
    var overlay = document.getElementById('report-generating-overlay');
    if (!overlay) return;

    var busyLink = null;

    function setBusy(busy) {
        overlay.hidden = !busy;
        overlay.setAttribute('aria-busy', busy ? 'true' : 'false');
        document.body.style.overflow = busy ? 'hidden' : '';
        if (busyLink) {
            busyLink.setAttribute('aria-busy', busy ? 'true' : 'false');
            if (!busy) busyLink = null;
        }
    }

    document.addEventListener('click', function (e) {
        var link = e.target.closest && e.target.closest('a[data-print-report]');
        if (!link) return;
        if (e.defaultPrevented) return;
        if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        if (overlay.getAttribute('aria-busy') === 'true') {
            e.preventDefault();
            return;
        }

        e.preventDefault();
        busyLink = link;
        setBusy(true);

        var generateUrl = link.href + (link.href.indexOf('?') >= 0 ? '&' : '?') + 'response=json';
        fetch(generateUrl, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
            .then(function (res) {
                if (!res.ok) throw new Error('print-report-failed');
                return res.json();
            })
            .then(function (data) {
                if (!data || !data.downloadUrl) throw new Error('print-report-failed');
                var a = document.createElement('a');
                a.href = data.downloadUrl;
                a.download = data.downloadFileName || 'report.pdf';
                document.body.appendChild(a);
                a.click();
                a.remove();
            })
            .catch(function () {
                window.alert(overlay.getAttribute('data-error') || 'Could not generate the report.');
            })
            .finally(function () {
                setBusy(false);
            });
    });
})();
