using System.Diagnostics.CodeAnalysis;
using BuildingBlocks.Application.Results;
using Npgsql;

namespace BuildingBlocks.Infrastructure.Persistence;

internal sealed class PostgresFaultTranslator : IPersistenceFaultTranslator
{
    public bool TryTranslate(Exception exception, [NotNullWhen(true)] out Failure? failure)
    {
        if (exception is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation)
        {
            failure = Failure.Conflict(PersistenceFailureCodes.UniqueViolation, Describe(violation));
            return true;
        }

        failure = null;
        return false;
    }

    private static string Describe(PostgresException exception) =>
        string.IsNullOrWhiteSpace(exception.ConstraintName)
            ? exception.Message
            : $"The unique constraint '{exception.ConstraintName}' was violated.";
}
