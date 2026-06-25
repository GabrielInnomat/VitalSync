using MediatR;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// A MediatR pipeline behavior that logs the start, completion, and failure
/// of each request together with its elapsed time.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>Initializes the behavior with a logger.</summary>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var startTimestamp = TimeProvider.System.GetTimestamp();
        try
        {
            var response = await next();
            var elapsed = TimeProvider.System.GetElapsedTime(startTimestamp);
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName,
                elapsed.TotalMilliseconds);
            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failure handling {RequestName}", requestName);
            throw;
        }
    }
}