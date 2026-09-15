/**
 * Load Google Maps embed only after an explicit click (TDDDG § 25).
 * Remembers the choice for the current browser session.
 */
(function () {
  "use strict";

  var STORAGE_KEY = "physiotech_maps_loaded";

  function sessionAllowed() {
    try {
      return sessionStorage.getItem(STORAGE_KEY) === "1";
    } catch (e) {
      return false;
    }
  }

  function remember() {
    try {
      sessionStorage.setItem(STORAGE_KEY, "1");
    } catch (e) {
      /* ignore */
    }
  }

  function loadGate(gate) {
    if (!gate || gate.classList.contains("is-loaded")) return;

    var iframe = gate.querySelector("iframe");
    var src = gate.getAttribute("data-map-src");
    if (!iframe || !src) return;

    iframe.setAttribute("src", src);
    iframe.removeAttribute("hidden");
    gate.classList.add("is-loaded");
    remember();
  }

  function bindGate(gate) {
    if (gate.getAttribute("data-map-bound") === "1") return;
    gate.setAttribute("data-map-bound", "1");

    if (sessionAllowed()) {
      loadGate(gate);
      return;
    }

    var button = gate.querySelector("[data-map-load]");
    if (button) {
      button.addEventListener("click", function () {
        loadGate(gate);
      });
    }
  }

  function init() {
    document.querySelectorAll("[data-map-gate]").forEach(bindGate);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
