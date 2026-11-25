using System.Globalization;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.Dotnet;

var builder = DistributedApplication.CreateBuilder(args);

var isPublishMode = builder.ExecutionContext.IsPublishMode;

builder.AddDockerComposeEnvironment("vitalsync")
    .WithDashboard(dashboard => dashboard
        .WithImageRegistry("mcr.microsoft.com")
        .WithImage("aspire/dashboard", "17.0.0")
        .WithHostPort(18888)
        .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true"))
    .ConfigureComposeFile(file =>
    {
        BindToLoopback(file.Services["vitalsync-dashboard"], "18888:18888");
        file.AddVolume(new Volume { Name = WebKeysVolume });

        foreach (var volume in file.Volumes.Values)
        {
            volume.External = true;
            volume.Driver = null;
        }
    });

var hostedMessagingPassword = isPublishMode ? builder.AddParameter("hosted-messaging-password", secret: true) : null;
var hostedPostgresPassword = isPublishMode ? builder.AddParameter("hosted-postgres-password", secret: true) : null;

var messaging = builder.AddRabbitMQ("messaging", password: hostedMessagingPassword)
    .WithImageTag("4.3")
    .WithManagementPlugin()
    .WithDataVolume(isPublishMode ? "vitalsync-messaging-data" : null)
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Restart = "unless-stopped";
        BindToLoopback(service, "15672:15672");
    });

var postgres = builder.AddPostgres("postgres", password: hostedPostgresPassword)
    .WithImageTag("18.3")
    .WithDataVolume(isPublishMode ? "vitalsync-postgres-data" : null)
    .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");

if (!isPublishMode)
{
    postgres.WithPgAdmin(pgAdmin => pgAdmin.WithImageTag("9.15.0"));
}

var nutritionWrite = postgres.AddDatabase("nutrition-write", "nutrition-write");
var nutritionRead = postgres.AddDatabase("nutrition-read", "nutrition-read");

var fitnessWrite = postgres.AddDatabase("fitness-write", "fitness-write");
var fitnessRead = postgres.AddDatabase("fitness-read", "fitness-read");

var healthAnalyticsWrite = postgres.AddDatabase("health-analytics-write", "health-analytics-write");
var healthAnalyticsRead = postgres.AddDatabase("health-analytics-read", "health-analytics-read");

var nutritionMigrationService = builder.AddDotnetProject("nutrition-migration-service", "../../Services/Nutrition/VitalSync.Nutrition.MigrationService/VitalSync.Nutrition.MigrationService.csproj")
    .WithReference(nutritionWrite)
    .WaitFor(nutritionWrite)
    .WithReference(nutritionRead)
    .WaitFor(nutritionRead)
    .WithReference(messaging)
    .WaitFor(messaging);

var fitnessMigrationService = builder.AddDotnetProject("fitness-migration-service", "../../Services/Fitness/VitalSync.Fitness.MigrationService/VitalSync.Fitness.MigrationService.csproj")
    .WithReference(fitnessWrite)
    .WaitFor(fitnessWrite)
    .WithReference(fitnessRead)
    .WaitFor(fitnessRead)
    .WithReference(messaging)
    .WaitFor(messaging);

var healthAnalyticsMigrationService = builder.AddDotnetProject("health-analytics-migration-service", "../../Services/HealthAnalytics/VitalSync.HealthAnalytics.MigrationService/VitalSync.HealthAnalytics.MigrationService.csproj")
    .WithReference(healthAnalyticsWrite)
    .WaitFor(healthAnalyticsWrite)
    .WithReference(healthAnalyticsRead)
    .WaitFor(healthAnalyticsRead)
    .WithReference(messaging)
    .WaitFor(messaging);

var nutritionService = AsHostedService(builder.AddDotnetProject("nutrition-service", "../../Services/Nutrition/VitalSync.Nutrition.Api/VitalSync.Nutrition.Api.csproj"))
    .WithReference(nutritionRead)
    .WaitFor(nutritionRead)
    .WithReference(nutritionWrite)
    .WaitFor(nutritionWrite)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(nutritionMigrationService)
    .WithHttpHealthCheck("/health");

