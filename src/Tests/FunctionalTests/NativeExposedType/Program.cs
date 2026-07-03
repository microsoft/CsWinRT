using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Data.Json;
using WindowsRuntime.InteropServices;

#pragma warning disable CSWINRT3001 // Type or member is obsolete

// The interop generator normally skips projected types when generating CCW marshalling code, as they are backed
// by native objects and never need to be exposed to native code through a CCW. Opting 'JsonArray' in via the
// '[WindowsRuntimeNativeExposedType]' attribute forces the interop generator to also emit CCW marshalling code
// for it, just like it does for any user defined type. This registers a proxy type map association for the type,
// which the checks below then verify is present (and absent for a projected type that was not opted in).
[assembly: WindowsRuntimeNativeExposedType(typeof(JsonArray))]

namespace NativeExposedType;

internal static class Program
{
    private static int Main()
    {
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

        return 100;
    }

    // Retrieves the proxy type map used by CsWinRT to resolve CCW marshalling info for managed objects
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The proxy type map is always preserved by the interop generator.")]
    private static IReadOnlyDictionary<Type, Type> GetComWrappersProxyTypeMapping()
    {
        return TypeMapping.GetOrCreateProxyTypeMapping<WindowsRuntimeComWrappersTypeMapGroup>();
    }
}
