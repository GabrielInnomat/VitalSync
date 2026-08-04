namespace BuildingBlocks.Infrastructure.DependencyInjection.Wiring;

internal sealed record MessagingSettings(Uri RabbitMqUri, string ExchangeName, string ContextName);
