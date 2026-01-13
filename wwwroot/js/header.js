// components/header/header.js
document.addEventListener('DOMContentLoaded', function () {
    // Mobile menu toggle
    const mobileMenuButton = document.getElementById('mobile-menu-button');
    const mobileMenuIcon = document.getElementById('mobile-menu-icon');
    const mobileMenu = document.getElementById('mobile-menu');

    mobileMenuButton.addEventListener('click', function () {
        mobileMenu.classList.toggle('hidden');
        mobileMenuIcon.setAttribute('data-lucide', mobileMenu.classList.contains('hidden') ? 'menu' : 'x');
        lucide.createIcons();
    });

    // Theme toggle
    const themeToggle = document.getElementById('theme-toggle');
    const themeIcon = document.getElementById('theme-icon');

    // Check saved theme or use preferred color scheme
    const savedTheme = localStorage.getItem('theme') ||
        (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    setTheme(savedTheme);

    themeToggle.addEventListener('click', function () {
        const currentTheme = document.documentElement.classList.contains('dark') ? 'dark' : 'light';
        setTheme(currentTheme === 'light' ? 'dark' : 'light');
    });

    function setTheme(theme) {
        if (theme === 'dark') {
            document.documentElement.classList.add('dark');
            themeIcon.setAttribute('data-lucide', 'sun');
            localStorage.setItem('theme', 'dark');
        } else {
            document.documentElement.classList.remove('dark');
            themeIcon.setAttribute('data-lucide', 'moon');
            localStorage.setItem('theme', 'light');
        }
        lucide.createIcons();
    }

    // Language toggle
    const languageToggle = document.getElementById('language-toggle');
    const languageText = document.getElementById('language-text');

    // Check saved language
    const savedLanguage = localStorage.getItem('language') || 'en';
    setLanguage(savedLanguage);

    languageToggle.addEventListener('click', function () {
        const currentLanguage = localStorage.getItem('language') || 'en';
        setLanguage(currentLanguage === 'en' ? 'de' : 'en');
    });

    function setLanguage(language) {
        localStorage.setItem('language', language);
        languageText.textContent = language.toUpperCase();
        // Burada dil değişikliği için ek işlemler yapılabilir
    }

    // Active link highlighting
    function highlightActiveLink() {
        const currentPath = window.location.pathname;
        document.querySelectorAll('nav a').forEach(link => {
            if (link.getAttribute('href') === currentPath) {
                link.classList.add('text-green-600', 'border-b-2', 'border-green-600');
                link.classList.remove('text-gray-700', 'dark:text-gray-300');
            } else {
                link.classList.remove('text-green-600', 'border-b-2', 'border-green-600');
                link.classList.add('text-gray-700', 'dark:text-gray-300');
            }
        });
    }

    highlightActiveLink();
    window.addEventListener('popstate', highlightActiveLink);

    // Initialize Lucide icons
    lucide.createIcons();
});