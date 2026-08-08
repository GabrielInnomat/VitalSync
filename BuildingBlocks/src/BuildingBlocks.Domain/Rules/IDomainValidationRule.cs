namespace BuildingBlocks.Domain.Rules;

public interface IDomainValidationRule
{
    string Code { get; }

    string? Target { get; }

    string Message { get; }

    bool IsInvalid();
}
