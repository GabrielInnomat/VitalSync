using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

namespace BuildingBlocks.Infrastructure.Persistence.StateStored;

internal sealed class EfCoreOutboxDurability(string writeConnectionString) : IOutboxDurabilityConfigurator
{
    public void Configure(WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PersistMessagesWithPostgresql(writeConnectionString);
        options.UseEntityFrameworkCoreTransactions();
    }
}
