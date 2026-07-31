using BuildingBlocks.Application;
using BuildingBlocks.Domain;

namespace ValidHandlersFixture;

public sealed record RegistrationCommand : ICommand;

public sealed record RegistrationQuery : IQuery<int>;

public sealed record RegistrationEvent : DomainEvent;

public sealed class RegistrationCommandHandler : ICommandHandler<RegistrationCommand>
{
    public Task<Result> Handle(RegistrationCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());
}

public sealed class RegistrationQueryHandler : IQueryHandler<RegistrationQuery, int>
{
    public Task<Result<int>> Handle(RegistrationQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(0));
}

public sealed class RegistrationProjectionHandler : IProjectionHandler<RegistrationEvent>
{
    public Task Handle(RegistrationEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class RegistrationMapper : IIntegrationEventMapper
{
    public IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent) => [];
}
