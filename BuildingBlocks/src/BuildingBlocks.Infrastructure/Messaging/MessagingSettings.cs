namespace BuildingBlocks.Infrastructure.Messaging;

internal sealed record MessagingSettings(Uri RabbitMqUri, string ExchangeName, string ContextName);
