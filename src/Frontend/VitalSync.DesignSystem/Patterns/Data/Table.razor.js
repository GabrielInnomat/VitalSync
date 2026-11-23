const states = new WeakMap();

export function attach(scroller, dotNet) {
    const outer = scroller.parentElement;
    let scrollable = null;
    let pending = 0;

    const measure = () => {
        pending = 0;

        const overflow = scroller.scrollWidth - scroller.clientWidth;
        const offset = Math.abs(scroller.scrollLeft);
        const now = overflow > 1;

        outer.toggleAttribute('data-vs-table-start', now && offset > 1);
        outer.toggleAttribute('data-vs-table-end', now && offset < overflow - 1);

        if (now !== scrollable) {
            scrollable = now;
            dotNet.invokeMethodAsync('SetScrollable', now).catch(() => {});
        }
    };

    const schedule = () => {
        if (!pending) {
            pending = requestAnimationFrame(measure);
        }
    };

    const observer = new ResizeObserver(schedule);
    observer.observe(scroller);
    scroller.querySelectorAll('table').forEach(table => observer.observe(table));
    scroller.addEventListener('scroll', schedule, { passive: true });
    measure();

    states.set(scroller, { observer, schedule, cancel: () => cancelAnimationFrame(pending) });
}

export function detach(scroller) {
    const state = states.get(scroller);

    if (!state) {
        return;
    }

    state.observer.disconnect();
    state.cancel();
    scroller.removeEventListener('scroll', state.schedule);
    states.delete(scroller);
}
