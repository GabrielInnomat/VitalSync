using BuildingBlocks.Domain.Aggregates;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class AggregateStateModelCheck<TContext>(IServiceProvider serviceProvider) : IStartupCheck
    where TContext : DbContext
{
    public StartupPhase Phase => StartupPhase.BeforeHostedServicesStart;

    public void Run()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        var offenders = new List<string>();
        var keyless = new List<string>();
        var visited = new HashSet<IEntityType>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            if (!entityType.IsOwned() && IsAggregateState(entityType.ClrType))
            {
                Validate(entityType, offenders, keyless, visited);
            }
        }

        if (offenders.Count > 0)
        {
            throw new InvalidOperationException(
                "Aggregate state mapping validation failed at startup. A child of an aggregate lives and dies " +
                "with that aggregate, so it maps as an owned type (OwnsOne/OwnsMany, optionally ToJson). EF Core " +
                "loads owned children with their owner and reconciles them against their key when the state is " +
                "replaced; a navigation to an independent entity type is loaded by neither and is silently lost " +
                $"on commit (ADR-0031): {string.Join("; ", offenders)}.");
        }

        if (keyless.Count > 0)
        {
            throw new InvalidOperationException(
                "Aggregate state mapping validation failed at startup. A child of an aggregate has its own "
                + "identity, so an owned collection declares that identity as its key (HasKey) with a single, "
                + "non-shadow property. Without it the commit cannot match a replaced child against the tracked "
                + $"one and would rewrite rows instead of updating them (ADR-0031): {string.Join("; ", keyless)}.");
        }
    }

    private static void Validate(
        IEntityType entityType,
        List<string> offenders,
        List<string> keyless,
        HashSet<IEntityType> visited)
    {
        if (!visited.Add(entityType))
        {
            return;
        }

        foreach (var navigation in entityType.GetNavigations())
        {
            if (navigation.ForeignKey.IsOwnership)
            {
                if (!navigation.IsOnDependent)
                {
                    if (navigation.IsCollection
                        && !navigation.TargetEntityType.IsMappedToJson()
                        && !AggregateStateGraph.IsReconcilableByKey(navigation.TargetEntityType))
                    {
                        keyless.Add($"'{entityType.ClrType.Name}.{navigation.Name}'");
                    }

                    Validate(navigation.TargetEntityType, offenders, keyless, visited);
                }

                continue;
            }

            offenders.Add(Describe(entityType, navigation.Name, navigation.TargetEntityType));
        }

        foreach (var navigation in entityType.GetSkipNavigations())
        {
            offenders.Add(Describe(entityType, navigation.Name, navigation.TargetEntityType));
        }
    }

    private static string Describe(IEntityType declaringType, string navigationName, IEntityType targetType) =>
        $"'{declaringType.ClrType.Name}.{navigationName}' navigates to the independent entity type " +
        $"'{targetType.ClrType.Name}'";

    private static bool IsAggregateState(Type clrType)
    {
        for (var current = clrType.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateState<,>))
            {
                return true;
            }
        }

        return false;
    }
}
