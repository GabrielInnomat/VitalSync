using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace VitalSync.ServiceDefaults;

public static class AspireExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";
    private const string DeadLetterEndpointPath = "/health/dead-letters";

    private static readonly Action<ILogger, string, string, string, string, Exception?> HealthChecksNotMappedAction =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(1, nameof(HealthChecksNotMapped)),
            "Health check endpoints are not mapped in '{Environment}' because no 'ManagementPort' is configured. " +
            "Set 'ManagementPort' to expose '{Health}', '{Alive}' and '{DeadLetters}' on an internal-only port for orchestrator probes.");

    public const string LiveTag = "live";
    public const string ReadyTag = "ready";
    public const string DeadLetterTag = "dead-letters";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        builder.Services.Configure<ServiceDiscoveryOptions>(options => options.AllowedSchemes = ["https"]);

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Npgsql"))
            .WithTracing(tracing => tracing.AddSource(builder.Environment.ApplicationName)
                    .AddSource("Thessera")
                    .AddSource("Npgsql")
                    .AddSource("Wolverine")
                    .AddSource("Marten")
                    .AddAspNetCoreInstrumentation(tracing =>
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath, StringComparison.OrdinalIgnoreCase)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath, StringComparison.OrdinalIgnoreCase)
                    )
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation());

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlpExporter)
        {
            builder.Services
                .AddOpenTelemetry()
                .UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag]);

        return builder;
    }

    public static TBuilder AddNpgSqlReadinessCheck<TBuilder>(
        this TBuilder builder,
        string connectionName = "postgres",
        string name = "postgres") where TBuilder : IHostApplicationBuilder
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string named '{connectionName}' is configured, so the readiness check " +
                $"'{name}' cannot be added. Does the AppHost reference a resource of that name?");
        }

        builder.Services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: name,
                tags: [ReadyTag]);

        return builder;
    }

    public static TBuilder AddRabbitMqReadinessCheck<TBuilder>(
        this TBuilder builder,
        string name = "rabbitmq") where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddRabbitMQ(
                name: name,
                tags: [ReadyTag]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var liveOptions = new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(LiveTag)
        };

        var readyOptions = new HealthCheckOptions
        {
            Predicate = r => !r.Tags.Contains(DeadLetterTag)
        };

        var deadLetterOptions = new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(DeadLetterTag)
        };

        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks(HealthEndpointPath, readyOptions);
            app.MapHealthChecks(AlivenessEndpointPath, liveOptions);
            app.MapHealthChecks(DeadLetterEndpointPath, deadLetterOptions);
            return app;
        }

        var managementPort = app.Configuration.GetValue<int?>("ManagementPort");
        if (managementPort is int port)
        {
            app.MapHealthChecks(HealthEndpointPath, readyOptions).RequireHost($"*:{port}");
            app.MapHealthChecks(AlivenessEndpointPath, liveOptions).RequireHost($"*:{port}");
            app.MapHealthChecks(DeadLetterEndpointPath, deadLetterOptions).RequireHost($"*:{port}");
        }
        else
        {
            app.Logger.HealthChecksNotMapped(app.Environment.EnvironmentName, HealthEndpointPath, AlivenessEndpointPath, DeadLetterEndpointPath);
        }

        return app;
    }

    private static void HealthChecksNotMapped(this ILogger logger, string environment, string health, string alive, string deadLetters) =>
        HealthChecksNotMappedAction(logger, environment, health, alive, deadLetters, null);
}
