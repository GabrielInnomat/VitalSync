using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Naming;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Wolverine;

namespace ForeignStoreHost;

public static class MatrixHost
{
    private const string WriteConnectionString = "DataSource=matrix-foreign";

    public static IHost Build()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddBuildingBlocks(options => options
            .AddDomainEventsFrom(typeof(MatrixHost).Assembly)
            .UsePersistence(new EfCorePersistenceAdapter<MatrixDbContext>(
                new ForeignDatabaseDriver(),
                WriteConnectionString)));

        return builder.Build();
    }
}

public sealed class ForeignDatabaseDriver : IEfCoreDatabaseDriver
{
    public IReadOnlyList<IPersistenceFaultTranslator> FaultTranslators => [];

    public void ConfigureContext(DbContextOptionsBuilder builder, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseInMemoryDatabase(connectionString);
    }

    public void PersistMessages(WolverineOptions options, string connectionString)
    {
    }

    public bool IsTransientFault(Exception exception) => false;
}

public sealed class MatrixDbContext(DbContextOptions<MatrixDbContext> options) : DbContext(options);

[EventName("matrix-foreign-probe-v1")]
public sealed record MatrixProbe(string Value) : DomainEvent;
