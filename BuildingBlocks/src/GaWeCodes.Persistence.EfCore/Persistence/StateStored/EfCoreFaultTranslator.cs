using System.Diagnostics.CodeAnalysis;
using GaWeCodes.Application.Results;
using Microsoft.EntityFrameworkCore;

namespace GaWeCodes.Persistence.StateStored;

internal sealed class EfCoreFaultTranslator : IPersistenceFaultTranslator
{
    public bool TryTranslate(Exception exception, [NotNullWhen(true)] out Failure? failure)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            failure = Failure.Conflict(PersistenceFailureCodes.ConcurrencyConflict, exception.Message);
            return true;
        }

        failure = null;
        return false;
    }
}
