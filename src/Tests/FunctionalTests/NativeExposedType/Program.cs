using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Data.Json;
using WindowsRuntime.InteropServices;

#pragma warning disable CSWINRT3001 // Type or member is obsolete

// Native code can request a CCW around an RCW to obtain additional managed collection interfaces.
[assembly: WindowsRuntimeNativeExposedType(typeof(JsonArray))]

namespace NativeExposedType;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // XAML modes require an interactive desktop; the default and --rcw-first modes are headless.
        if (args is ["--xaml-only"])
        {
            XamlTests.Run(freshOnly: true);

            return 100;
        }

        if (args is not ([] or ["--rcw-first"] or ["--xaml"]))
        {
            throw new ArgumentException("Supported arguments: --rcw-first, --xaml, --xaml-only.");
        }

        IReadOnlyDictionary<Type, Type> proxyTypeMapping = GetComWrappersProxyTypeMapping();

        // 'JsonArray' was explicitly opted into CCW marshalling code generation, so the interop generator must
        // have generated a proxy for it and registered the associated proxy type map entry.
        if (!proxyTypeMapping.TryGetValue(typeof(JsonArray), out _))
        {
            return 101;
        }

        // 'JsonObject' is a projected type that was not opted into CCW marshalling code generation. The interop
        // generator must have skipped it, as projected types are backed by native objects and never need CCW
        // marshalling code generated for them by default.
        if (proxyTypeMapping.TryGetValue(typeof(JsonObject), out _))
        {
            return 102;
        }

        CollectionTests.Run(rcwFirst: args is ["--rcw-first"]);

        if (args is ["--xaml"])
        {
            XamlTests.Run(freshOnly: false);
        }

        return 100;
    }

    // Retrieves the proxy type map used by CsWinRT to resolve CCW marshalling info for managed objects
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The proxy type map is always preserved by the interop generator.")]
    private static IReadOnlyDictionary<Type, Type> GetComWrappersProxyTypeMapping()
    {
        return TypeMapping.GetOrCreateProxyTypeMapping<WindowsRuntimeComWrappersTypeMapGroup>();
    }
}
