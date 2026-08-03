namespace BuildingBlocks.Application;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventTopicAttribute : Attribute
{
    public IntegrationEventTopicAttribute(string topic)
    {
        Topic = Validate(topic);
    }

    public string Topic { get; }

    private static string Validate(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var segments = topic.Split('.');
        return segments.Length == 2 && Array.TrueForAll(segments, IsKebabCase)
            ? topic
            : throw new ArgumentException(
                $"'{topic}' is not a valid integration event topic. A topic is the published routing key " +
                "in the form '<context>.<event>', both segments lower-case kebab-case " +
                "(for example 'nutrition.recipe-created'), so that consumer bindings such as 'nutrition.*' " +
                "stay stable and independent of the CLR type the attribute happens to be written on.",
                nameof(topic));
    }

    private static bool IsKebabCase(string segment)
    {
        if (segment.Length == 0 || segment[0] == '-' || segment[^1] == '-')
        {
            return false;
        }

        var previousWasHyphen = false;

        foreach (var character in segment)
        {
            if (character == '-')
            {
                if (previousWasHyphen)
                {
                    return false;
                }

                previousWasHyphen = true;
                continue;
            }

            if (!char.IsAsciiLetterLower(character) && !char.IsAsciiDigit(character))
            {
                return false;
            }

            previousWasHyphen = false;
        }

        return true;
    }
}
