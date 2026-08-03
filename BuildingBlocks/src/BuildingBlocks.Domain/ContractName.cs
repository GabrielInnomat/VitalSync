namespace BuildingBlocks.Domain;

internal static class ContractName
{
    public static string Validate(string name, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, parameterName);

        return IsKebabCase(name)
            ? name
            : throw new ArgumentException(
                $"'{name}' is not a valid contract name. A persisted name is lower-case kebab-case " +
                "(letters, digits and single hyphens, for example 'widget-created-v1'), so that it stays " +
                "readable in the database and independent of the CLR type it happens to be written on.",
                parameterName);
    }

    private static bool IsKebabCase(string name)
    {
        if (name[0] == '-' || name[^1] == '-')
        {
            return false;
        }

        var previousWasHyphen = false;

        foreach (var character in name)
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
