using System.Diagnostics.CodeAnalysis;
using BuildingBlocks.Application.Results;

namespace BuildingBlocks.Infrastructure.Persistence;

public interface IPersistenceFaultTranslator
{
    bool TryTranslate(Exception exception, [NotNullWhen(true)] out Failure? failure);
}
