using System.Reflection;
using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// Hosted service that verifies at host startup that every command and query has a resolvable handler.
/// </summary>
/// <remarks>
/// Handler registration via <see cref="BuildingBlocksOptions.AddHandlersFrom"/> is convention-based, so a misspelled
/// handler interface, a handler in an unscanned assembly, or a forgotten scan produces no error until the first
/// request dispatches the affected type. This check turns that whole failure class into a fail-fast startup error: it
/// walks every <see cref="ICommand"/>, <see cref="ICommand{TResult}"/>, and <see cref="IQuery{TResult}"/>
/// implementation in the scanned assemblies and resolves its handler contract from the container, failing the host
/// with all unresolvable request types named. It also rejects request types that implement more than one
/// result-bearing contract (<see cref="ICommand{TResult}"/> / <see cref="IQuery{TResult}"/>): a command or query has
/// exactly one result type, so such a type is a modeling error even though the sender dispatches it correctly
/// (IMP-06). It is registered automatically by
/// <see cref="ServiceCollectionExtensions.AddBuildingBlocks"/> unless the host sets
/// <see cref="BuildingBlocksOptions.ValidateHandlersOnStart"/> to <see langword="false"/>. Duplicate handlers are
/// already rejected at registration time, so this check only guards against absence and ambiguity.
/// </remarks>
internal sealed class HandlerRegistrationStartupValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyCollection<Assembly> _scannedAssemblies;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerRegistrationStartupValidator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The root service provider used to resolve handler contracts from a validation scope.</param>
    /// <param name="scannedAssemblies">The assemblies that were given to <see cref="BuildingBlocksOptions.AddHandlersFrom"/>.</param>
    public HandlerRegistrationStartupValidator(
        IServiceProvider serviceProvider,
        IReadOnlyCollection<Assembly> scannedAssemblies)
    {
        _serviceProvider = serviceProvider;
        _scannedAssemblies = scannedAssemblies;
    }

    /// <summary>
    /// Validates that every command and query in the scanned assemblies resolves to a handler.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A completed task when validation succeeds.</returns>
    /// <exception cref="InvalidOperationException">Thrown when at least one command or query has no resolvable handler or implements more than one result-bearing request contract; the message names every affected request type.</exception>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Validate();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Does nothing; validation only runs at startup.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Validate()
    {
        var missing = new List<string>();
        var ambiguous = new List<string>();
        using var scope = _serviceProvider.CreateScope();

        foreach (var assembly in _scannedAssemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is not { IsClass: true, IsAbstract: false } || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                var resultContracts = ResultContractsOf(type);
                if (resultContracts.Length > 1)
                {
                    ambiguous.Add(
                        $"'{type}' implements multiple result-bearing request contracts " +
                        $"({string.Join(", ", resultContracts.Select(ContractName))})");
                    continue;
                }

                foreach (var handlerContract in HandlerContractsOf(type))
                {
                    if (scope.ServiceProvider.GetService(handlerContract) is null)
                    {
                        missing.Add($"'{type}' has no registered '{handlerContract}'");
                    }
                }
            }
        }

        if (ambiguous.Count > 0)
        {
            throw new InvalidOperationException(
                "Handler registration validation failed at startup. A command or query has exactly one result " +
                $"type; split the ambiguous request types into one type per result: {string.Join("; ", ambiguous)}.");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Handler registration validation failed at startup. Every command and query must have exactly one " +
                "registered handler; make sure the handler implements the matching handler interface and its " +
                $"assembly is passed to AddHandlersFrom: {string.Join("; ", missing)}.");
        }
    }

    private static Type[] ResultContractsOf(Type requestType) =>
        [.. requestType.GetInterfaces()
            .Where(contract => contract.IsGenericType &&
                (contract.GetGenericTypeDefinition() == typeof(ICommand<>) ||
                 contract.GetGenericTypeDefinition() == typeof(IQuery<>)))];

    private static string ContractName(Type contract) =>
        $"{contract.Name.Split('`')[0]}<{contract.GetGenericArguments()[0].Name}>";

    private static IEnumerable<Type> HandlerContractsOf(Type requestType)
    {
        foreach (var contract in requestType.GetInterfaces())
        {
            if (contract == typeof(ICommand))
            {
                yield return typeof(ICommandHandler<>).MakeGenericType(requestType);
            }
            else if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICommand<>))
            {
                yield return typeof(ICommandHandler<,>).MakeGenericType(requestType, contract.GetGenericArguments()[0]);
            }
            else if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IQuery<>))
            {
                yield return typeof(IQueryHandler<,>).MakeGenericType(requestType, contract.GetGenericArguments()[0]);
            }
        }
    }
}
