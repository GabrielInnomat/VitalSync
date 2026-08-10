using BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class PersistenceChoiceTests
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=test;******";

    [Fact]
    public void None_IsNotSelectedAndCarriesNoConnectionString()
    {
        Assert.False(PersistenceChoice.None.IsSelected);
        Assert.Null(PersistenceChoice.None.EfCoreWriteConnectionString);
    }

    [Fact]
    public void Marten_IsSelectedAndCarriesItsWriteConnectionStringButNotTheEfCoreOne()
    {
        var choice = PersistenceChoice.Marten(ConnectionString);

        Assert.True(choice.IsSelected);
        Assert.Equal(ConnectionString, choice.WriteConnectionString);
        Assert.Null(choice.EfCoreWriteConnectionString);
    }

    [Fact]
    public void TwoMartenChoicesOverDifferentDatabases_Throw()
    {
        var settings = new WolverineWiringSettings();
        settings.SelectPersistence(PersistenceChoice.Marten(ConnectionString));

        var exception = Assert.Throws<InvalidOperationException>(
            () => settings.SelectPersistence(PersistenceChoice.Marten("Host=elsewhere")));

        Assert.Contains("twice with different arguments", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EfCore_IsSelectedAndCarriesItsConnectionString()
    {
        var choice = PersistenceChoice.EfCore(ConnectionString);

        Assert.True(choice.IsSelected);
        Assert.Equal(ConnectionString, choice.EfCoreWriteConnectionString);
    }

    [Fact]
    public void TwoEfCoreChoicesOverTheSameConnectionString_AreEqual()
    {
        Assert.Equal(PersistenceChoice.EfCore(ConnectionString), PersistenceChoice.EfCore(ConnectionString));
        Assert.NotEqual(PersistenceChoice.EfCore(ConnectionString), PersistenceChoice.EfCore("Host=elsewhere"));
    }

    [Fact]
    public void TheHierarchyIsClosed()
    {
        var declared = typeof(PersistenceChoice).Assembly
            .GetTypes()
            .Where(type => type != typeof(PersistenceChoice) && typeof(PersistenceChoice).IsAssignableFrom(type))
            .ToArray();

        Assert.Equal(4, declared.Length);
        Assert.All(declared, type => Assert.Equal(typeof(PersistenceChoice), type.DeclaringType));
    }

    [Fact]
    public void DeliberatelyWithoutPersistence_IsChosenButNotSelected()
    {
        var settings = new WolverineWiringSettings();
        settings.SelectPersistence(PersistenceChoice.NoPersistence);

        Assert.True(settings.Persistence.IsChosen);
        Assert.False(settings.Persistence.IsSelected);
        Assert.True(settings.Persistence.IsDeliberatelyWithoutPersistence);
        Assert.False(settings.RequiresWolverine);
    }

    [Fact]
    public void NoPersistenceAfterAStrategy_Throws()
    {
        var settings = new WolverineWiringSettings();
        settings.SelectPersistence(PersistenceChoice.Marten(ConnectionString));

        var exception = Assert.Throws<InvalidOperationException>(
            () => settings.SelectPersistence(PersistenceChoice.NoPersistence));

        Assert.Contains("UseNoPersistence", exception.Message, StringComparison.Ordinal);
        Assert.Contains("UseMartenEventSourcing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectingTheSameChoiceTwice_IsAccepted()
    {
        var settings = new WolverineWiringSettings();

        settings.SelectPersistence(PersistenceChoice.EfCore(ConnectionString));
        settings.SelectPersistence(PersistenceChoice.EfCore(ConnectionString));

        Assert.Equal(ConnectionString, settings.Persistence.EfCoreWriteConnectionString);
    }

    [Fact]
    public void EitherPersistenceChoice_MakesWolverineRequired()
    {
        var efCore = new WolverineWiringSettings();
        efCore.SelectPersistence(PersistenceChoice.EfCore(ConnectionString));

        var marten = new WolverineWiringSettings();
        marten.SelectPersistence(PersistenceChoice.Marten(ConnectionString));

        Assert.False(new WolverineWiringSettings().RequiresWolverine);
        Assert.True(efCore.RequiresWolverine);
        Assert.True(marten.RequiresWolverine);
    }
}
