using GaWeCodes.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace GaWeCodes.Persistence.StateStored;

public static class EfCorePersistenceOptionsExtensions
{
    public static BuildingBlocksOptions UseEfCorePersistence<TContext>(
        this BuildingBlocksOptions options,
        string connectionString,
        Action<DbContextOptionsBuilder>? configureContext = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionString);

        return options.UsePersistence(
            new EfCorePersistenceAdapter<TContext>(
                PostgresDatabaseDriver.Instance,
                connectionString,
                configureContext));
    }
}
