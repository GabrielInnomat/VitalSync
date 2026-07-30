using BuildingBlocks.Application;
using BuildingBlocks.Domain;

namespace BuildingBlocks.Infrastructure.Tests.Fixtures;

// A self-contained assembly of valid, non-conflicting handlers used to exercise
// AddHandlersFrom assembly scanning (IMP-05). It is kept separate from the main
// test assembly so a scan sees exactly these handlers and is not affected by the
// intentional fixture duplicates other tests declare.
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
