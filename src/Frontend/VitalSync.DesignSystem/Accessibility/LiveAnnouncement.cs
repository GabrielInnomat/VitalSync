using Microsoft.AspNetCore.Components;

namespace VitalSync.DesignSystem.Accessibility;

internal sealed record LiveAnnouncement(long Id, bool Urgent, RenderFragment Content);
