const storageKey = 'vitalsync-theme';
const systemDark = window.matchMedia('(prefers-color-scheme: dark)');

function store(choice) {
    try {
        if (choice === 'system') {
            localStorage.removeItem(storageKey);
        } else {
            localStorage.setItem(storageKey, choice);
        }
    } catch {
        return;
    }
}

function apply(choice) {
    document.documentElement.setAttribute('data-theme', choice === 'system' ? systemTheme() : choice);
}

function refresh() {
    apply(storedChoice());
}

function isStorageChange(event) {
    return event.key === storageKey || event.key === null;
}

export function storedChoice() {
    try {
        const choice = localStorage.getItem(storageKey);
        return choice === 'light' || choice === 'dark' ? choice : 'system';
    } catch {
        return 'system';
    }
}

export function systemTheme() {
    return systemDark.matches ? 'dark' : 'light';
}

export function chooseTheme(choice) {
    if (choice !== 'light' && choice !== 'dark' && choice !== 'system') {
        return;
    }

    store(choice);
    apply(choice);
}

export function watchTheme(listener) {
    const notify = () => listener.invokeMethodAsync('OnThemeChanged', storedChoice(), systemTheme());
    const onStorage = event => {
        if (isStorageChange(event)) {
            notify();
        }
    };

    systemDark.addEventListener('change', notify);
    window.addEventListener('storage', onStorage);

    return {
        stop() {
            systemDark.removeEventListener('change', notify);
            window.removeEventListener('storage', onStorage);
        }
    };
}

export function startTheme(blazor) {
    systemDark.addEventListener('change', refresh);
    blazor.addEventListener('enhancedload', refresh);
    window.addEventListener('storage', event => {
        if (isStorageChange(event)) {
            refresh();
        }
    });
}
