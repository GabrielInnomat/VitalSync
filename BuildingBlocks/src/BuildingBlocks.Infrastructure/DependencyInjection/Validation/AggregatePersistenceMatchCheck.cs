using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class AggregatePersistenceMatchCheck(
    PersistenceSelection persistence,
    IServiceCollection services) : SynchronousStartupCheck
{
    public override StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

    protected override void Run()
    {
        if (persistence.Choice.Adapter is not { } adapter)
        {
            return;
        }

        var style = adapter.AggregateStyle;
        var mismatched = RequestedAggregates()
            .Where(aggregate => StyleOf(aggregate) != style)
            .Select(aggregate => $"'{aggregate}'")
            .ToList();

        if (mismatched.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            style == AggregateStyle.EventSourced
                ? $"{adapter.Description} stores an aggregate as the stream of events that produced it, but these " +
                    "aggregates keep no history because they derive from AggregateRoot instead of " +
                    "EventSourcedAggregateRoot. Their repository cannot even be constructed, and that failure " +
                    "surfaces on the first command that asks for one rather than here: " +
                    $"{Join(mismatched)}. Derive them from EventSourcedAggregateRoot, or select " +
                    "UseEfCorePersistence<TContext>(writeConnectionString) for this host."
                : $"{adapter.Description} stores the current state of an aggregate, but these aggregates derive " +
                    "from EventSourcedAggregateRoot and therefore treat their events as the record of truth. " +
                    "Their events would be published and then forgotten, no stream would ever exist, the " +
                    "aggregate could never be rehydrated, and nothing at run time would say so: " +
                    $"{Join(mismatched)}. Derive them from AggregateRoot, or select " +
                    "UseMartenEventSourcing(writeConnectionString) for this host.");
    }

    private static string Join(List<string> aggregates) =>
        string.Join(", ", aggregates.Take(5))
        + (aggregates.Count > 5 ? $" and {aggregates.Count - 5} more" : string.Empty);

    private IEnumerable<Type> RequestedAggregates() =>
        services
            .Select(static descriptor => descriptor.ImplementationType)
            .Where(static type => type is { IsAbstract: false } && !type.IsGenericTypeDefinition)
            .SelectMany(static type => type!.GetConstructors())
            .SelectMany(static constructor => constructor.GetParameters())
            .Select(static parameter => parameter.ParameterType)
            .Where(static type => type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(IRepository<,>))
            .Select(static type => type.GenericTypeArguments[0])
            .Distinct();

    private static AggregateStyle StyleOf(Type aggregate) =>
        Array.Exists(
            aggregate.GetInterfaces(),
            static @interface => @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IEventSourcedAggregateRoot<>))
            ? AggregateStyle.EventSourced
            : AggregateStyle.StateStored;
}
