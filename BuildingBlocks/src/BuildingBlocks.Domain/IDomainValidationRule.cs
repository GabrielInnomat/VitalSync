namespace BuildingBlocks.Domain;

public interface IDomainValidationRule
{
    string Message { get; }
    bool IsInvalid();
}
