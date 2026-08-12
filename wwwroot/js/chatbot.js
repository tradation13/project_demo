/**
 * PhysioTech live chatbot → n8n Chat Trigger (Embedded / webhook).
 * Session is kept in sessionStorage (shared key with privacy consent sync).
 * Persistence to MVC is best-effort and must never break live chat.
 * SessionId is cleared on Logout / Login page so identities do not share conversations.
 */
(function () {
  "use strict";

  var SESSION_KEY = "physiotech_n8n_chat_session";
  var PERSIST_BASE = "/api/chatbot";

  function ready(fn) {
    if (document.readyState === "loading") {
      document.addEventListener("DOMContentLoaded", fn);
    } else {
      fn();
    }
  }

  function uuid() {
    if (window.crypto && typeof window.crypto.randomUUID === "function") {
      return window.crypto.randomUUID();
    }
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
      var r = (Math.random() * 16) | 0;
      var v = c === "x" ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }

  function getSessionId() {
    try {
      var existing = sessionStorage.getItem(SESSION_KEY);
      if (existing) return existing;
      var id = uuid();
      sessionStorage.setItem(SESSION_KEY, id);
      return id;
    } catch (e) {
      return uuid();
    }
  }

  function clearSessionId() {
    try {
      sessionStorage.removeItem(SESSION_KEY);
    } catch (e) {
      /* ignore */
    }
  }

  function hasLocalChatHistoryPreference() {
    try {
      if (window.PhysioTechConsent && typeof window.PhysioTechConsent.isChatHistoryEnabled === "function") {
        return window.PhysioTechConsent.isChatHistoryEnabled() === true;
      }
      if (window.PhysioTechConsent && typeof window.PhysioTechConsent.get === "function") {
        var consent = window.PhysioTechConsent.get();
        return !!(consent && consent.chatHistory === true);
      }
      var raw = localStorage.getItem("physiotech_privacyConsent");
      if (!raw) return false;
      var parsed = JSON.parse(raw);
      return !!(parsed && parsed.chatHistory === true);
    } catch (e) {
      return false;
    }
  }

  function grantConsentForSession(sessionId) {
    // Session GrantConsent only — do not flip AppUser.ChatHistoryEnabled here.
    return fetch(PERSIST_BASE + "/consent", {
      method: "POST",
      credentials: "same-origin",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      body: JSON.stringify({ sessionId: sessionId }),
    }).catch(function () {
      /* optional */
    });
  }

  /**
   * Best-effort persistence. Server decides consent via PostgreSQL.
   * Does not send UserId/UserType/Role/IP/ConsentGiven.
   * Guests: GrantConsent right before first persist (avoids empty rows on page load).
   * Authenticated: server creates conversation lazily on first message when ChatHistoryEnabled.
   * On identity mismatch: clear SessionId, re-consent if needed, retry once.
   */
  function persistChatMessage(kind, messageText, isRecovery) {
    try {
      var path =
        kind === "ai" ? PERSIST_BASE + "/messages/ai" : PERSIST_BASE + "/messages/user";
      var sessionId = getSessionId();
      var isAuth =
        window.PhysioTechConsent &&
        typeof window.PhysioTechConsent.isAuthenticated === "function" &&
        window.PhysioTechConsent.isAuthenticated();

      var ready = Promise.resolve();
      if (!isAuth && hasLocalChatHistoryPreference()) {
        ready = grantConsentForSession(sessionId);
      }

      ready
        .then(function () {
          return fetch(path, {
            method: "POST",
            credentials: "same-origin",
            headers: {
              "Content-Type": "application/json",
              Accept: "application/json",
            },
            body: JSON.stringify({
              sessionId: sessionId,
              message: messageText,
            }),
          });
        })
        .then(function (response) {
          return response.json().catch(function () {
            return null;
          });
        })
        .then(function (data) {
          if (!data || !data.skippedDueToIdentityMismatch || isRecovery) return;

          clearSessionId();
          var newSessionId = getSessionId();
          var retryReady = Promise.resolve();
          if (hasLocalChatHistoryPreference()) {
            retryReady = grantConsentForSession(newSessionId);
          }
          return retryReady.then(function () {
            persistChatMessage(kind, messageText, true);
          });
        })
        .catch(function () {
          /* ignore — chat must continue */
        });
    } catch (e) {
      /* ignore */
    }
  }

  function escapeHtml(text) {
    return String(text)
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function isSafeHttpUrl(url) {
    try {
      var parsed = new URL(url);
      return parsed.protocol === "http:" || parsed.protocol === "https:";
    } catch (e) {
      return false;
    }
  }

  /**
   * Escape all text, preserve line breaks, and safely convert Markdown links:
   * [label](https://example.com) → <a href="..." ...>label</a>
   * Only http/https URLs are allowed. No raw HTML from the model is trusted.
   */
  function formatMessageHtml(text) {
    var source = String(text == null ? "" : text);
    var linkRe = /\[([^\]]+)\]\(([^)\s]+)\)/g;
    var result = "";
    var lastIndex = 0;
    var match;

    while ((match = linkRe.exec(source)) !== null) {
      result += escapeHtml(source.slice(lastIndex, match.index)).replace(/\n/g, "<br>");

      var label = match[1];
      var url = match[2];

      if (isSafeHttpUrl(url)) {
        result +=
          '<a href="' +
          escapeHtml(url) +
          '" target="_blank" rel="noopener noreferrer" dir="auto">' +
          escapeHtml(label) +
          "</a>";
      } else {
        // Keep original markdown text visible but escaped (not clickable).
        result += escapeHtml(match[0]).replace(/\n/g, "<br>");
      }

      lastIndex = match.index + match[0].length;
    }

    result += escapeHtml(source.slice(lastIndex)).replace(/\n/g, "<br>");
    return result;
  }

  function extractReply(payload) {
    if (payload == null) return null;
    if (typeof payload === "string") {
      var trimmed = payload.trim();
      if (!trimmed) return null;
      try {
        return extractReply(JSON.parse(trimmed));
      } catch (e) {
        return trimmed;
      }
    }
    if (Array.isArray(payload)) {
      for (var i = payload.length - 1; i >= 0; i--) {
        var fromItem = extractReply(payload[i]);
        if (fromItem) return fromItem;
      }
      return null;
    }
    if (typeof payload === "object") {
      var keys = ["output", "text", "response", "message", "answer"];
      for (var k = 0; k < keys.length; k++) {
        var val = payload[keys[k]];
        if (typeof val === "string" && val.trim()) return val.trim();
        if (val && typeof val === "object") {
          var nested = extractReply(val);
          if (nested) return nested;
        }
      }
      if (payload.data != null) return extractReply(payload.data);
    }
    return null;
  }

  ready(function () {
    var root = document.getElementById("cbRoot");
    if (!root) return;

    var webhookUrl = (root.getAttribute("data-webhook-url") || "").trim();
    var i18n = {
      expand: root.getAttribute("data-i18n-expand") || "Expand",
      collapse: root.getAttribute("data-i18n-collapse") || "Collapse",
      typing: root.getAttribute("data-i18n-typing") || "Assistant is typing…",
      errorGeneric: root.getAttribute("data-i18n-error") || "Sorry, something went wrong.",
      errorEmpty: root.getAttribute("data-i18n-empty") || "Please enter a message.",
      errorConfig: root.getAttribute("data-i18n-config") || "Chat is temporarily unavailable.",
    };

    var panel = document.getElementById("cbPanel");
    var expandEl = document.getElementById("cbExpand");
    var toggle = document.getElementById("cbToggle");
    var closeEl = document.getElementById("cbClose");
    var notif = document.getElementById("cbNotif");
    var messagesEl = document.getElementById("cbMessages");
    var form = document.getElementById("cbForm");
    var input = document.getElementById("cbInput");
    var sendBtn = document.getElementById("cbSend");

    if (!panel || !toggle || !messagesEl || !form || !input || !sendBtn) return;

    var isSending = false;

    function openPanel() {
      panel.classList.add("open");
      toggle.classList.add("open");
      toggle.setAttribute("aria-expanded", "true");
      if (notif) notif.style.display = "none";
      setTimeout(function () {
        input.focus();
        scrollToBottom();
      }, 50);
    }

    function closePanel() {
      panel.classList.remove("open");
      toggle.classList.remove("open");
      toggle.setAttribute("aria-expanded", "false");
    }

    function scrollToBottom() {
      messagesEl.scrollTop = messagesEl.scrollHeight;
    }

    function appendBubble(role, text) {
      var row = document.createElement("div");
      row.className = "cb-msg cb-msg--" + role;
      var bubble = document.createElement("div");
      bubble.className = "cb-bubble";
      bubble.setAttribute("dir", "auto");
      bubble.innerHTML = formatMessageHtml(text);
      row.appendChild(bubble);
      messagesEl.appendChild(row);
      scrollToBottom();
      return row;
    }

    function setTyping(show) {
      var existing = document.getElementById("cbTyping");
      if (!show) {
        if (existing) existing.remove();
        return;
      }
      if (existing) return;
      var row = document.createElement("div");
      row.id = "cbTyping";
      row.className = "cb-msg cb-msg--ai";
      row.innerHTML =
        '<div class="cb-bubble cb-bubble--typing" aria-live="polite">' +
        '<span class="cb-dot"></span><span class="cb-dot"></span><span class="cb-dot"></span>' +
        '<span class="cb-typing-label">' +
        escapeHtml(i18n.typing) +
        "</span></div>";
      messagesEl.appendChild(row);
      scrollToBottom();
    }

    function setBusy(busy) {
      isSending = busy;
      input.disabled = busy;
      sendBtn.disabled = busy;
      sendBtn.classList.toggle("is-busy", busy);
    }

    async function sendMessage(rawText) {
      var text = (rawText || "").trim();
      if (!text) {
        appendBubble("system", i18n.errorEmpty);
        return;
      }
      if (!webhookUrl) {
        appendBubble("system", i18n.errorConfig);
        return;
      }
      if (isSending) return;

      appendBubble("user", text);
      input.value = "";
      autoResize();
      setBusy(true);
      setTyping(true);

      // Persist user message (server skips if no PostgreSQL consent). Never block n8n.
      persistChatMessage("user", text);

      try {
        var response = await fetch(webhookUrl, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Accept: "application/json, text/plain, */*",
          },
          body: JSON.stringify({
            action: "sendMessage",
            chatInput: text,
            sessionId: getSessionId(),
          }),
        });

        var raw = await response.text();
        var data = null;
        if (raw) {
          try {
            data = JSON.parse(raw);
          } catch (e) {
            data = raw;
          }
        }

        setTyping(false);

        if (!response.ok) {
          appendBubble("system", i18n.errorGeneric);
          return;
        }

        var reply = extractReply(data);
        if (!reply) {
          appendBubble("system", i18n.errorGeneric);
          return;
        }

        appendBubble("ai", reply);
        // Option A: persist AI text returned by n8n (best-effort).
        persistChatMessage("ai", reply);
      } catch (err) {
        setTyping(false);
        appendBubble("system", i18n.errorGeneric);
      } finally {
        setBusy(false);
        input.focus();
      }
    }

    function autoResize() {
      input.style.height = "auto";
      var next = Math.min(input.scrollHeight, 110);
      input.style.height = next + "px";
    }

    toggle.addEventListener("click", function () {
      if (panel.classList.contains("open")) closePanel();
      else openPanel();
    });

    if (closeEl) closeEl.addEventListener("click", closePanel);

    if (expandEl) {
      expandEl.addEventListener("click", function () {
        var expanded = panel.classList.toggle("expanded");
        expandEl.setAttribute("aria-label", expanded ? i18n.collapse : i18n.expand);
        var icon = expandEl.querySelector("i");
        if (icon) icon.className = expanded ? "ti ti-minimize" : "ti ti-maximize";
        scrollToBottom();
      });
    }

    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape" && panel.classList.contains("open")) closePanel();
    });

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      sendMessage(input.value);
    });

    input.addEventListener("keydown", function (e) {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        sendMessage(input.value);
      }
    });

    input.addEventListener("input", autoResize);
  });
})();
