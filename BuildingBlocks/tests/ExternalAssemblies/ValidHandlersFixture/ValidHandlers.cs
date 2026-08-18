using GaWeCodes.Application.Cqrs;
using GaWeCodes.Application.DomainEvents;
using GaWeCodes.Application.IntegrationEvents;
using GaWeCodes.Application.Results;
using GaWeCodes.Domain.Events;
using GaWeCodes.Domain.Naming;

namespace ValidHandlersFixture;

public sealed record RegistrationCommand : ICommand;

public sealed record RegistrationQuery : IQuery<int>;

[EventName("registration-v1")]
public sealed record RegistrationEvent : DomainEvent;

public sealed class RegistrationCommandHandler : ICommandHandler<RegistrationCommand>
{
    public Task<Result> HandleAsync(RegistrationCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}

public sealed class RegistrationQueryHandler : IQueryHandler<RegistrationQuery, int>
{
    public Task<Result<int>> HandleAsync(RegistrationQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(0));
}

public sealed class RegistrationProjectionHandler : IProjectionHandler<RegistrationEvent>
{
    public Task HandleAsync(RegistrationEvent domainEvent, DomainEventMetadata metadata, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class RegistrationMapper : IIntegrationEventMapper<RegistrationEvent>
{
    public IReadOnlyCollection<IIntegrationEvent> Map(RegistrationEvent domainEvent, DomainEventMetadata metadata) => [];
}
