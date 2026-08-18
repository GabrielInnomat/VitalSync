using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;
using GaWeCodes.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BareHost;

public static class MatrixHost
{
    public static IHost Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocks(options => options.AddDomainEventsFrom(typeof(MatrixHost).Assembly));

        return builder.Build();
    }
}

[EventName("matrix-bare-probe-v1")]
public sealed record MatrixProbe(string Value) : DomainEvent;
