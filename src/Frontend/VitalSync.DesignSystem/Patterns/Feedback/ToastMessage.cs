namespace VitalSync.DesignSystem.Patterns.Feedback;

internal sealed record ToastMessage(long Id, ToastVariant Variant, string Title, string Text);
