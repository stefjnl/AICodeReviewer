// AI Code Reviewer - Theme Manager
// Handles dark/light theme switching and persistence

/**
 * Theme Manager - Manages application theme (dark/light mode)
 */
class ThemeManager {
    constructor() {
        this.currentTheme = this.getStoredTheme() || this.getSystemPreference();
        this.themeToggle = document.getElementById('theme-toggle');
        this.lightIcon = document.getElementById('theme-toggle-light-icon');
        this.darkIcon = document.getElementById('theme-toggle-dark-icon');
        
        this.init();
    }

    /**
     * Initialize the theme manager
     */
    init() {
        // Apply the initial theme
        this.applyTheme(this.currentTheme);
        
        // Set up event listeners
        if (this.themeToggle) {
            this.themeToggle.addEventListener('click', () => this.toggleTheme());
        }

        // Listen for system theme changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            // Only update if user hasn't manually set a preference
            if (!this.getStoredTheme()) {
                this.currentTheme = e.matches ? 'dark' : 'light';
                this.applyTheme(this.currentTheme);
            }
        });

        console.log(`🎨 Theme initialized: ${this.currentTheme}`);
    }

    /**
     * Get the user's stored theme preference
     * @returns {string|null} The stored theme or null if not set
     */
    getStoredTheme() {
        return localStorage.getItem('theme');
    }

    /**
     * Get the system's color scheme preference
     * @returns {string} 'dark' or 'light'
     */
    getSystemPreference() {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    /**
     * Store the user's theme preference
     * @param {string} theme - The theme to store ('dark' or 'light')
     */
    storeTheme(theme) {
        localStorage.setItem('theme', theme);
    }

    /**
     * Apply the specified theme to the document
     * @param {string} theme - The theme to apply ('dark' or 'light')
     */
    applyTheme(theme) {
        const html = document.documentElement;
        
        // Remove existing theme classes
        html.classList.remove('light-theme', 'dark-theme');
        
        // Add the new theme class
        html.classList.add(`${theme}-theme`);
        
        // Update the toggle button icons
        this.updateToggleIcons(theme);
        
        // Update Tailwind classes for body background and text
        this.updateTailwindClasses(theme);
        
        console.log(`🎨 Theme applied: ${theme}`);
    }

    /**
     * Update the theme toggle button icons based on current theme
     * @param {string} theme - The current theme
     */
    updateToggleIcons(theme) {
        if (this.lightIcon && this.darkIcon) {
            if (theme === 'dark') {
                // Show light icon (sun) when in dark mode
                this.lightIcon.classList.remove('hidden');
                this.darkIcon.classList.add('hidden');
            } else {
                // Show dark icon (moon) when in light mode
                this.lightIcon.classList.add('hidden');
                this.darkIcon.classList.remove('hidden');
            }
        }
    }

    /**
     * Update Tailwind classes for body background and text
     * @param {string} theme - The current theme
     */
    updateTailwindClasses(theme) {
        const body = document.body;
        
        // Remove existing theme-related classes
        body.classList.remove('bg-surface', 'bg-gray-900', 'text-gray-900', 'text-gray-100');
        
        if (theme === 'dark') {
            // Dark mode classes
            body.classList.add('bg-gray-900', 'text-gray-100');
        } else {
            // Light mode classes
            body.classList.add('bg-surface', 'text-gray-900');
        }
    }

    /**
     * Toggle between dark and light themes
     */
    toggleTheme() {
        this.currentTheme = this.currentTheme === 'dark' ? 'light' : 'dark';
        this.applyTheme(this.currentTheme);
        this.storeTheme(this.currentTheme);
        
        console.log(`🎨 Theme toggled: ${this.currentTheme}`);
    }

    /**
     * Get the current theme
     * @returns {string} The current theme ('dark' or 'light')
     */
    getCurrentTheme() {
        return this.currentTheme;
    }

    /**
     * Set a specific theme
     * @param {string} theme - The theme to set ('dark' or 'light')
     */
    setTheme(theme) {
        if (theme === 'dark' || theme === 'light') {
            this.currentTheme = theme;
            this.applyTheme(this.currentTheme);
            this.storeTheme(this.currentTheme);
            console.log(`🎨 Theme set: ${this.currentTheme}`);
        } else {
            console.warn(`⚠️ Invalid theme specified: ${theme}`);
        }
    }

    /**
     * Reset to system preference
     */
    resetToSystemPreference() {
        localStorage.removeItem('theme');
        this.currentTheme = this.getSystemPreference();
        this.applyTheme(this.currentTheme);
        console.log(`🎨 Theme reset to system preference: ${this.currentTheme}`);
    }
}

// Export the class instead of creating an instance immediately
export default ThemeManager;