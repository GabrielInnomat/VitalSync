using Testcontainers.RabbitMq;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Shared disposable RabbitMQ instance for the integration-event routing tests. When Docker is unavailable the
/// container fails to start and <see cref="Available"/> stays <c>false</c>, so the tests skip instead of failing.
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;

    public Uri ConnectionUri { get; private set; } = new("amqp://localhost");

    public bool Available { get; private set; }

    public string SkipReason { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new RabbitMqBuilder("rabbitmq:4-alpine").Build();
            await _container.StartAsync();
            ConnectionUri = new Uri(_container.GetConnectionString());
            Available = true;
        }
        catch (Exception exception)
        {
            Available = false;
            SkipReason = $"RabbitMQ Testcontainer could not be started (Docker required): {exception.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "RabbitMQ";
}
