namespace VitalSync.Sample.EventSourced.Tests;

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
