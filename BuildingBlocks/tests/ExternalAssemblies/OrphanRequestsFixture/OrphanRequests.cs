using BuildingBlocks.Application;

namespace OrphanRequestsFixture;

public sealed record OrphanCommand : ICommand;

public sealed record OrphanQuery : IQuery<int>;
