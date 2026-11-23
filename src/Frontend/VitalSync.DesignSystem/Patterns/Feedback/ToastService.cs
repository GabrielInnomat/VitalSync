using VitalSync.DesignSystem.Accessibility;

namespace VitalSync.DesignSystem.Patterns.Feedback;

public sealed class ToastService(LiveAnnouncer announcer)
{
    internal const int Limit = 3;

    private readonly Lock _gate = new();
    private readonly List<ToastMessage> _toasts = [];
    private long _lastId;
    private long? _focusedId;

    internal event Action? Changed;

    internal IReadOnlyList<ToastMessage> Toasts
    {
        get
        {
            lock (_gate)
            {
                return [.. _toasts];
            }
        }
    }

    public void ShowSuccess(string title, string text) => Show(ToastVariant.Success, title, text);

    public void ShowInfo(string title, string text) => Show(ToastVariant.Info, title, text);

    public void ShowCritical(string title, string text) => Show(ToastVariant.Critical, title, text);

    internal void Close(long id)
    {
        lock (_gate)
        {
            if (_toasts.RemoveAll(toast => toast.Id == id) == 0)
            {
                return;
            }

            if (_focusedId == id)
            {
                _focusedId = null;
            }
        }

        Changed?.Invoke();
    }

    internal void SetFocused(long? id)
    {
        lock (_gate)
        {
            _focusedId = id;
        }
    }

    private void Show(ToastVariant variant, string title, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var trimmedTitle = title.Trim();
        var trimmedText = text.Trim();

        lock (_gate)
        {
            _lastId++;
            _toasts.Add(new ToastMessage(_lastId, variant, trimmedTitle, trimmedText));

            while (_toasts.Count > Limit)
            {
                _toasts.Remove(_toasts.First(toast => toast.Id != _focusedId));
            }
        }

        Changed?.Invoke();
        announcer.AnnounceMessage(
            $"{Toast.KindWord(variant)} – {trimmedTitle}",
            trimmedText,
            urgent: variant is ToastVariant.Critical);
    }
}
