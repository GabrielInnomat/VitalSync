using GaWeCodes.Thessera.Testing;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Tests;

public sealed class AggregateConventionTests
{
    [Fact]
    public void EveryAggregateAndDomainEvent_FollowsTheStorageConventions() =>
        AggregateConventions.Verify([typeof(Widget).Assembly]);
}
