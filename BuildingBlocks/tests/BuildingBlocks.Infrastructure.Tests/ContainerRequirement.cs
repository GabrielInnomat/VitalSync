namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Decides whether a container-backed test may skip when Docker is unavailable, or must fail.
/// </summary>
/// <remarks>
/// Skipping keeps the suite usable on a developer machine without Docker, but it also means a build agent
/// without Docker reports success while whole test classes never ran — the regressions they guard would land
/// unnoticed. Setting <c>VITALSYNC_REQUIRE_CONTAINERS</c> in CI turns a failed container start into a failed
/// test run, so the difference between "passed" and "did not run" stays visible where it matters.
/// </remarks>
public static class ContainerRequirement
{
    public const string EnvironmentVariable = "VITALSYNC_REQUIRE_CONTAINERS";

    /// <summary>
    /// Gets a value indicating whether container-backed tests must run rather than skip.
    /// </summary>
    public static bool ContainersRequired
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(EnvironmentVariable);
            return !string.IsNullOrWhiteSpace(value)
                && !string.Equals(value, "0", StringComparison.Ordinal)
                && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Rethrows the container start-up failure when containers are required, so the run fails instead of skipping.
    /// </summary>
    public static void ThrowIfRequired(string containerName, Exception failure)
    {
        if (ContainersRequired)
        {
            throw new InvalidOperationException(
                $"The {containerName} Testcontainer could not be started and {EnvironmentVariable} is set, " +
                "so this run must not silently skip the tests that depend on it.",
                failure);
        }
    }
}
