namespace GaWeCodes.Domain;

public interface IClock
{
    DateTimeOffset Now { get; }
}
