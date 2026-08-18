using GaWeCodes.Domain.Aggregates;
using GaWeCodes.Domain.Entities;
using GaWeCodes.Domain.Events;
using GaWeCodes.DependencyInjection.Validation;

namespace GaWeCodes.Tests;

public sealed class AggregateStateSelfBindingCheckTests
{
    [Fact]
    public async Task AStateNamingAnotherTypeAsItself_FailsTheStartWithTheReason()
    {
        var check = new AggregateStateSelfBindingCheck([typeof(AggregateStateSelfBindingCheckTests).Assembly]);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => check.RunAsync(TestContext.Current.CancellationToken));

        Assert.Contains(nameof(MisboundState), thrown.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(WellBoundState), thrown.Message, StringComparison.Ordinal);
        Assert.Contains("InvalidCastException", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AStateNamingItself_PassesTheStart()
    {
        var check = new AggregateStateSelfBindingCheck([typeof(AggregateState<,>).Assembly]);

        await check.RunAsync(TestContext.Current.CancellationToken);
    }

    private readonly record struct SelfBindingProbeId(Guid Value) : IEntityKey<Guid>
    {
        public bool IsEmpty => Value == Guid.Empty;
    }

    private sealed record WellBoundState(SelfBindingProbeId Id)
        : AggregateState<WellBoundState, SelfBindingProbeId>
    {
        public override WellBoundState Apply(IDomainEvent domainEvent) => this;
    }

    private sealed record MisboundState(SelfBindingProbeId Id)
        : AggregateState<WellBoundState, SelfBindingProbeId>
    {
        public override WellBoundState Apply(IDomainEvent domainEvent) => new(Id);
    }
}
