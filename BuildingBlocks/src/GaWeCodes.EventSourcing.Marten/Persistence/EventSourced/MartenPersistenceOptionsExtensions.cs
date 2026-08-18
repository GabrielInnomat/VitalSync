using GaWeCodes.DependencyInjection;

namespace GaWeCodes.Persistence.EventSourced;

public static class MartenPersistenceOptionsExtensions
{
    public static BuildingBlocksOptions UseMartenEventSourcing(
        this BuildingBlocksOptions options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionString);

        return options.UsePersistence(new MartenPersistenceAdapter(connectionString));
    }
}
