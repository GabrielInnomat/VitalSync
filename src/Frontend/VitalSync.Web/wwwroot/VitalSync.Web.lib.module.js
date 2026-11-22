let lastPage = pageOf(location.href);

function pageOf(href) {
    const url = new URL(href);
    return `${url.origin}${url.pathname}`;
}

function focusHeading() {
    const heading = document.querySelector('h1');

    if (!heading) {
        return;
    }

    if (!heading.hasAttribute('tabindex')) {
        heading.tabIndex = -1;
    }

    heading.focus({ preventScroll: true });
}

function onEnhancedLoad() {
    const page = pageOf(location.href);

    if (page === lastPage) {
        return;
    }

    lastPage = page;
    focusHeading();
}

export function afterWebStarted(blazor) {
    blazor.addEventListener('enhancedload', onEnhancedLoad);
}
