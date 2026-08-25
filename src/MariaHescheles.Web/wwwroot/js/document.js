/**
 * Document-level helpers that have no Blazor equivalent.
 *
 * @module document
 */

/** Live dialog bindings, so a dismissal can be reported exactly once. @type {WeakMap<Element, () => void>} */
const dialogCleanup = new WeakMap();

/** Live range bindings. @type {WeakMap<Element, () => void>} */
const rangeCleanup = new WeakMap();

/** Nested modal count. Locking is refcounted so closing an inner dialog does not unlock the page. */
let scrollLocks = 0;

// ---------------------------------------------------------------------------
// Structured data
// ---------------------------------------------------------------------------

/**
 * Publishes a JSON-LD document into <head>, replacing any previous one with the same id.
 *
 * Blazor's HeadOutlet cannot do this: it renders a <script> element as inert markup, so the
 * JSON never becomes a node a crawler will parse. Creating the element here does.
 *
 * @param {string} id
 * @param {string} json A complete JSON document.
 */
export function setStructuredData(id, json) {
    let element = document.getElementById(id);

    if (!element) {
        element = document.createElement('script');
        element.id = id;
        element.type = 'application/ld+json';
        document.head.appendChild(element);
    }

    // textContent, never innerHTML: the value is data, and it must never be parsed as markup.
    element.textContent = json;
}

// ---------------------------------------------------------------------------
// Scrolling
// ---------------------------------------------------------------------------

function prefersReducedMotion() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * Returns the page to the top, as a browser would on a real navigation.
 *
 * @param {boolean} smooth Requested smoothness, overridden by the reduced-motion preference.
 */
export function scrollToTop(smooth) {
    window.scrollTo({
        top: 0,
        left: 0,
        behavior: smooth && !prefersReducedMotion() ? 'smooth' : 'auto',
    });
}

/**
 * Scrolls an element into view, clearing the fixed header.
 *
 * @param {string} elementId
 */
export function scrollToAnchor(elementId) {
    const target = document.getElementById(elementId);
    if (!target) {
        return;
    }

    // Read the header height from the same custom property the header itself is sized with,
    // so the offset can never drift out of sync with the design.
    const headerHeight = Number.parseFloat(
        getComputedStyle(document.documentElement).getPropertyValue('--header-height'),
    ) || 0;

    const top = target.getBoundingClientRect().top + window.scrollY - headerHeight - 24;

    window.scrollTo({
        top,
        behavior: prefersReducedMotion() ? 'auto' : 'smooth',
    });
}

function lockScroll() {
    scrollLocks += 1;
    if (scrollLocks > 1) {
        return;
    }

    // Reserve the scrollbar's width as padding before hiding it, otherwise the whole page
    // jumps sideways by ~15px the instant a modal opens.
    const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
    document.documentElement.style.setProperty('--scrollbar-width', `${scrollbarWidth}px`);
    document.documentElement.classList.add('is-scroll-locked');
}

function unlockScroll() {
    scrollLocks = Math.max(0, scrollLocks - 1);
    if (scrollLocks === 0) {
        document.documentElement.classList.remove('is-scroll-locked');
        document.documentElement.style.removeProperty('--scrollbar-width');
    }
}

// ---------------------------------------------------------------------------
// Modal dialogs
// ---------------------------------------------------------------------------

/**
 * Opens a native <dialog> modally and reports its dismissal back to .NET.
 *
 * showModal() is used deliberately. The platform gives focus trapping, Escape to dismiss,
 * `inert` background content, correct top-layer stacking and focus restoration on close.
 * Reimplementing that on a <div> is a well-known way to ship an inaccessible modal.
 *
 * @param {HTMLDialogElement} dialog
 * @param {{ invokeMethodAsync: (name: string) => Promise<void> }} owner A DotNetObjectReference.
 */
export function openDialog(dialog, owner) {
    if (!dialog || dialog.open) {
        return;
    }

    const onClose = () => {
        dialog.removeEventListener('close', onClose);
        dialog.removeEventListener('click', onBackdropClick);
        dialogCleanup.delete(dialog);
        unlockScroll();
        owner.invokeMethodAsync('OnDialogClosed');
    };

    // A click that lands on the dialog element itself rather than on its contents is a
    // click on the backdrop, which conventionally dismisses.
    const onBackdropClick = (event) => {
        if (event.target === dialog) {
            dialog.close();
        }
    };

    dialog.addEventListener('close', onClose);
    dialog.addEventListener('click', onBackdropClick);
    dialogCleanup.set(dialog, onClose);

    lockScroll();
    dialog.showModal();
}

/** @param {HTMLDialogElement} dialog */
export function closeDialog(dialog) {
    if (dialog?.open) {
        dialog.close();
    }
}

// ---------------------------------------------------------------------------
// Range to custom property
// ---------------------------------------------------------------------------

/**
 * Mirrors a range input's value into a CSS custom property on another element.
 *
 * A drag fires input events at the display refresh rate. Routing those through Blazor would
 * mean a component render per frame to move one divider, so the value is written straight
 * into the style system instead.
 *
 * @param {HTMLInputElement} range
 * @param {HTMLElement} target
 * @param {string} property Custom property name, including the leading `--`.
 */
export function bindRangeToProperty(range, target, property) {
    if (!range || !target) {
        return;
    }

    const onInput = () => target.style.setProperty(property, range.value);

    range.addEventListener('input', onInput);
    rangeCleanup.set(range, () => range.removeEventListener('input', onInput));

    onInput();
}

/** @param {HTMLInputElement} range */
export function releaseRangeBinding(range) {
    const cleanup = rangeCleanup.get(range);
    if (cleanup) {
        cleanup();
        rangeCleanup.delete(range);
    }
}
