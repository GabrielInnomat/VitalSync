using System.Collections.Concurrent;
using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Dispatching;

/// <summary>
/// Records the explicit execution order assigned to each registered pipeline behavior.
/// </summary>
/// <remarks>
/// The DI container has no notion of order for an enumerable of open-generic
/// <see cref="IPipelineBehavior{TRequest, TResponse}"/>s, so <see cref="Sender"/> cannot rely on registration order to
/// decide the wrapping sequence. This registry maps each behavior's open-generic type definition to a numeric order and
/// lets the sender sort the resolved behaviors deterministically: lower orders wrap further out (execute earlier),
/// higher orders sit closer to the handler. Registered as a singleton by
/// <see cref="DependencyInjection.ServiceCollectionExtensions.AddBuildingBlocks"/> and populated through
/// <see cref="DependencyInjection.BuildingBlocksOptions.AddPipelineBehavior"/>.
/// </remarks>
internal sealed class PipelineBehaviorRegistry
{
    private readonly ConcurrentDictionary<Type, int> _orders = new();

    /// <summary>
    /// Assigns an execution order to a behavior's open-generic type definition.
    /// </summary>
    /// <param name="openGenericBehavior">The open-generic behavior type definition (for example <c>typeof(LoggingBehavior&lt;,&gt;)</c>).</param>
    /// <param name="order">The execution order; lower values wrap further out and execute earlier.</param>
    public void Register(Type openGenericBehavior, int order) => _orders[openGenericBehavior] = order;

    /// <summary>
    /// Gets the execution order registered for a resolved behavior instance's type.
    /// </summary>
    /// <param name="closedBehaviorType">The closed-generic runtime type of a resolved behavior.</param>
    /// <returns>The registered order, or <c>0</c> when the behavior's type definition was not registered.</returns>
    public int GetOrder(Type closedBehaviorType)
    {
        var definition = closedBehaviorType.IsGenericType
            ? closedBehaviorType.GetGenericTypeDefinition()
            : closedBehaviorType;

        return _orders.TryGetValue(definition, out var order) ? order : 0;
    }
}
