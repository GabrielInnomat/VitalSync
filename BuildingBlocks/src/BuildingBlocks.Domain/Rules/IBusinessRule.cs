
namespace BuildingBlocks.Domain.Rules;

public interface IBusinessRule
{
    string Message { get; }

    bool IsBroken();
}
