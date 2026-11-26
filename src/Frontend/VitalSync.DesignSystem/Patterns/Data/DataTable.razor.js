const states = new WeakMap();

export function attach(root, dotNet) {
    const onClick = event => {
        if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
            return;
        }

        const link = event.target.closest('a[data-vs-data-table-action]');

        if (!link || link.closest('.vs-data-table') !== root) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        const call = link.dataset.vsDataTableAction === 'page'
            ? dotNet.invokeMethodAsync('GoToPage', Number(link.dataset.vsDataTablePage))
            : dotNet.invokeMethodAsync('ResetFilters');

        call.catch(() => location.assign(link.href));
    };

    root.addEventListener('click', onClick, true);
    states.set(root, { onClick });
}

export function replaceUrl(prefix, entries) {
    const url = new URL(location.href);

    for (const key of [...url.searchParams.keys()]) {
        if (key.startsWith(`${prefix}.`)) {
            url.searchParams.delete(key);
        }
    }

    for (const [key, value] of Object.entries(entries)) {
        url.searchParams.set(key, value);
    }

    history.replaceState(history.state, '', url);
}

export function detach(root) {
    const state = states.get(root);

    if (!state) {
        return;
    }

    root.removeEventListener('click', state.onClick, true);
    states.delete(root);
}
