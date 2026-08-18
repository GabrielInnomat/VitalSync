using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;
using GaWeCodes.DependencyInjection;
using GaWeCodes.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace StateBusHost;

public static class MatrixHost
{
    private const string WriteConnectionString = "Host=localhost;Database=matrix-state-bus;Username=matrix;Password=matrix";

    private const string ExchangeName = "matrix.integration-events";

    private const string ContextName = "matrix";

    private static readonly Uri BrokerUri = new("amqp://localhost:5672");

    public static IHost Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocks(options => options
            .AddDomainEventsFrom(typeof(MatrixHost).Assembly)
            .UseEfCorePersistence<MatrixDbContext>(WriteConnectionString)
            .UseWolverineMessaging(BrokerUri, ExchangeName, ContextName));

        return builder.Build();
    }
}

public sealed class MatrixDbContext(DbContextOptions<MatrixDbContext> options) : DbContext(options);

[EventName("matrix-state-bus-probe-v1")]
public sealed record MatrixProbe(string Value) : DomainEvent;
