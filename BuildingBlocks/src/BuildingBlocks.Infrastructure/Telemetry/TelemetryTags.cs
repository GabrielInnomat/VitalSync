namespace BuildingBlocks.Infrastructure.Telemetry;

internal static class TelemetryTags
{
    public const string RequestName = "buildingblocks.request.name";

    public const string RequestKind = "buildingblocks.request.kind";

    public const string RequestKindCommand = "command";

    public const string RequestKindQuery = "query";

    public const string Outcome = "buildingblocks.outcome";

    public const string OutcomeSuccess = "success";

    public const string OutcomeFailure = "failure";

    public const string OutcomeFaulted = "faulted";

    public const string FailureCategories = "buildingblocks.failure.categories";

    public const string ExceptionType = "buildingblocks.exception.type";

    public const string DomainEventName = "buildingblocks.domain_event.name";

    public const string AggregateName = "buildingblocks.aggregate.name";

    public const string AggregateId = "buildingblocks.aggregate.id";

    public const string AggregateVersion = "buildingblocks.aggregate.version";

    public const string ProjectionHandler = "buildingblocks.projection.handler";

    public const string IntegrationEventsPublished = "buildingblocks.integration_events.published";
}
