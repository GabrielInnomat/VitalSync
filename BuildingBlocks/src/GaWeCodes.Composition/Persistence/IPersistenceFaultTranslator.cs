using System.Diagnostics.CodeAnalysis;
using GaWeCodes.Application.Results;

namespace GaWeCodes.Persistence;

public interface IPersistenceFaultTranslator
{
    bool TryTranslate(Exception exception, [NotNullWhen(true)] out Failure? failure);
}
