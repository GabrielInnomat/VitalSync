const focusableSelector = [
    'a[href]',
    'button:not([disabled])',
    'input:not([disabled]):not([type="hidden"])',
    'select:not([disabled])',
    'textarea:not([disabled])',
    'summary',
    '[contenteditable="true"]',
    '[tabindex]:not([tabindex="-1"])',
].join(',');

const fieldSelector = 'input:not([type="hidden"]), select, textarea';

const states = new WeakMap();
let lockCount = 0;
let previousOverflow = '';

function isShown(element) {
    return element.checkVisibility ? element.checkVisibility() : element.getClientRects().length > 0;
}

function focusablesIn(container) {
    if (!container) {
        return [];
    }

    const own = container.matches(focusableSelector) ? [container] : [];
    return own.concat([...container.querySelectorAll(focusableSelector)]).filter(isShown);
}

function updateScrollAccess(dialog, body) {
    const scrolls = body.scrollHeight > body.clientHeight;
    const holdsFocusable = [...body.querySelectorAll(focusableSelector)].some(isShown);

    if (scrolls && !holdsFocusable) {
        body.setAttribute('tabindex', '0');
        body.setAttribute('role', 'region');
        body.setAttribute('aria-labelledby', dialog.getAttribute('aria-labelledby'));
    } else {
        body.removeAttribute('tabindex');
        body.removeAttribute('role');
        body.removeAttribute('aria-labelledby');
    }
}

function lockScroll() {
    if (lockCount === 0) {
        previousOverflow = document.documentElement.style.overflow;
        document.documentElement.style.overflow = 'hidden';
    }

    lockCount += 1;
}

function unlockScroll() {
    lockCount = Math.max(0, lockCount - 1);

    if (lockCount === 0) {
        document.documentElement.style.overflow = previousOverflow;
    }
}

function release(dialog) {
    const state = states.get(dialog);

    if (!state) {
        return null;
    }

    states.delete(dialog);
    dialog.removeEventListener('keydown', state.onKeyDown);
    dialog.removeEventListener('cancel', state.onCancel);
    dialog.removeEventListener('close', state.onClose);
    state.observer.disconnect();
    unlockScroll();
    return state;
}

function commitPendingEdit(dialog) {
    const active = document.activeElement;

    if (active && dialog.contains(active) && active.matches('input, textarea')) {
        active.blur();
        active.focus();
    }
}

export function open(dialog, dotNet) {
    if (states.has(dialog)) {
        return;
    }

    const opener = document.activeElement;
    const body = dialog.querySelector('.vs-dialog__body');
    const footer = dialog.querySelector('.vs-dialog__footer');

    const onKeyDown = event => {
        if (event.key !== 'Escape' || event.defaultPrevented || dialog.querySelector(':popover-open')) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        if (!event.repeat) {
            commitPendingEdit(dialog);
            dotNet.invokeMethodAsync('RequestClose');
        }
    };

    const onCancel = event => {
        event.preventDefault();
        commitPendingEdit(dialog);
        dotNet.invokeMethodAsync('RequestClose');
    };

    const onClose = () => {
        if (release(dialog)) {
            dotNet.invokeMethodAsync('ClosedByBrowser');
        }
    };

    const observer = new ResizeObserver(() => {
        if (body) {
            updateScrollAccess(dialog, body);
        }
    });

    dialog.addEventListener('keydown', onKeyDown);
    dialog.addEventListener('cancel', onCancel);
    dialog.addEventListener('close', onClose);
    states.set(dialog, { opener, onKeyDown, onCancel, onClose, observer });

    if (!dialog.open) {
        dialog.showModal();
    }

    lockScroll();

    if (body) {
        updateScrollAccess(dialog, body);
        observer.observe(body);

        for (const child of body.children) {
            observer.observe(child);
        }
    }

    const start = dialog.querySelector('[data-vs-dialog-start]')
        ?? (body && !body.querySelector(fieldSelector) ? dialog.querySelector('.vs-dialog__title') : null)
        ?? focusablesIn(body)[0]
        ?? focusablesIn(footer)[0]
        ?? dialog.querySelector('.vs-dialog__close');

    start?.focus();
}

export function close(dialog, target) {
    const state = release(dialog);

    if (dialog.open) {
        dialog.close();
    }

    if (target) {
        target.focus();
        return;
    }

    const opener = state?.opener;
    const active = document.activeElement;
    const focusLost = !active || active === document.body || dialog.contains(active);

    if (opener && opener.isConnected && focusLost) {
        opener.focus();
    }
}

export function dispose(dialog) {
    release(dialog);
}
