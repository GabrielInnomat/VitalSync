using System.Collections.Concurrent;
using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Dispatching;

internal sealed class PipelineBehaviorRegistry
{
    private readonly ConcurrentDictionary<Type, int> _orders = new();

    public void Register(Type openGenericBehavior, int order) => _orders[openGenericBehavior] = order;

    public int GetOrder(Type closedBehaviorType)
    {
        var definition = closedBehaviorType.IsGenericType
            ? closedBehaviorType.GetGenericTypeDefinition()
            : closedBehaviorType;

        return _orders.TryGetValue(definition, out var order) ? order : 0;
    }
}
