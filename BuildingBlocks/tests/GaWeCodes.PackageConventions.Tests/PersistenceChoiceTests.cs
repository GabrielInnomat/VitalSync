using GaWeCodes.DependencyInjection.Wiring;
using GaWeCodes.Persistence;
using GaWeCodes.Persistence.EventSourced;
using GaWeCodes.Persistence.StateStored;
using Microsoft.EntityFrameworkCore;

namespace GaWeCodes.Tests;

public sealed class PersistenceChoiceTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;******";

    private static EfCorePersistenceAdapter<TestDbContext> EfCore(string connectionString) =>
        new(PostgresDatabaseDriver.Instance, connectionString, null);

    [Fact]
    public void None_IsNotSelectedAndCarriesNoConnectionString()
    {
        Assert.False(PersistenceChoice.None.IsSelected);
        Assert.Null(PersistenceChoice.None.WriteConnectionString);
        Assert.Null(PersistenceChoice.None.Adapter);
    }

    [Fact]
    public void Marten_IsSelectedAndCarriesItsWriteConnectionString()
    {
        var choice = PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString));

        Assert.True(choice.IsSelected);
        Assert.Equal(ConnectionString, choice.WriteConnectionString);
        Assert.Equal("UseMartenEventSourcing", choice.Description);
    }

    [Fact]
    public void TwoMartenChoicesOverDifferentDatabases_Throw()
    {
        var settings = new BuildingBlocksWiringSettings();
        settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)));

        var exception = Assert.Throws<InvalidOperationException>(
            () => settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter("Host=elsewhere"))));

        Assert.Contains("twice with different arguments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EfCore_IsSelectedAndCarriesItsConnectionString()
    {
        var choice = PersistenceChoice.For(EfCore(ConnectionString));

        Assert.True(choice.IsSelected);
        Assert.Equal(ConnectionString, choice.WriteConnectionString);
        Assert.Equal("UseEfCorePersistence", choice.Description);
    }

    [Fact]
    public void TwoEfCoreChoicesOverTheSameConnectionString_AreEqual()
    {
        Assert.Equal(
            PersistenceChoice.For(EfCore(ConnectionString)),
            PersistenceChoice.For(EfCore(ConnectionString)));
        Assert.NotEqual(
            PersistenceChoice.For(EfCore(ConnectionString)),
            PersistenceChoice.For(EfCore("Host=elsewhere")));
    }

    [Fact]
    public void TheHierarchyIsOpen_AForeignAdapterIsAcceptedAndDescribesItself()
    {
        var settings = new BuildingBlocksWiringSettings();

        settings.Persistence.Select(PersistenceChoice.For(new ForeignAdapter(ConnectionString)));

        Assert.True(settings.Persistence.IsSelected);
        Assert.True(settings.RequiresRuntime);
        Assert.Equal(ConnectionString, settings.Persistence.WriteConnectionString);
        Assert.Equal("UseForeignPersistence", settings.Persistence.Choice.Description);
    }

    [Fact]
    public void AForeignAdapterAlongsideABuiltInOne_IsRejectedAsTwoStrategies()
    {
        var settings = new BuildingBlocksWiringSettings();
        settings.Persistence.Select(PersistenceChoice.For(EfCore(ConnectionString)));

        var exception = Assert.Throws<InvalidOperationException>(
            () => settings.Persistence.Select(PersistenceChoice.For(new ForeignAdapter(ConnectionString))));

        Assert.Contains("Two persistence strategies", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseForeignPersistence", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DeliberatelyWithoutPersistence_IsChosenButNotSelected()
    {
        var settings = new BuildingBlocksWiringSettings();
        settings.Persistence.Select(PersistenceChoice.NoPersistence);

        Assert.True(settings.Persistence.IsChosen);
        Assert.False(settings.Persistence.IsSelected);
        Assert.True(settings.Persistence.IsDeliberatelyWithoutPersistence);
        Assert.False(settings.RequiresRuntime);
    }

    [Fact]
    public void NoPersistenceAfterAStrategy_Throws()
    {
        var settings = new BuildingBlocksWiringSettings();
        settings.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)));

        var exception = Assert.Throws<InvalidOperationException>(
            () => settings.Persistence.Select(PersistenceChoice.NoPersistence));

        Assert.Contains("UseNoPersistence", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseMartenEventSourcing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectingTheSameChoiceTwice_IsAccepted()
    {
        var settings = new BuildingBlocksWiringSettings();

        settings.Persistence.Select(PersistenceChoice.For(EfCore(ConnectionString)));
        settings.Persistence.Select(PersistenceChoice.For(EfCore(ConnectionString)));

        Assert.Equal(ConnectionString, settings.Persistence.WriteConnectionString);
    }

    [Fact]
    public void EitherPersistenceChoice_MakesTheRuntimeRequired()
    {
        var efCore = new BuildingBlocksWiringSettings();
        efCore.Persistence.Select(PersistenceChoice.For(EfCore(ConnectionString)));

        var marten = new BuildingBlocksWiringSettings();
        marten.Persistence.Select(PersistenceChoice.For(new MartenPersistenceAdapter(ConnectionString)));

        Assert.False(new BuildingBlocksWiringSettings().RequiresRuntime);
        Assert.True(efCore.RequiresRuntime);
        Assert.True(marten.RequiresRuntime);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

    private sealed record ForeignAdapter(string WriteConnectionString) : IPersistenceAdapter
    {
        public string Description => "UseForeignPersistence";

        public AggregateStyle AggregateStyle => AggregateStyle.StateStored;

        public bool IsTransientFault(Exception exception) => false;

        public void Register(PersistenceRegistrationContext context)
        {
        }
    }
}
