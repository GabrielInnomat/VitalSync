using Microsoft.AspNetCore.Components;

namespace VitalSync.DesignSystem.Primitives;

internal sealed record LiveAnnouncement(long Id, bool Urgent, RenderFragment Content);
