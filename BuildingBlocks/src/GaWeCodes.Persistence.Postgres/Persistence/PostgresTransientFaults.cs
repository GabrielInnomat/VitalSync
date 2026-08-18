using Npgsql;

namespace GaWeCodes.Persistence;

public static class PostgresTransientFaults
{
    public static bool IsTransient(Exception exception) =>
        exception is NpgsqlException { IsTransient: true };
}
