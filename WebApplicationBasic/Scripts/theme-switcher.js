/**
 * Theme Switcher - Bootstrap 5.3 Dark Mode
 * Gerencia tema com 3 estados: light, dark, auto
 */
(function () {
    'use strict';

    const STORAGE_KEY = 'theme-preference';
    const THEMES = { LIGHT: 'light', DARK: 'dark', AUTO: 'auto' };

    function getSystemPreference() {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
            ? THEMES.DARK
            : THEMES.LIGHT;
    }

    function getStoredPreference() {
        try {
            return localStorage.getItem(STORAGE_KEY) || THEMES.AUTO;
        } catch (e) {
            return THEMES.AUTO;
        }
    }

    function savePreference(theme) {
        try {
            localStorage.setItem(STORAGE_KEY, theme);
        } catch (e) {
            console.warn('Não foi possível salvar preferência de tema:', e);
        }
    }

    function getEffectiveTheme(preference) {
        return preference === THEMES.AUTO ? getSystemPreference() : preference;
    }

    function applyTheme(preference) {
        const effectiveTheme = getEffectiveTheme(preference);
        document.documentElement.setAttribute('data-bs-theme', effectiveTheme);

        // Dispara evento para outros componentes (ex: Chart.js)
        window.dispatchEvent(new CustomEvent('themechange', {
            detail: { theme: effectiveTheme, preference: preference }
        }));
    }

    function updateToggleUI(preference) {
        const effectiveTheme = getEffectiveTheme(preference);

        // Atualiza ícones
        document.querySelectorAll('[data-theme-icon]').forEach(icon => {
            icon.classList.remove('bi-sun-fill', 'bi-moon-fill', 'bi-circle-half');
            if (preference === THEMES.AUTO) {
                icon.classList.add('bi-circle-half');
            } else if (effectiveTheme === THEMES.DARK) {
                icon.classList.add('bi-moon-fill');
            } else {
                icon.classList.add('bi-sun-fill');
            }
        });

        // Atualiza labels
        document.querySelectorAll('[data-theme-label]').forEach(label => {
            if (preference === THEMES.AUTO) {
                label.textContent = 'Auto';
            } else if (effectiveTheme === THEMES.DARK) {
                label.textContent = 'Escuro';
            } else {
                label.textContent = 'Claro';
            }
        });
    }

    function cycleTheme() {
        const current = getStoredPreference();
        let newTheme;

        switch (current) {
            case THEMES.LIGHT:
                newTheme = THEMES.DARK;
                break;
            case THEMES.DARK:
                newTheme = THEMES.AUTO;
                break;
            default:
                newTheme = THEMES.LIGHT;
        }

        savePreference(newTheme);
        applyTheme(newTheme);
        updateToggleUI(newTheme);
    }

    function setTheme(theme) {
        if (Object.values(THEMES).includes(theme)) {
            savePreference(theme);
            applyTheme(theme);
            updateToggleUI(theme);
        }
    }

    function init() {
        const preference = getStoredPreference();
        applyTheme(preference);

        const setupUI = () => {
            updateToggleUI(preference);

            // Event listeners para botões de toggle
            document.querySelectorAll('[data-theme-toggle="cycle"]').forEach(btn => {
                btn.addEventListener('click', cycleTheme);
            });

            document.querySelectorAll('[data-theme-value]').forEach(btn => {
                btn.addEventListener('click', () => setTheme(btn.dataset.themeValue));
            });
        };

        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', setupUI);
        } else {
            setupUI();
        }

        // Observa mudança de preferência do sistema
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
                if (getStoredPreference() === THEMES.AUTO) {
                    applyTheme(THEMES.AUTO);
                    updateToggleUI(THEMES.AUTO);
                }
            });
        }

        // Sincroniza entre abas
        window.addEventListener('storage', (e) => {
            if (e.key === STORAGE_KEY) {
                const newTheme = e.newValue || THEMES.AUTO;
                applyTheme(newTheme);
                updateToggleUI(newTheme);
            }
        });
    }

    init();

    // API pública
    window.ThemeSwitcher = {
        cycle: cycleTheme,
        set: setTheme,
        get: getStoredPreference,
        getEffective: () => getEffectiveTheme(getStoredPreference()),
        THEMES: THEMES
    };
})();
