using RabbitMQ.Client;

namespace VitalSync.ServiceDefaults;

internal sealed class RabbitMqReadinessConnection(Uri brokerUri) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync()
    {
        if (_connection is { IsOpen: true } open)
        {
            return open;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true } reopened)
            {
                return reopened;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                Uri = brokerUri,
                ClientProvidedName = "vitalsync-readiness",
            };

            _connection = await factory.CreateConnectionAsync().ConfigureAwait(false);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _gate.Dispose();
    }
}
