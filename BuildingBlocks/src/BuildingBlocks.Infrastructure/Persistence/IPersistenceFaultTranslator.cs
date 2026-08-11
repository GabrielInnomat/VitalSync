using System.Diagnostics.CodeAnalysis;
using BuildingBlocks.Application.Results;

namespace BuildingBlocks.Infrastructure.Persistence;

internal interface IPersistenceFaultTranslator
{
    bool TryTranslate(Exception exception, [NotNullWhen(true)] out Failure? failure);
}
