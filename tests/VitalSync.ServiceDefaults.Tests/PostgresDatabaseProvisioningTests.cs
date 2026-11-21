using Microsoft.Extensions.Hosting;
using VitalSync.ServiceDefaults;

namespace VitalSync.ServiceDefaults.Tests;

public class PostgresDatabaseProvisioningTests(PostgresServer server) : IClassFixture<PostgresServer>
{
    [Fact]
    public async Task EnsureDatabasesExist_WithUnknownConnectionName_Throws()
    {
        using var host = Host.CreateApplicationBuilder().Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.EnsureDatabasesExistAsync(["nutrition-write"], TestContext.Current.CancellationToken));

        Assert.Contains("nutrition-write", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureDatabasesExist_WhenTheDatabasesAreMissing_CreatesThem()
    {
        Assert.SkipWhen(server.SkipReason is not null, server.SkipReason ?? string.Empty);
        var write = UniqueName("context-write");
        var read = UniqueName("context-read");
        using var host = HostWith(("context-write", write), ("context-read", read));

        await host.EnsureDatabasesExistAsync(["context-write", "context-read"], TestContext.Current.CancellationToken);

        Assert.True(await server.DatabaseExistsAsync(write));
        Assert.True(await server.DatabaseExistsAsync(read));
    }

    [Fact]
    public async Task EnsureDatabasesExist_WhenTheDatabaseAlreadyExists_LeavesItInPlace()
    {
        Assert.SkipWhen(server.SkipReason is not null, server.SkipReason ?? string.Empty);
        var database = UniqueName("context-write");
        using var host = HostWith(("context-write", database));

        await host.EnsureDatabasesExistAsync(["context-write"], TestContext.Current.CancellationToken);
        await host.EnsureDatabasesExistAsync(["context-write"], TestContext.Current.CancellationToken);

        Assert.True(await server.DatabaseExistsAsync(database));
    }

    [Fact]
    public async Task EnsureDatabasesExist_WithANameThatNeedsQuoting_CreatesExactlyThatName()
    {
        Assert.SkipWhen(server.SkipReason is not null, server.SkipReason ?? string.Empty);
        var database = UniqueName("Mixed\"Case; DROP");
        using var host = HostWith(("context-write", database));

        await host.EnsureDatabasesExistAsync(["context-write"], TestContext.Current.CancellationToken);

        Assert.True(await server.DatabaseExistsAsync(database));
    }

    private IHost HostWith(params (string ConnectionName, string Database)[] databases)
    {
        var builder = Host.CreateApplicationBuilder();
        foreach (var (connectionName, database) in databases)
        {
            builder.Configuration[$"ConnectionStrings:{connectionName}"] = server.ConnectionStringFor(database);
        }

        return builder.Build();
    }

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
