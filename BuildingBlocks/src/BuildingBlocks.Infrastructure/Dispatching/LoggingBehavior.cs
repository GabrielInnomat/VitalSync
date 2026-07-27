using System.Diagnostics;
using BuildingBlocks.Application;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Dispatching;

/// <summary>
/// Pipeline behavior that emits structured logs for every dispatched request.
/// </summary>
/// <remarks>
/// The behavior logs the request name, the outcome (success, or the distinct failure categories on failure), and the
/// duration; it never logs request payloads, so sensitive command or query data cannot leak into logs by default.
/// Register it second, inside the exception-translation behavior, so translated failures are logged as failed results
/// while unexpected exceptions are still observed (logged as faulted, then rethrown).
/// </remarks>
/// <typeparam name="TRequest">The type of the request flowing through the pipeline.</typeparam>
/// <typeparam name="TResponse">The type of the result produced by the pipeline.</typeparam>
/// <param name="logger">The logger the behavior writes to.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(TRequest request, RequestPipelineContinuation<TResponse> continuation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        var requestName = typeof(TRequest).Name;
        Log.RequestStarted(logger, requestName);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var response = await continuation(cancellationToken).ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(startedAt);

            if (response.IsSuccess)
            {
                Log.RequestSucceeded(logger, requestName, elapsed.TotalMilliseconds);
            }
            else
            {
                var categories = string.Join(", ", response.Failures.Select(failure => failure.Category).Distinct());
                Log.RequestFailed(logger, requestName, categories, elapsed.TotalMilliseconds);
            }

            return response;
        }
        catch (Exception)
        {
            Log.RequestFaulted(logger, requestName, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            throw;
        }
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> RequestStartedMessage =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, nameof(RequestStarted)),
                "Handling {RequestName}");

        private static readonly Action<ILogger, string, double, Exception?> RequestSucceededMessage =
            LoggerMessage.Define<string, double>(
                LogLevel.Information,
                new EventId(2, nameof(RequestSucceeded)),
                "Handled {RequestName} successfully in {ElapsedMilliseconds:0.###} ms");

        private static readonly Action<ILogger, string, string, double, Exception?> RequestFailedMessage =
            LoggerMessage.Define<string, string, double>(
                LogLevel.Warning,
                new EventId(3, nameof(RequestFailed)),
                "Handled {RequestName} with failure categories [{FailureCategories}] in {ElapsedMilliseconds:0.###} ms");

        private static readonly Action<ILogger, string, double, Exception?> RequestFaultedMessage =
            LoggerMessage.Define<string, double>(
                LogLevel.Error,
                new EventId(4, nameof(RequestFaulted)),
                "Handling {RequestName} threw an unexpected exception after {ElapsedMilliseconds:0.###} ms");

        public static void RequestStarted(ILogger logger, string requestName) =>
            RequestStartedMessage(logger, requestName, null);

        public static void RequestSucceeded(ILogger logger, string requestName, double elapsedMilliseconds) =>
            RequestSucceededMessage(logger, requestName, elapsedMilliseconds, null);

        public static void RequestFailed(ILogger logger, string requestName, string failureCategories, double elapsedMilliseconds) =>
            RequestFailedMessage(logger, requestName, failureCategories, elapsedMilliseconds, null);

        public static void RequestFaulted(ILogger logger, string requestName, double elapsedMilliseconds) =>
            RequestFaultedMessage(logger, requestName, elapsedMilliseconds, null);
    }
}
