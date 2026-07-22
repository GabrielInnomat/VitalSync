namespace BuildingBlocks.Domain.Tests.TestDoubles;

/// <summary>Configurable <see cref="IBusinessRule"/> that records whether it was evaluated.</summary>
public sealed class FakeBusinessRule(bool isBroken, string message = "business rule broken")
    : IBusinessRule
{
    public bool Evaluated { get; private set; }

    public string Message => message;

    public bool IsBroken()
    {
        Evaluated = true;
        return isBroken;
    }
}

/// <summary>Configurable <see cref="IDomainValidationRule"/> that records whether it was evaluated.</summary>
public sealed class FakeValidationRule(bool isInvalid, string message = "validation rule invalid")
    : IDomainValidationRule
{
    public bool Evaluated { get; private set; }

    public string Message => message;

    public bool IsInvalid()
    {
        Evaluated = true;
        return isInvalid;
    }
}
