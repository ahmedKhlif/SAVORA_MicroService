// Theme management JavaScript
window.applyTheme = (isDark) => {
    const html = document.documentElement;
    if (isDark) {
        html.setAttribute('data-theme', 'dark');
        html.classList.add('dark-mode');
    } else {
        html.removeAttribute('data-theme');
        html.classList.remove('dark-mode');
    }
};

// Initialize theme on page load
window.initializeTheme = () => {
    const theme = localStorage.getItem('theme');
    if (theme === 'dark') {
        window.applyTheme(true);
    } else {
        window.applyTheme(false);
    }
};

// Listen for system theme changes (only if user hasn't set a preference)
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    const theme = localStorage.getItem('theme');
    if (!theme) {
        window.applyTheme(e.matches);
        localStorage.setItem('theme', e.matches ? 'dark' : 'light');
    }
});

// Initialize on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window.initializeTheme);
} else {
    window.initializeTheme();
}

