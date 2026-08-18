using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;
using GaWeCodes.DependencyInjection;
using GaWeCodes.Persistence.EventSourced;
using Microsoft.Extensions.Hosting;

namespace EventsBusHost;

public static class MatrixHost
{
    private const string WriteConnectionString = "Host=localhost;Database=matrix-events-bus;Username=matrix;Password=matrix";

    private const string ExchangeName = "matrix.integration-events";

    private const string ContextName = "matrix";

    private static readonly Uri BrokerUri = new("amqp://localhost:5672");

    public static IHost Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocks(options => options
            .AddDomainEventsFrom(typeof(MatrixHost).Assembly)
            .UseMartenEventSourcing(WriteConnectionString)
            .UseWolverineMessaging(BrokerUri, ExchangeName, ContextName));

        return builder.Build();
    }
}

[EventName("matrix-events-bus-probe-v1")]
public sealed record MatrixProbe(string Value) : DomainEvent;
