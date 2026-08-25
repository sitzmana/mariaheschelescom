/*
    Applies the colour scheme before the first paint.

    Loaded as a plain synchronous <script src> in <head>, so it still runs before the browser
    paints anything. Without it, a visitor on a dark-mode device gets a full-screen white flash
    for the second or so it takes WebAssembly to boot.

    It lives in a file rather than inline in index.html for one reason: the Content Security
    Policy forbids inline script. Allowing it would mean either 'unsafe-inline', which defeats
    much of the policy, or a sha256 hash that silently breaks the site the next time anyone
    edits these few lines. One extra request on a cached, HTTP/2 connection is the cheaper
    trade.

    Keep this dependency-free and synchronous. Deferring it reintroduces the flash.
*/
(function () {
    try {
        var stored = localStorage.getItem('mh-theme');
        var theme = (stored === 'light' || stored === 'dark')
            ? stored
            : (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
        document.documentElement.dataset.theme = theme;
    } catch (e) {
        // Storage blocked (private mode, blocked third-party storage). The document default applies.
    }
})();
