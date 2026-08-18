using System.Diagnostics.CodeAnalysis;
using GaWeCodes.Application.Results;
using JasperFx;

namespace GaWeCodes.Persistence.EventSourced;

internal sealed class MartenFaultTranslator : IPersistenceFaultTranslator
{
    public bool TryTranslate(Exception exception, [NotNullWhen(true)] out Failure? failure)
    {
        if (exception is ConcurrencyException)
        {
            failure = Failure.Conflict(PersistenceFailureCodes.ConcurrencyConflict, exception.Message);
            return true;
        }

        failure = null;
        return false;
    }
}
