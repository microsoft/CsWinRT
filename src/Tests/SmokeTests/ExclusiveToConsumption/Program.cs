using System;
#if EXCLUSIVE_TO_PUBLIC_INTERFACES
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Windows.Data.Json;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
#endif

internal static class Program
{
    [STAThread]
#if EXCLUSIVE_TO_PUBLIC_INTERFACES
    [DynamicDependency(nameof(InvalidateViewport))]
#endif
    private static int Main(string[] args)
    {
#if EXCLUSIVE_TO_PUBLIC_INTERFACES
        bool expectArray = true;
        bool expectObject = true;
        bool expectXaml = true;
        bool runXaml = args is ["--xaml"];

        if (args is ["--expect-idic", string array, string jsonObject, string xaml])
        {
            expectArray = bool.Parse(array);
            expectObject = bool.Parse(jsonObject);
            expectXaml = bool.Parse(xaml);
        }
        else if (args.Length != 0 && !runXaml)
        {
            throw new ArgumentException("Supported arguments: --xaml, or --expect-idic <array> <object> <xaml>.");
        }

        if (typeof(IFrameworkElementProtected7).GUID != new Guid("65aa0480-22e3-5103-ad2a-b626f88ca5ae"))
        {
            return 1;
        }

        if (typeof(IFrameworkElementProtected7).Assembly.GetName().Name != "WinRT.Projection" ||
            typeof(IJsonArray).Assembly.GetName().Name != "WinRT.Projection" ||
            typeof(IJsonObjectWithDefaultValues).Assembly.GetName().Name != "WinRT.Projection" ||
            typeof(FrameworkElement).Assembly.GetName().Name != "WinRT.Sdk.Xaml.Projection" ||
            typeof(JsonArray).Assembly.GetName().Name != "WinRT.Sdk.Projection" ||
            typeof(JsonObject).Assembly.GetName().Name != "WinRT.Sdk.Projection")
        {
            return 2;
        }

        Console.WriteLine(typeof(IFrameworkElementProtected7).FullName);

        object nativeArray = JsonArray.Parse("[42]");
        object nativeObject = JsonObject.Parse("""{ "number": 42 }""");

        // Always test native RCWs, not managed implementations (which bypass the IDIC policy).
        AssertDynamicCast<IJsonArray>(nativeArray, expectArray);
        AssertDynamicCast<IJsonObjectWithDefaultValues>(nativeObject, expectObject);

        if (expectArray && ((IJsonArray)nativeArray).GetNumberAt(0) != 42)
        {
            return 3;
        }

        if (expectObject && ((IJsonObjectWithDefaultValues)nativeObject).GetNamedNumber("missing", 42) != 42)
        {
            return 4;
        }

        // Ordinary interfaces and the owner's ABI helpers do not participate in the exclusive policy.
        AssertDynamicCast<IJsonValue>(nativeArray, expected: true);
        AssertDynamicCast<IJsonValue>(nativeObject, expected: true);

        if (((IJsonValue)nativeArray).Stringify() != "[42]" ||
            ((JsonObject)nativeObject).GetNamedNumber("number") != 42)
        {
            return 5;
        }

        // This is deliberately separate from the negative RCW tests: a direct managed implementation
        // must keep working, and crossing a native PropertySet forces its exclusive CCW vtables to exist.
        ManagedExclusives managed = new();
        AssertDynamicCast<IJsonObjectWithDefaultValues>(managed, expected: true);
        AssertDynamicCast<IJsonValue>(managed, expected: true);
        ((IFrameworkElementProtected7)managed).InvalidateViewport();

        PropertySet properties = new();
        properties["exclusive"] = managed;

        if (!ReferenceEquals(properties["exclusive"], managed) ||
            managed.ViewportInvalidations != 1 ||
            ((IJsonValue)managed).GetNumber() != 42 ||
            ((IJsonObjectWithDefaultValues)managed).GetNamedNumber("missing", 42) != 42)
        {
            return 6;
        }

        properties.Clear();
        Console.WriteLine($"Exclusive IDIC: array={expectArray}, object={expectObject}; ordinary casts and CCW round-trip passed.");

        // The default run is headless. Preserve the real SDK-owner XAML QI/viewport test for desktops.
        if (runXaml)
        {
            RunXamlTest(expectXaml);
        }
#else
        // No public exclusive type can be named here. The runner inspects the regenerated merged
        // assembly to verify that the producer's string metadata restored the selected internal types.
        if (args.Length != 0)
        {
            throw new ArgumentException("The internal-only projection check does not accept arguments.");
        }
#endif

        return 0;
    }

#if EXCLUSIVE_TO_PUBLIC_INTERFACES
    private static void AssertDynamicCast<T>(object instance, bool expected)
        where T : class
    {
        if ((instance is T) != expected || (instance as T is not null) != expected)
        {
            throw new InvalidOperationException($"Unexpected is/as result for {typeof(T)} (expected {expected}).");
        }

        try
        {
            _ = (T)instance;

            if (!expected)
            {
                throw new InvalidOperationException($"An explicit cast to {typeof(T)} unexpectedly succeeded.");
            }
        }
        catch (InvalidCastException) when (!expected)
        {
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InvalidateViewport(FrameworkElement element)
    {
        ((IFrameworkElementProtected7)element).InvalidateViewport();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunXamlTest(bool expected)
    {
        using WindowsXamlManager manager = WindowsXamlManager.InitializeForCurrentThread();
        ScrollContentPresenter element = new();
        AssertDynamicCast<IFrameworkElementProtected7>(element, expected);

        if (expected)
        {
            InvalidateViewport(element);
        }

        Console.WriteLine($"FrameworkElement QI and exclusive cast policy ({expected}) passed.");
    }
#endif
}
