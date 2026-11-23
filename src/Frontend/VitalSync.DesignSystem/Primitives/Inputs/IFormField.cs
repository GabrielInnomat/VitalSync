using Microsoft.AspNetCore.Components.Forms;

namespace VitalSync.DesignSystem.Primitives.Inputs;

internal interface IFormField
{
    FieldIdentifier FieldIdentifier { get; }

    string Id { get; }

    string Label { get; }

    bool Required { get; }

    bool Disabled { get; }

    ValueTask FocusAsync();
}
