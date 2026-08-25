/**
 * Colour scheme.
 *
 * The scheme is already applied by the inline boot script in index.html, which runs before
 * first paint. This module exists to keep the C# ThemeService in agreement with it and to
 * react when the operating system's scheme changes mid-session.
 *
 * @module theme
 */

const STORAGE_KEY = 'mh-theme';

const systemDark = window.matchMedia('(prefers-color-scheme: dark)');

/** @type {{ invokeMethodAsync: (name: string, arg: string) => Promise<void> } | null} */
let owner = null;

function onSystemChange() {
    // Only relevant while the visitor is following the system. An explicit choice wins.
    if (readPreference() === null) {
        const resolved = systemDark.matches ? 'dark' : 'light';
        document.documentElement.dataset.theme = resolved;
        owner?.invokeMethodAsync('OnSystemSchemeChanged', resolved);
    }
}

/**
 * Registers for system scheme changes.
 *
 * @param {{ invokeMethodAsync: (name: string, arg: string) => Promise<void> }} dotNetOwner
 * @returns {'light' | 'dark'} The scheme currently applied to the document.
 */
export function initialise(dotNetOwner) {
    owner = dotNetOwner;
    systemDark.addEventListener('change', onSystemChange);

    return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light';
}

/**
 * @returns {'light' | 'dark' | null} The stored choice, or null when following the system.
 */
export function readPreference() {
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        return stored === 'light' || stored === 'dark' ? stored : null;
    } catch {
        // Private browsing modes and blocked third-party storage both throw here.
        // Following the system is a perfectly good fallback.
        return null;
    }
}

/**
 * Applies and persists a preference.
 *
 * @param {'light' | 'dark' | 'system'} preference
 * @returns {'light' | 'dark'} The resulting scheme.
 */
export function apply(preference) {
    const resolved = preference === 'system'
        ? (systemDark.matches ? 'dark' : 'light')
        : preference;

    document.documentElement.dataset.theme = resolved;

    try {
        if (preference === 'system') {
            localStorage.removeItem(STORAGE_KEY);
        } else {
            localStorage.setItem(STORAGE_KEY, preference);
        }
    } catch {
        // Storage unavailable. The choice still applies for this session.
    }

    return resolved;
}

export function shutdown() {
    systemDark.removeEventListener('change', onSystemChange);
    owner = null;
}
