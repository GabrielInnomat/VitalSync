using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.Application.DomainEvents;
using BuildingBlocks.Application.IntegrationEvents;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Naming;

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

public sealed class RegistrationMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent, DomainEventMetadata metadata) => [];
}
