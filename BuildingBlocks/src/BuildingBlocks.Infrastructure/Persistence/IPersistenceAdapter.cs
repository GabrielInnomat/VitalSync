namespace BuildingBlocks.Infrastructure.Persistence;

public interface IPersistenceAdapter
{
    string Description { get; }

    string WriteConnectionString { get; }

    bool IsTransientFault(Exception exception);

    void Register(PersistenceRegistrationContext context);
}
