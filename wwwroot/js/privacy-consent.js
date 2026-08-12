/**
 * PhysioTech Privacy & Terms settings
 * Guest: localStorage UI preference; GrantConsent from chatbot.js on first persist
 * Authenticated: AppUser.ChatHistoryEnabled in PostgreSQL; localStorage not authority
 */
(function () {
  "use strict";

  var STORAGE_KEY = "physiotech_privacyConsent";
  var CHAT_SESSION_KEY = "physiotech_n8n_chat_session";
  var PREFERENCES_API_URL = "/api/chatbot/preferences";
  var CHAT_PREF_API_URL = "/api/chatbot/preferences/chat-history";
  var VERSION = 3;
  var SHOW_DELAY_MS = 5000;

  var root = document.getElementById("ptConsentRoot");
  if (!root) return;

  var overlay = document.getElementById("ptConsentOverlay");
  var panel = document.getElementById("ptConsentPanel");
  var details = document.getElementById("ptConsentDetails");
  var chatToggle = document.getElementById("ptConsentChatHistory");
  var chatLabel = document.getElementById("ptConsentChatLabel");
  var btnAcceptSelected = document.getElementById("ptConsentAcceptSelected");
  var btnAcceptAll = document.getElementById("ptConsentAcceptAll");
  var btnReject = document.getElementById("ptConsentReject");
  var btnCustomize = document.getElementById("ptConsentCustomize");
  var btnClose = document.getElementById("ptConsentClose");

  var labelOn = root.getAttribute("data-label-on") || "ON";
  var labelOff = root.getAttribute("data-label-off") || "OFF";
  var autoShowTimer = null;
  var detailsOpen = false;
  var accountChatHistoryEnabled = null;
  var accountPrefsLoaded = false;

  function isAuthenticated() {
    return root.getAttribute("data-authenticated") === "1";
  }

  function defaultConsent(overrides) {
    return Object.assign(
      {
        version: VERSION,
        necessary: true,
        chatHistory: false,
        consented: true,
        timestamp: new Date().toISOString(),
      },
      overrides || {}
    );
  }

  function readConsent() {
    try {
      var raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      var parsed = JSON.parse(raw);
      if (!parsed || parsed.consented !== true) return null;
      return parsed;
    } catch (e) {
      return null;
    }
  }

  function isAuthSurface() {
    var path = (window.location.pathname || "").toLowerCase();
    return /(^|\/)auth(\/|$)/.test(path);
  }

  function applyDomChatAttr(enabled) {
    document.documentElement.setAttribute("data-consent-chat", enabled ? "1" : "0");
  }

  /**
   * Guest preference stays in localStorage only until the user actually chats.
   * GrantConsent runs from chatbot.js on first persist — avoids empty conversations.
   */
  function syncGuestChatConsentToServer() {
    /* no-op: do not create DB conversation on preference save / page load */
  }

  function saveAccountChatHistory(enabled) {
    accountChatHistoryEnabled = !!enabled;
    applyDomChatAttr(enabled);
    if (isAuthSurface()) return Promise.resolve();

    return fetch(CHAT_PREF_API_URL, {
      method: "POST",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      body: JSON.stringify({
        enabled: !!enabled,
      }),
    }).catch(function () {});
  }

  function writeGuestConsent(consent) {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(consent));
      applyDomChatAttr(!!consent.chatHistory);
      window.dispatchEvent(
        new CustomEvent("physiotech:consent-updated", { detail: consent })
      );
      syncGuestChatConsentToServer();
    } catch (e) {
      console.warn("PhysioTech consent could not be saved.", e);
    }
  }

  function syncToggleLabels() {
    if (chatLabel && chatToggle) {
      chatLabel.textContent = chatToggle.checked ? labelOn : labelOff;
      chatLabel.classList.toggle("is-on", chatToggle.checked);
    }
  }

  function applyConsentToUi(consent) {
    if (chatToggle) chatToggle.checked = !!consent.chatHistory;
    syncToggleLabels();
  }

  function setDetailsOpen(open) {
    detailsOpen = !!open;
    if (details) {
      details.hidden = !detailsOpen;
      details.classList.toggle("is-open", detailsOpen);
    }
    if (btnCustomize) {
      btnCustomize.setAttribute("aria-expanded", detailsOpen ? "true" : "false");
    }
  }

  function openModal(options) {
    options = options || {};
    if (autoShowTimer) {
      clearTimeout(autoShowTimer);
      autoShowTimer = null;
    }

    if (isAuthenticated()) {
      applyConsentToUi({ chatHistory: accountChatHistoryEnabled === true });
      setDetailsOpen(true);
    } else {
      var existing = readConsent();
      applyConsentToUi(existing || defaultConsent({ consented: false }));
      setDetailsOpen(!!options.forceDetails || !!existing);
    }

    if (overlay) {
      overlay.hidden = false;
      overlay.classList.add("is-visible");
    }
    document.body.classList.add("pt-consent-open");
    if (panel) {
      panel.focus({ preventScroll: true });
    }
  }

  function closeModal() {
    if (overlay) {
      overlay.classList.remove("is-visible");
      overlay.hidden = true;
    }
    document.body.classList.remove("pt-consent-open");
  }

  function saveFromUi(overrides) {
    var chatHistory = chatToggle ? !!chatToggle.checked : false;
    if (overrides) {
      if (typeof overrides.chatHistory === "boolean") chatHistory = overrides.chatHistory;
      if (chatToggle && typeof overrides.chatHistory === "boolean") {
        chatToggle.checked = overrides.chatHistory;
      }
      syncToggleLabels();
    }

    if (isAuthenticated()) {
      saveAccountChatHistory(chatHistory);
      try {
        localStorage.setItem(
          STORAGE_KEY,
          JSON.stringify(defaultConsent({ chatHistory: chatHistory }))
        );
      } catch (e) {
        /* ignore */
      }
      window.dispatchEvent(
        new CustomEvent("physiotech:consent-updated", {
          detail: { chatHistory: chatHistory, account: true },
        })
      );
    } else {
      writeGuestConsent(defaultConsent({ chatHistory: chatHistory }));
    }
    closeModal();
  }

  function scheduleFirstVisit() {
    if (isAuthenticated()) return;
    if (readConsent()) return;
    autoShowTimer = setTimeout(function () {
      openModal({ forceDetails: false });
    }, SHOW_DELAY_MS);
  }

  function loadAccountPreferences() {
    return fetch(PREFERENCES_API_URL, {
      method: "GET",
      credentials: "same-origin",
      headers: { Accept: "application/json" },
    })
      .then(function (r) {
        return r.json().catch(function () {
          return null;
        });
      })
      .then(function (data) {
        if (!data || !data.success || !data.isAuthenticated) {
          accountPrefsLoaded = true;
          accountChatHistoryEnabled = false;
          return;
        }
        accountPrefsLoaded = true;
        accountChatHistoryEnabled = !!data.chatHistoryEnabled;
        applyDomChatAttr(accountChatHistoryEnabled);
      })
      .catch(function () {
        accountPrefsLoaded = true;
      });
  }

  window.PhysioTechConsent = {
    open: function () {
      openModal({ forceDetails: true });
    },
    get: readConsent,
    storageKey: STORAGE_KEY,
    isAuthenticated: isAuthenticated,
    isChatHistoryEnabled: function () {
      if (isAuthenticated()) {
        return accountChatHistoryEnabled === true;
      }
      var c = readConsent();
      return !!(c && c.chatHistory === true);
    },
  };

  if (chatToggle) {
    chatToggle.addEventListener("change", syncToggleLabels);
  }

  if (btnCustomize) {
    btnCustomize.addEventListener("click", function () {
      setDetailsOpen(!detailsOpen);
    });
  }

  if (btnAcceptSelected) {
    btnAcceptSelected.addEventListener("click", function () {
      saveFromUi();
    });
  }

  if (btnAcceptAll) {
    btnAcceptAll.addEventListener("click", function () {
      saveFromUi({ chatHistory: true });
    });
  }

  if (btnReject) {
    btnReject.addEventListener("click", function () {
      saveFromUi({ chatHistory: false });
    });
  }

  if (btnClose) {
    btnClose.addEventListener("click", function () {
      if (isAuthenticated()) {
        closeModal();
        return;
      }
      if (!readConsent()) {
        saveFromUi({ chatHistory: false });
      } else {
        closeModal();
      }
    });
  }

  if (overlay) {
    overlay.addEventListener("click", function (e) {
      if (e.target === overlay) {
        if (isAuthenticated() || readConsent()) closeModal();
      }
    });
  }

  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && overlay && overlay.classList.contains("is-visible")) {
      if (isAuthenticated() || readConsent()) closeModal();
    }
  });

  document.querySelectorAll("[data-open-privacy-settings]").forEach(function (el) {
    el.addEventListener("click", function (e) {
      e.preventDefault();
      openModal({ forceDetails: true });
    });
  });

  if (isAuthenticated()) {
    loadAccountPreferences();
  } else {
    var existing = readConsent();
    if (existing) {
      applyDomChatAttr(!!existing.chatHistory);
      syncGuestChatConsentToServer();
    }
    scheduleFirstVisit();
  }
})();
