using Microsoft.AspNetCore.Components;

namespace VitalSync.DesignSystem.Primitives;

public sealed class LiveAnnouncer
{
    private readonly Lock _gate = new();
    private readonly List<LiveAnnouncement> _announcements = [];
    private long _lastId;

    internal event Action? Changed;

    internal TimeSpan ClearDelay { get; set; } = TimeSpan.FromSeconds(5);

    internal IReadOnlyList<LiveAnnouncement> Announcements
    {
        get
        {
            lock (_gate)
            {
                return [.. _announcements];
            }
        }
    }

    public void Announce(string text) => Add(Plain(text), urgent: false);

    public void AnnounceUrgent(string text) => Add(Plain(text), urgent: true);

    internal void Announce(RenderFragment content, bool urgent)
    {
        ArgumentNullException.ThrowIfNull(content);
        Add(content, urgent);
    }

    internal void AnnounceMessage(string heading, string text, bool urgent) => Add(
        builder =>
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, heading);
            builder.CloseElement();
            builder.OpenElement(2, "p");
            builder.AddContent(3, text);
            builder.CloseElement();
        },
        urgent);

    private static RenderFragment Plain(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var trimmed = text.Trim();
        return builder => builder.AddContent(0, trimmed);
    }

    private void Add(RenderFragment content, bool urgent)
    {
        long id;

        lock (_gate)
        {
            _lastId++;
            id = _lastId;
            _announcements.Add(new LiveAnnouncement(id, urgent, content));
        }

        Changed?.Invoke();
        _ = RemoveLaterAsync(id, ClearDelay);
    }

    private async Task RemoveLaterAsync(long id, TimeSpan delay)
    {
        await Task.Delay(delay).ConfigureAwait(false);

        lock (_gate)
        {
            if (_announcements.RemoveAll(announcement => announcement.Id == id) == 0)
            {
                return;
            }
        }

        Changed?.Invoke();
    }
}
