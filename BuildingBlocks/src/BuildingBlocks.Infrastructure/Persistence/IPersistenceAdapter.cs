namespace BuildingBlocks.Infrastructure.Persistence;

internal interface IPersistenceAdapter
{
    string Description { get; }

    string WriteConnectionString { get; }

    void Register(PersistenceRegistrationContext context);
}
