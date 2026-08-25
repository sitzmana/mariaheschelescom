/**
 * Motion engine.
 *
 * Every scroll-driven effect on the site is served by the two shared observers created here.
 * The design constraints, in priority order:
 *
 *   1. One IntersectionObserver and one requestAnimationFrame loop for the entire page,
 *      no matter how many elements register. Per-element scroll listeners are the classic
 *      way to make a beautiful site feel cheap.
 *   2. Reads and writes are batched. All getBoundingClientRect() calls happen together,
 *      then all style writes happen together, so the browser never has to flush layout in
 *      the middle of a frame.
 *   3. The loop stops completely when nothing is on screen. An idle page costs nothing.
 *   4. Results are published as CSS custom properties, never as JavaScript-applied styles.
 *      CSS decides what a progress value means, which keeps behaviour and presentation in
 *      the layer each belongs to.
 *
 * @module motion
 */

/** Elements awaiting their entrance animation. @type {Map<Element, { once: boolean }>} */
const revealTargets = new Map();

/** Elements whose scroll progress is published continuously. @type {Map<Element, { mode: string, visible: boolean, progress: number }>} */
const progressTargets = new Map();

const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');

/** @type {IntersectionObserver | null} */
let revealObserver = null;

/** @type {IntersectionObserver | null} */
let visibilityObserver = null;

/** @type {number} rAF handle, 0 when the loop is parked. */
let frame = 0;

/** Number of progress targets currently on screen. The loop runs only while this is above zero. */
let visibleCount = 0;

// ---------------------------------------------------------------------------
// Reveal on scroll
// ---------------------------------------------------------------------------

/**
 * How far up from the bottom of the viewport the trigger line sits, as a percentage of
 * viewport height. Without it, content on a tall display reveals while it is still a sliver
 * tall and the animation is over before it is legible.
 *
 * Fixed rather than per-element: one shared observer can only have one configuration, and a
 * consistent trigger line is what makes a long page feel like one document rather than a
 * stack of independently animating sections.
 */
const REVEAL_INSET_PERCENT = 8;

function ensureRevealObserver() {
    if (revealObserver) {
        return revealObserver;
    }

    revealObserver = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                const config = revealTargets.get(entry.target);
                if (!config) {
                    continue;
                }

                // `boundingClientRect.top < 0` catches an element that is already above the
                // viewport: a restored scroll position, a deep link, or a reload part way
                // down the page. Such an element never intersects, so without this it would
                // stay invisible forever — which is exactly what a fractional threshold
                // combined with scroll restoration used to cause here.
                const shouldReveal = entry.isIntersecting || entry.boundingClientRect.top < 0;

                if (shouldReveal) {
                    entry.target.classList.add('is-revealed');

                    if (config.once) {
                        revealObserver.unobserve(entry.target);
                        revealTargets.delete(entry.target);
                    }
                } else if (!config.once) {
                    entry.target.classList.remove('is-revealed');
                }
            }
        },
        {
            // Deliberately zero. A fractional threshold can never be met by an element
            // taller than the viewport, which silently breaks every full-height hero and
            // long section on the site. The inset below controls the trigger point instead.
            threshold: 0,
            rootMargin: `0px 0px -${REVEAL_INSET_PERCENT}% 0px`,
        },
    );

    return revealObserver;
}

/**
 * Reveals an element the first time it scrolls into view.
 *
 * @param {Element} element
 * @param {boolean} once Stop observing after the first reveal.
 */
export function observeReveal(element, once) {
    if (!element) {
        return;
    }

    // Someone who has asked for reduced motion still needs to see the content. Mark it
    // revealed immediately and skip the animation entirely rather than observing it.
    if (reducedMotion.matches) {
        element.classList.add('is-revealed');
        return;
    }

    revealTargets.set(element, { once });
    ensureRevealObserver().observe(element);
}

// ---------------------------------------------------------------------------
// Scroll-linked progress
// ---------------------------------------------------------------------------

function ensureVisibilityObserver() {
    if (visibilityObserver) {
        return visibilityObserver;
    }

    visibilityObserver = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                const state = progressTargets.get(entry.target);
                if (!state || state.visible === entry.isIntersecting) {
                    continue;
                }

                state.visible = entry.isIntersecting;
                visibleCount += entry.isIntersecting ? 1 : -1;
            }

            if (visibleCount > 0) {
                start();
            }
        },
        // A full viewport of margin on each side means progress is already correct by the
        // time an element becomes visible, rather than snapping into place on entry.
        { rootMargin: '100% 0px 100% 0px' },
    );

    return visibilityObserver;
}

/**
 * Publishes an element's scroll progress into its own `--progress` custom property.
 *
 * @param {Element} element
 * @param {'cover' | 'contain'} mode See the ProgressMode enum in MotionInterop.cs.
 */