var fitnessService = AsHostedService(builder.AddDotnetProject("fitness-service", "../../Services/Fitness/VitalSync.Fitness.Api/VitalSync.Fitness.Api.csproj"))
    .WithReference(fitnessRead)
    .WaitFor(fitnessRead)
    .WithReference(fitnessWrite)
    .WaitFor(fitnessWrite)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(fitnessMigrationService)
    .WithHttpHealthCheck("/health");

var healthAnalyticsService = AsHostedService(builder.AddDotnetProject("health-analytics-service", "../../Services/HealthAnalytics/VitalSync.HealthAnalytics.Api/VitalSync.HealthAnalytics.Api.csproj"))
    .WithReference(healthAnalyticsRead)
    .WaitFor(healthAnalyticsRead)
    .WithReference(healthAnalyticsWrite)
    .WaitFor(healthAnalyticsWrite)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WaitForCompletion(healthAnalyticsMigrationService)
    .WithHttpHealthCheck("/health");

var backendForFrontend = AsHostedService(builder.AddDotnetProject("backend-for-frontend", "../../Bff/VitalSync.Bff/VitalSync.Bff.csproj"))
    .WithReference(nutritionService)
    .WaitFor(nutritionService)
    .WithReference(fitnessService)
    .WaitFor(fitnessService)
    .WithReference(healthAnalyticsService)
    .WaitFor(healthAnalyticsService)
    .WithHttpHealthCheck("/health");

var webFrontend = AsHostedService(builder.AddDotnetProject("web-frontend", "../../Frontend/VitalSync.Web/VitalSync.Web.csproj"))
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(backendForFrontend)
    .WaitFor(backendForFrontend);

if (isPublishMode)
{
    webFrontend.PublishAsDockerComposeService((_, service) =>
    {
        BindToLoopback(service, $"{ApplicationPort}:{ApplicationPort}");
        service.AddVolume(new Volume { Name = WebKeysVolume, Type = "volume", Source = WebKeysVolume, Target = "/home/app" });
    });

    var pgAdminEmail = builder.AddParameter("pgadmin-email");
    var pgAdminPassword = builder.AddParameter("pgadmin-password", secret: true);

    builder.AddContainer("pgadmin", "dpage/pgadmin4", "9.15.0")
        .WithImageRegistry("docker.io")
        .WithEnvironment("PGADMIN_DEFAULT_EMAIL", pgAdminEmail)
        .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", pgAdminPassword)
        .WithHttpEndpoint(targetPort: 80)
        .WithVolume(PgAdminVolume, "/var/lib/pgadmin")
        .WaitFor(postgres)
        .PublishAsDockerComposeService((_, service) =>
        {
            service.Restart = "unless-stopped";
            BindToLoopback(service, "5050:80");
        });
}

await builder.Build().RunAsync().ConfigureAwait(false);

IResourceBuilder<DotnetProjectResource> AsHostedService(IResourceBuilder<DotnetProjectResource> project)
{
    if (!isPublishMode)
    {
        return project;
    }

    foreach (var https in project.Resource.Annotations.OfType<EndpointAnnotation>().Where(e => e.UriScheme == "https").ToList())
    {
        project.Resource.Annotations.Remove(https);
    }

    return project
        .WithEndpoint("http", endpoint =>
        {
            endpoint.Port = ApplicationPort;
            endpoint.TargetPort = ApplicationPort;
        })
        .WithHttpEndpoint(name: "management", targetPort: ManagementPort)
        .WithEnvironment("ManagementPort", ManagementPort.ToString(CultureInfo.InvariantCulture))
        .WithEnvironment("ServiceDiscovery__AllowPlainHttp", "true")
        .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");
}

static void BindToLoopback(Service service, string mapping) => service.Ports = [$"127.0.0.1:{mapping}"];

internal static partial class Program
{
    private const int ApplicationPort = 8080;
    private const int ManagementPort = 8081;
    private const string WebKeysVolume = "vitalsync-web-keys";
    private const string PgAdminVolume = "vitalsync-pgadmin-data";
}
