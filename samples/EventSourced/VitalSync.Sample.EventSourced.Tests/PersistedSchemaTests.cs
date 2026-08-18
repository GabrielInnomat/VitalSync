using System.Runtime.CompilerServices;
using GaWeCodes.Schema;
using VitalSync.Sample.EventSourced.Domain;
using VitalSync.Sample.EventSourced.Infrastructure.Integration;

namespace VitalSync.Sample.EventSourced.Tests;

public sealed class PersistedSchemaTests
{
    [Fact]
    public void ThePersistedEventSchema_MatchesTheApprovedSnapshot() =>
        PersistedSchema.Verify(
            ApprovedFilePath(),
            [typeof(Gadget).Assembly, typeof(GadgetRetiredIntegrationEvent).Assembly]);

    private static string ApprovedFilePath([CallerFilePath] string testFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(testFilePath)!, "EventSchema.approved.txt");
}
