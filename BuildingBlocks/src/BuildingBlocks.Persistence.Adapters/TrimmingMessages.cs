namespace BuildingBlocks.Infrastructure;

internal static class TrimmingMessages
{
    internal const string TypedKeyReflection =
        "Typed entity keys are read through reflection over IEntityKey<TValue> and through an expression tree that " +
        "is compiled at run time. Trimming removes the value property that the accessor reads and ahead-of-time " +
        "compilation cannot build the generic accessor at all, so writing or reading an aggregate fails. Publish " +
        "without PublishTrimmed and without PublishAot.";
}
