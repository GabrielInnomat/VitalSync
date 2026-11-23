using Microsoft.AspNetCore.Components;

namespace VitalSync.DesignSystem.Patterns.Navigation;

internal sealed class TabsRegistration : ComponentBase
{
    [CascadingParameter]
    private Tabs? Parent { get; set; }

    protected override void OnInitialized() => Parent?.Complete();

    protected override bool ShouldRender() => false;
}
