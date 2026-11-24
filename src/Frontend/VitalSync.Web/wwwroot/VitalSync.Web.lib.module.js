import { startTheme } from './theme.js';

let lastPage = pageOf(location.href);
let stopFollowing = () => {};

function pageOf(href) {
    const url = new URL(href);
    return `${url.origin}${url.pathname}`;
}

function focusOn(heading) {
    if (!heading.hasAttribute('tabindex')) {
        heading.tabIndex = -1;
    }

    heading.focus({ preventScroll: true });
}

function focusWasLost() {
    return !document.activeElement || document.activeElement === document.body;
}

function follow(heading) {
    let current = heading;

    const observer = new MutationObserver(() => {
        if (current.isConnected || !focusWasLost()) {
            return;
        }

        const next = document.querySelector('h1');

        if (next) {
            current = next;
            focusOn(next);
        }
    });

    const stop = () => {
        observer.disconnect();
        document.removeEventListener('keydown', stop, true);
        document.removeEventListener('pointerdown', stop, true);
    };

    observer.observe(document.body, { childList: true, subtree: true });
    document.addEventListener('keydown', stop, true);
    document.addEventListener('pointerdown', stop, true);

    return stop;
}

function focusHeading() {
    const heading = document.querySelector('h1');

    if (!heading) {
        return;
    }

    focusOn(heading);
    stopFollowing = follow(heading);
}

function onEnhancedLoad() {
    const page = pageOf(location.href);

    if (page === lastPage) {
        return;
    }

    lastPage = page;
    stopFollowing();
    focusHeading();
}

export function afterWebStarted(blazor) {
    blazor.addEventListener('enhancedload', onEnhancedLoad);
    startTheme(blazor);
}
