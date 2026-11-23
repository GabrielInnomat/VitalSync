const states = new WeakMap();

function tabsIn(list) {
    return [...list.querySelectorAll('[role="tab"]')];
}

function updateLayout(list) {
    list.setAttribute('data-vs-tabs-layout', 'row');

    if (list.scrollWidth > list.clientWidth) {
        list.setAttribute('data-vs-tabs-layout', 'stacked');
        list.setAttribute('aria-orientation', 'vertical');
    } else {
        list.removeAttribute('aria-orientation');
    }
}

function targetTab(list, tab, key) {
    const tabs = tabsIn(list);
    const index = tabs.indexOf(tab);
    const last = tabs.length - 1;
    const stacked = list.getAttribute('aria-orientation') === 'vertical';
    const rtl = getComputedStyle(list).direction === 'rtl';
    const next = () => tabs[index === last ? 0 : index + 1];
    const previous = () => tabs[index === 0 ? last : index - 1];

    switch (key) {
        case 'ArrowRight':
            return rtl ? previous() : next();
        case 'ArrowLeft':
            return rtl ? next() : previous();
        case 'ArrowDown':
            return stacked ? next() : null;
        case 'ArrowUp':
            return stacked ? previous() : null;
        case 'Home':
            return tabs[0];
        case 'End':
            return tabs[last];
        default:
            return null;
    }
}

export function attach(list, dotNet) {
    const activate = tab => {
        dotNet.invokeMethodAsync('Select', tab.dataset.vsTabKey).catch(() => location.assign(tab.href));
    };

    const onClick = event => {
        if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
            return;
        }

        const tab = event.target.closest('[role="tab"]');

        if (!tab || !list.contains(tab)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        activate(tab);
    };

    const onKeyDown = event => {
        if (event.ctrlKey || event.metaKey || event.altKey) {
            return;
        }

        const tab = event.target.closest('[role="tab"]');

        if (!tab || !list.contains(tab)) {
            return;
        }

        if (event.key === ' ') {
            event.preventDefault();
            activate(tab);
            return;
        }

        const target = targetTab(list, tab, event.key);

        if (!target) {
            return;
        }

        event.preventDefault();
        target.focus();
        activate(target);
    };

    let frame = 0;
    const observer = new ResizeObserver(() => {
        if (frame) {
            return;
        }

        frame = requestAnimationFrame(() => {
            frame = 0;
            updateLayout(list);
        });
    });

    observer.observe(list);
    tabsIn(list).forEach(tab => observer.observe(tab));
    list.addEventListener('click', onClick, true);
    list.addEventListener('keydown', onKeyDown);
    updateLayout(list);

    states.set(list, { onClick, onKeyDown, observer, cancel: () => cancelAnimationFrame(frame) });
}

export function replaceUrl(name, key) {
    const url = new URL(location.href);
    url.searchParams.set(name, key);
    history.replaceState(history.state, '', url);
}

export function detach(list) {
    const state = states.get(list);

    if (!state) {
        return;
    }

    state.observer.disconnect();
    state.cancel();
    list.removeEventListener('click', state.onClick, true);
    list.removeEventListener('keydown', state.onKeyDown);
    states.delete(list);
}
