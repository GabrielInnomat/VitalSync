namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal abstract record PersistenceChoice
{
    private PersistenceChoice()
    {
    }

    public static PersistenceChoice None { get; } = new NoneSelected();

    public static PersistenceChoice NoPersistence { get; } = new NoPersistenceSelected();

    public abstract string Description { get; }

    public bool IsChosen => this is not NoneSelected;

    public bool IsSelected => this is MartenEventStore or EfCoreWriteDatabase;

    public bool IsDeliberatelyWithoutPersistence => this is NoPersistenceSelected;

    public string? EfCoreWriteConnectionString => (this as EfCoreWriteDatabase)?.ConnectionString;

    public string? WriteConnectionString => this switch
    {
        EfCoreWriteDatabase efCore => efCore.ConnectionString,
        MartenEventStore marten => marten.ConnectionString,
        _ => null,
    };

    public static PersistenceChoice Marten(string connectionString) => new MartenEventStore(connectionString);

    public static PersistenceChoice EfCore(string connectionString) => new EfCoreWriteDatabase(connectionString);

    private sealed record NoneSelected : PersistenceChoice
    {
        public override string Description => "none";
    }

    private sealed record NoPersistenceSelected : PersistenceChoice
    {
        public override string Description => "UseNoPersistence";
    }

    private sealed record MartenEventStore(string ConnectionString) : PersistenceChoice
    {
        public override string Description => "UseMartenEventSourcing";
    }

    private sealed record EfCoreWriteDatabase(string ConnectionString) : PersistenceChoice
    {
        public override string Description => "UseEfCorePersistence";
    }
}
