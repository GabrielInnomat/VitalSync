namespace BuildingBlocks.Domain.Rules;

public interface IDomainValidationRule
{
    string Message { get; }

    bool IsInvalid();
}
