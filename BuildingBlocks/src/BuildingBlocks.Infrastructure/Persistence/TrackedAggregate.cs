using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// An aggregate registered with the <see cref="MartenAggregateTracker"/>, together with what the unit of work needs to append its events.
/// </summary>
/// <remarks>
/// The stream key and expected version are captured as accessors rather than values because both may only be final at
/// commit time: a newly added aggregate may still raise events (advancing its version) after it was tracked, and its
/// identity — from which the stream key derives — is established by its first event.
/// </remarks>
/// <param name="Aggregate">The tracked aggregate, exposing its uncommitted domain events.</param>
/// <param name="StreamKey">The accessor yielding the key of the event stream the aggregate's events belong to.</param>
/// <param name="ExpectedVersion">The accessor yielding the expected stream version after the uncommitted events are appended.</param>
public sealed record TrackedAggregate(
    IDomainEventsManager Aggregate,
    Func<string> StreamKey,
    Func<long> ExpectedVersion);
