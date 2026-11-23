namespace VitalSync.DesignSystem.Patterns.Feedback;

internal sealed record AlertBannerMessage(
    string Key,
    long Sequence,
    long Order,
    AlertBannerVariant Variant,
    string Title,
    string Text,
    AlertBannerLink? Link,
    bool Closable);
