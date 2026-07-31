using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Tests.OrphanRequestsFixture;

public sealed record OrphanCommand : ICommand;

public sealed record OrphanQuery : IQuery<int>;
