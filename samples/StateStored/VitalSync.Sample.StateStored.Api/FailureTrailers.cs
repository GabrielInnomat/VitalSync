using System.Globalization;
using BuildingBlocks.Application.Results;
using Grpc.Core;

namespace VitalSync.Sample.StateStored.Api;

internal static class FailureTrailers
{
    public const string CountKey = "failure-count";

    public static string Describe(IReadOnlyList<Failure> failures) =>
        string.Join("; ", failures.Select(static failure => $"{failure.Code}: {failure.Message}"));

    public static Metadata Build(IReadOnlyList<Failure> failures)
    {
        var trailers = new Metadata
        {
            { CountKey, failures.Count.ToString(CultureInfo.InvariantCulture) },
        };

        for (var index = 0; index < failures.Count; index++)
        {
            var failure = failures[index];
            var prefix = string.Create(CultureInfo.InvariantCulture, $"failure-{index}-");

            trailers.Add(prefix + "code", failure.Code);
            trailers.Add(prefix + "message", failure.Message);

            if (failure.Target is not null)
            {
                trailers.Add(prefix + "target", failure.Target);
            }
        }

        return trailers;
    }
}
