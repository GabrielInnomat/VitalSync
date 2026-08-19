using GaWeCodes.Domain;

namespace GaWeCodes.Core.Time;

internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset Now => timeProvider.GetUtcNow().ToUniversalTime();
}
