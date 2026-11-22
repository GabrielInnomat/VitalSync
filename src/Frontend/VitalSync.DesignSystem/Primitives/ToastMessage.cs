namespace VitalSync.DesignSystem.Primitives;

internal sealed record ToastMessage(long Id, ToastVariant Variant, string Title, string Text);
