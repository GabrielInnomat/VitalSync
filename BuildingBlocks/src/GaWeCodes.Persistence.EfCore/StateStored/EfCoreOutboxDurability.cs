using GaWeCodes.Wolverine.Persistence;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace GaWeCodes.Persistence.EfCore.StateStored;

internal sealed class EfCoreOutboxDurability(IEfCoreDatabaseDriver driver, string writeConnectionString)
    : IOutboxDurabilityConfigurator
{
    public void Configure(WolverineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        driver.PersistMessages(options, writeConnectionString);
        options.UseEntityFrameworkCoreTransactions();
    }
}
