using System.Reflection;
using BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.DependencyInjection.Validation;

internal sealed class HandlerRegistrationStartupValidator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyCollection<Assembly> _scannedAssemblies;

    public HandlerRegistrationStartupValidator(
        IServiceProvider serviceProvider,
        IReadOnlyCollection<Assembly> scannedAssemblies)
    {
        _serviceProvider = serviceProvider;
        _scannedAssemblies = scannedAssemblies;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Validate();
        return Task.CompletedTask;
    }

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
