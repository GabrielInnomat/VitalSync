using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Tests.OrphanRequests;

// This assembly deliberately contains commands and queries WITHOUT handlers so that a
// scan of it exercises the startup handler validation (IMP-05 step 4). It is isolated
// in its own assembly: placing these types in the main test assembly would make the
// startup check fail for every test that scans that assembly.
public sealed record OrphanCommand : ICommand;

public sealed record OrphanQuery : IQuery<int>;
