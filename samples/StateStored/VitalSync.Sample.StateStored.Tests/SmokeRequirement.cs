namespace VitalSync.Sample.StateStored.Tests;

// The counterpart to BuildingBlocks' ContainerRequirement, for the other silent skip in this repository:
// a smoke test whose API URL is not set skips, and the run stays green while the tests that need a real
// web host, a real broker and a real database never ran. Exactly those found the defects that build and
// unit tests were green through (WalkingSkeleton.md §3), so "did not run" must be distinguishable from
// "passed" wherever it matters - in CI.
//
// Deliberately duplicated in the event-sourced sample: the two throwaway projects share no test assembly,
// and inventing one for the walking skeleton would outlive its purpose.
internal static class SmokeRequirement
{
    public const string EnvironmentVariable = "VITALSYNC_REQUIRE_SMOKE";

    public static bool SmokeRequired
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(EnvironmentVariable);
            return !string.IsNullOrWhiteSpace(value)
                && !string.Equals(value, "0", StringComparison.Ordinal)
                && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Fails the test instead of skipping it when smoke coverage is required but the URL is missing.</summary>
    public static void ThrowIfRequired(string urlVariable)
    {
        if (SmokeRequired)
        {
            throw new InvalidOperationException(
                $"'{urlVariable}' is not set and {EnvironmentVariable} is set, so this run must not silently " +
                "skip the smoke tests. Start the Aspire host and point the variable at the API.");
        }
    }
}
