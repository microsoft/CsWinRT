using System.Reflection;

[assembly: global::System.Runtime.Versioning.SupportedOSPlatform("Windows")]

namespace AuthoringTest;

// CsWinRT makes use of the .NET typemap to register all the projected types.
// As part of this, .NET uses TypeMapAssemblyTarget to discover the assemblies with the type map.
// But this needs to be on the launching executable for it to discover them by default or use the
// RuntimeHostConfiguration which isn't available on current builds. Due to this,
// we manually set the entry assembly which allows .NET to discover it.
internal static class ProjectionTypesInitializer
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    internal static void InitializeProjectionTypes()
    {
        Assembly.SetEntryAssembly(typeof(ProjectionTypesInitializer).Assembly);
    }
}

internal class Program
{
    static void Main(string[] args)
    {
    }
}