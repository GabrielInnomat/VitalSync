namespace VitalSync.DesignSystem.Primitives;

public sealed class AlertBannerService(LiveAnnouncer announcer)
{
    private readonly Lock _gate = new();
    private readonly List<AlertBannerMessage> _banners = [];
    private readonly HashSet<string> _closed = new(StringComparer.Ordinal);
    private long _lastSequence;

    internal event Action? Changed;

    internal IReadOnlyList<AlertBannerMessage> Banners
    {
        get
        {
            lock (_gate)
            {
                return [.. _banners.OrderBy(banner => Severity(banner.Variant)).ThenBy(banner => banner.Order)];
            }
        }
    }

    public void ShowInfo(string key, string title, string text, AlertBannerLink? link = null, bool closable = false) =>
        Show(key, AlertBannerVariant.Info, title, text, link, closable);

    public void ShowWarning(string key, string title, string text, AlertBannerLink? link = null, bool closable = false) =>
        Show(key, AlertBannerVariant.Warning, title, text, link, closable);

    public void ShowCritical(string key, string title, string text, AlertBannerLink? link = null, bool closable = false) =>
        Show(key, AlertBannerVariant.Critical, title, text, link, closable);

    public void Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_gate)
        {
            if (_banners.RemoveAll(banner => banner.Key == key) == 0)
            {
                return;
            }
        }

        Changed?.Invoke();
    }

    internal void Close(string key)
    {
        lock (_gate)
        {
            if (_banners.RemoveAll(banner => banner.Key == key) == 0)
            {
                return;
            }

            _closed.Add(key);
        }

        Changed?.Invoke();
    }

    private static int Severity(AlertBannerVariant variant) => variant switch
    {
        AlertBannerVariant.Critical => 0,
        AlertBannerVariant.Warning => 1,
        AlertBannerVariant.Info => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    private void Show(
        string key,
        AlertBannerVariant variant,
        string title,
        string text,
        AlertBannerLink? link,
        bool closable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var trimmedTitle = title.Trim();
        var trimmedText = text.Trim();

        lock (_gate)
        {
            if (_closed.Contains(key))
            {
                return;
            }

            var index = _banners.FindIndex(banner => banner.Key == key);

            if (index < 0)
            {
                _lastSequence++;
                _banners.Add(new AlertBannerMessage(
                    key, _lastSequence, _lastSequence, variant, trimmedTitle, trimmedText, link, closable));
            }
            else
            {
                var existing = _banners[index];

                if (existing.Variant == variant
                    && existing.Title == trimmedTitle
                    && existing.Text == trimmedText
                    && existing.Link == link
                    && existing.Closable == closable)
                {
                    return;
                }

                _lastSequence++;
                _banners[index] = existing with
                {
                    Sequence = _lastSequence,
                    Variant = variant,
                    Title = trimmedTitle,
                    Text = trimmedText,
                    Link = link,
                    Closable = closable,
                };
            }
        }

        Changed?.Invoke();
        announcer.AnnounceMessage(
            $"{AlertBanner.KindWord(variant)} – {trimmedTitle}",
            trimmedText,
            urgent: variant is not AlertBannerVariant.Info);
    }
}
