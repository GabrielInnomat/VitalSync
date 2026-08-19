using GaWeCodes.Domain.Naming;

namespace GaWeCodes.Core.Telemetry;

internal static class TelemetryTags
{
    public const string RequestName = "gawecodes.request.name";

    public const string RequestKind = "gawecodes.request.kind";

    public const string RequestKindCommand = "command";

    public const string RequestKindQuery = "query";

    public const string Outcome = "gawecodes.outcome";

    public const string OutcomeSuccess = "success";

    public const string OutcomeFailure = "failure";

    public const string OutcomeFaulted = "faulted";

    public const string FailureCategories = "gawecodes.failure.categories";

    public const string ExceptionType = "gawecodes.exception.type";

    public const string DomainEventName = "gawecodes.domain_event.name";

    public const string AggregateName = "gawecodes.aggregate.name";

    public const string AggregateId = "gawecodes.aggregate.id";

    public const string AggregateVersion = "gawecodes.aggregate.version";

    public const string ProjectionHandler = "gawecodes.projection.handler";

    public const string IntegrationEventsPublished = "gawecodes.integration_events.published";
}
