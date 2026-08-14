using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Naming;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence.EventSourced;
using Microsoft.Extensions.Hosting;

namespace EventsHost;

public static class MatrixHost
{
    private const string WriteConnectionString = "Host=localhost;Database=matrix-events;Username=matrix;Password=matrix";

    public static IHost Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocks(options => options
            .AddDomainEventsFrom(typeof(MatrixHost).Assembly)
            .UseMartenEventSourcing(WriteConnectionString));

        return builder.Build();
    }
}

[EventName("matrix-events-probe-v1")]
public sealed record MatrixProbe(string Value) : DomainEvent;
