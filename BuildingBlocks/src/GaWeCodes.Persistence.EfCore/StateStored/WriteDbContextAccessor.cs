using Microsoft.EntityFrameworkCore;

namespace GaWeCodes.Persistence.EfCore.StateStored;

internal sealed class WriteDbContextAccessor
{
    public WriteDbContextAccessor(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Context = context;
    }

    public DbContext Context { get; }
}
