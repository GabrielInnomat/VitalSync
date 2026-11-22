let attached = false;

function isShown(element) {
    return element.getClientRects().length > 0;
}

function collapse(toggle) {
    toggle.setAttribute('aria-expanded', 'false');
}

function onClick(event) {
    const toggle = event.target.closest('[data-vs-app-navigation-toggle]');

    if (toggle) {
        toggle.setAttribute('aria-expanded', toggle.getAttribute('aria-expanded') === 'true' ? 'false' : 'true');
        return;
    }

    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
        return;
    }

    const link = event.target.closest('[data-vs-app-navigation] a[href]');

    if (!link) {
        return;
    }

    const navigation = link.closest('[data-vs-app-navigation]');
    const navigationToggle = navigation.querySelector('[data-vs-app-navigation-toggle]');

    if (navigationToggle) {
        collapse(navigationToggle);
    }
}

function onKeyDown(event) {
    if (event.key !== 'Escape') {
        return;
    }

    const navigation = event.target.closest('[data-vs-app-navigation]');
    const toggle = navigation?.querySelector('[data-vs-app-navigation-toggle]');

    if (!toggle || toggle.getAttribute('aria-expanded') !== 'true' || !isShown(toggle)) {
        return;
    }

    event.preventDefault();
    collapse(toggle);
    toggle.focus();
}

export function attach() {
    if (attached) {
        return;
    }

    attached = true;
    document.addEventListener('click', onClick);
    document.addEventListener('keydown', onKeyDown);
}
