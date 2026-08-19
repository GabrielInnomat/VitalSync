using GaWeCodes.Thessera.Testing;
using VitalSync.Sample.EventSourced.Domain;

namespace VitalSync.Sample.EventSourced.Tests;

public sealed class AggregateConventionTests
{
    [Fact]
    public void EveryAggregateAndDomainEvent_FollowsTheStorageConventions() =>
        AggregateConventions.Verify([typeof(Gadget).Assembly]);
}
