// components/header/header.js
document.addEventListener("DOMContentLoaded", function () {
  function refreshIcons() {
    if (window.lucide && typeof window.lucide.createIcons === "function") {
      window.lucide.createIcons();
    }
  }

  // Mobile menu toggle
  const mobileMenuButton = document.getElementById("mobile-menu-button");
  const mobileMenuIcon = document.getElementById("mobile-menu-icon");
  const mobileMenu = document.getElementById("mobile-menu");

  if (mobileMenuButton && mobileMenu) {
    mobileMenuButton.addEventListener("click", function () {
      mobileMenu.classList.toggle("hidden");
      if (mobileMenuIcon) {
        mobileMenuIcon.setAttribute(
          "data-lucide",
          mobileMenu.classList.contains("hidden") ? "menu" : "x",
        );
      }
      refreshIcons();
    });
  }

  // Theme toggle
  const themeToggle = document.getElementById("theme-toggle");
  const themeIcon = document.getElementById("theme-icon");

  // Check saved theme or use preferred color scheme
  const savedTheme =
    localStorage.getItem("theme") ||
    (window.matchMedia("(prefers-color-scheme: dark)").matches
      ? "dark"
      : "light");
  if (themeIcon) {
    setTheme(savedTheme);
  }

  if (themeToggle && themeIcon) {
    themeToggle.addEventListener("click", function () {
      const currentTheme = document.documentElement.classList.contains("dark")
        ? "dark"
        : "light";
      setTheme(currentTheme === "light" ? "dark" : "light");
    });
  }

  function setTheme(theme) {
    if (theme === "dark") {
      document.documentElement.classList.add("dark");
      themeIcon.setAttribute("data-lucide", "sun");
      localStorage.setItem("theme", "dark");
    } else {
      document.documentElement.classList.remove("dark");
      themeIcon.setAttribute("data-lucide", "moon");
      localStorage.setItem("theme", "light");
    }
    refreshIcons();
  }

  // Active link highlighting
  function highlightActiveLink() {
    const currentPath = window.location.pathname;
    document.querySelectorAll("nav a").forEach((link) => {
      if (link.getAttribute("href") === currentPath) {
        link.classList.add("text-green-600", "border-b-2", "border-green-600");
        link.classList.remove("text-gray-700", "dark:text-gray-300");
      } else {
        link.classList.remove(
          "text-green-600",
          "border-b-2",
          "border-green-600",
        );
        link.classList.add("text-gray-700", "dark:text-gray-300");
      }
    });
  }

  highlightActiveLink();
  window.addEventListener("popstate", highlightActiveLink);

  // Initialize Lucide icons
  refreshIcons();
});