export function trackProgress(element, mode) {
    if (!element || reducedMotion.matches) {
        // With reduced motion the value is pinned mid-travel, which renders every effect in
        // its neutral resting state instead of at an arbitrary extreme.
        element?.style.setProperty('--progress', '0.5');
        return;
    }

    progressTargets.set(element, { mode, visible: false, progress: -1 });
    ensureVisibilityObserver().observe(element);
}

/** Stops all observation of an element. Safe for elements that were never registered. */
export function release(element) {
    if (!element) {
        return;
    }

    if (revealTargets.delete(element)) {
        revealObserver?.unobserve(element);
    }

    const state = progressTargets.get(element);
    if (state) {
        if (state.visible) {
            visibleCount -= 1;
        }

        progressTargets.delete(element);
        visibilityObserver?.unobserve(element);
    }
}

/** @returns {boolean} Whether the visitor has asked their system to reduce motion. */
export function prefersReducedMotion() {
    return reducedMotion.matches;
}

// ---------------------------------------------------------------------------
// The frame loop
// ---------------------------------------------------------------------------

function measure() {
    const viewportHeight = window.innerHeight;

    // Phase one: read. Every geometry query happens here, before any style is written,
    // so the browser computes layout at most once per frame.
    /** @type {Array<[Element, number]>} */
    const writes = [];

    for (const [element, state] of progressTargets) {
        if (!state.visible) {
            continue;
        }

        const rect = element.getBoundingClientRect();
        let progress;

        if (state.mode === 'contain') {
            // Pinned sections: 0 when the top reaches the top of the viewport, 1 when the
            // bottom does. `distance` is how far the page scrolls while the child is stuck.
            const distance = rect.height - viewportHeight;
            progress = distance > 0 ? -rect.top / distance : 0;
        } else {
            // Elements passing through: 0 as the top edge enters from below, 1 as the
            // bottom edge exits above.
            const distance = rect.height + viewportHeight;
            progress = distance > 0 ? (viewportHeight - rect.top) / distance : 0;
        }

        progress = progress < 0 ? 0 : progress > 1 ? 1 : progress;

        // Sub-thousandth changes are invisible. Skipping them avoids pointless style
        // invalidation on the many frames where a slow scroll barely moves.
        if (Math.abs(progress - state.progress) < 0.0005) {
            continue;
        }

        state.progress = progress;
        writes.push([element, progress]);
    }

    // Phase two: write.
    for (const [element, progress] of writes) {
        element.style.setProperty('--progress', progress.toFixed(4));
    }

    updateDocumentScrollState();

    frame = visibleCount > 0 ? requestAnimationFrame(measure) : 0;
}

let lastScrolled = null;
let lastPageProgress = -1;

/**
 * Publishes two document-level signals used by the header and the reading-progress bar:
 * `data-scrolled` for the condensed header state, and `--page-progress` for the bar.
 * Both are CSS-only consumers, so neither costs a component render.
 */
function updateDocumentScrollState() {
    const root = document.documentElement;

    const scrolled = window.scrollY > 24;
    if (scrolled !== lastScrolled) {
        lastScrolled = scrolled;
        root.dataset.scrolled = String(scrolled);
    }

    const scrollable = root.scrollHeight - window.innerHeight;
    const pageProgress = scrollable > 0 ? Math.min(1, window.scrollY / scrollable) : 0;

    if (Math.abs(pageProgress - lastPageProgress) >= 0.001) {
        lastPageProgress = pageProgress;
        root.style.setProperty('--page-progress', pageProgress.toFixed(4));
    }
}

function start() {
    if (!frame) {
        frame = requestAnimationFrame(measure);
    }
}

// The header state has to be correct even on a page with no registered progress targets,
// so it is also updated from a passive scroll listener. Passive tells the browser it can
// scroll without waiting to find out whether this handler cancels the event.
window.addEventListener('scroll', updateDocumentScrollState, { passive: true });
window.addEventListener('resize', () => {
    // Every cached rect is stale after a resize. Forcing a recompute is cheaper than
    // caching sizes and trying to keep them valid.
    for (const state of progressTargets.values()) {
        state.progress = -1;
    }

    lastPageProgress = -1;
    start();
}, { passive: true });

updateDocumentScrollState();

/** Releases every observer and stops the loop. Called when the Blazor app shuts down. */
export function shutdown() {
    if (frame) {
        cancelAnimationFrame(frame);
        frame = 0;
    }

    revealObserver?.disconnect();
    visibilityObserver?.disconnect();
    revealObserver = null;
    visibilityObserver = null;

    revealTargets.clear();
    progressTargets.clear();
    visibleCount = 0;
}
