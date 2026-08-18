using GaWeCodes.Domain;

namespace GaWeCodes.Time;

internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset Now => timeProvider.GetUtcNow().ToUniversalTime();
}
