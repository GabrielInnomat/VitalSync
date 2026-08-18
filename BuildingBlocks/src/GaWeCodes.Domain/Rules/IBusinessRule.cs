namespace GaWeCodes.Domain.Rules;

public interface IBusinessRule
{
    string Code { get; }

    string Message { get; }

    bool IsBroken();
}
