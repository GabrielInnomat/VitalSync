namespace GaWeCodes;

internal static class TrimmingMessages
{
    internal const string AssemblyScanning =
        "BuildingBlocks discovers handlers, domain events and aggregates by scanning assemblies at run time. "
        + "Trimming removes types that are reached only this way, so discovery silently finds nothing and the "
        + "first request fails with 'No service for type ICommandHandler<...> has been registered'. "
        + "Publish without PublishTrimmed.";

    internal const string DynamicGenerics =
        "BuildingBlocks builds dispatcher, projection and mapper types with MakeGenericType at run time. "
        + "Native AOT cannot create instantiations it has not seen statically. Publish without PublishAot.";
}
