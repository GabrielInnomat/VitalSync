using System.Runtime.CompilerServices;
using GaWeCodes.Schema;
using VitalSync.Sample.Contracts;
using VitalSync.Sample.StateStored.Domain;

namespace VitalSync.Sample.StateStored.Tests;

public sealed class PersistedSchemaTests
{
    [Fact]
    public void ThePersistedEventSchema_MatchesTheApprovedSnapshot() =>
        PersistedSchema.Verify(
            ApprovedFilePath(),
            [typeof(Widget).Assembly, typeof(WidgetCreatedIntegrationEvent).Assembly]);

    private static string ApprovedFilePath([CallerFilePath] string testFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(testFilePath)!, "EventSchema.approved.txt");
}
