// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Contoso;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static NativeCollection;

[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows6.3")]
[assembly: DisableRuntimeMarshalling]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Projection")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Sdk.Projection")]

internal static unsafe class Program
{
    private const int E_ACCESSDENIED = unchecked((int)0x80070005);

    public static int Main(string[] args)
    {
        try
        {
#if FALLBACK_ONLY
            Run("list");
            Console.WriteLine("Compile-time-isolated generated fallback passed.");
#else
            if (args.Length != 0)
            {
                Run(args[0]);
            }
            else
            {
                // Keep the full AOT scenario list in sync with the CoreCLR DataRows and Run cases.
                foreach (string scenario in new[]
                {
                    "list", "read-only-list", "strings", "pairs", "dictionary", "read-only-dictionary",
                    "observable-vector", "observable-map", "required-vector", "required-map", "bindable",
                    "first-failure", "concurrent", "negative-cache", "mixed-elements",
                    "generic-adapter", "object-adapter", "bindable-adapter"
                })
                {
                    Run(scenario);
                }
            }
#endif
            for (int i = 0; i < 5 && LiveObjects != 0; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Check(LiveObjects == 0, $"native references leaked: {LiveObjects}");
            Console.WriteLine("Generated non-generic enumeration passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string scenario)
    {
        switch (scenario)
        {
            case "list":
                VerifyCollection(source => source.Items, ElementKind.Int32);
                break;
#if !FALLBACK_ONLY
            case "read-only-list":
                VerifyCollection(source => source.ReadOnlyItems, ElementKind.Int32);
                break;
            case "strings":
                VerifyCollection(source => source.Names, ElementKind.String);
                break;
            case "pairs":
                VerifyCollection(source => source.Pairs, ElementKind.Pair);
                break;
            case "dictionary":
                VerifyCollection(source => source.Dictionary, ElementKind.Pair, map: true);
                break;
            case "read-only-dictionary":
                VerifyCollection(source => source.ReadOnlyDictionary, ElementKind.Pair, map: true);
                break;
            case "observable-vector":
                VerifyCollection(source => source.ObservableItems, ElementKind.Int32);
                break;
            case "observable-map":
                VerifyCollection(source => source.ObservableDictionary, ElementKind.Pair, map: true);
                break;
            case "required-vector":
                VerifyCollection(source => source.RequiredItems, ElementKind.Int32);
                break;
            case "required-map":
                VerifyCollection(source => source.RequiredDictionary, ElementKind.Pair, map: true);
                break;
            case "bindable":
                VerifyCollection(source => source.Items, ElementKind.Int32, bindable: true);
                break;
            case "first-failure":
                VerifyFirstFailure();
                break;
            case "concurrent":
                VerifyConcurrentEnumeration();
                break;
            case "negative-cache":
                VerifyNegativeCache();
                break;
            case "mixed-elements":
                VerifyCollection(source => source.Items, ElementKind.Int32);
                VerifyCollection(source => source.Names, ElementKind.String);
                VerifyCollection(source => source.Items, ElementKind.Int32);
                break;
            case "generic-adapter":
            case "object-adapter":
            case "bindable-adapter":
                VerifyAdapterRoundTrip(scenario);
                break;
#endif
            default:
                throw new ArgumentException($"Unknown scenario: {scenario}");
        }
    }

    // Retain the declared collection type in consumer IL without priming an IEnumerable<T> cast.
    private static void VerifyCollection<TCollection>(Func<CollectionSource, TCollection> getSource, ElementKind element,
        bool map = false, bool bindable = false)
        where TCollection : IEnumerable
    {
        using NativeCollection native = new(element, map, bindable);
        // Each getter's generated return marshaller supplies the collection cast, not a test-side cast.
        IEnumerable source = getSource(native.Source);
        CheckMarshalledCollection(native, source);
        VerifyEnumeration(source, element, bindable);
        VerifyEnumeration(source, element, bindable);
        Check(native.FirstCalls == 2, "expected one First call per enumeration");
        Check(native.IterableQueries == (bindable ? 0 : 1), "typed iterable QI must be lazy, cached, and skipped for bindables");
        Check(native.TrapCalls == 0, "a vector/map pointer was used as an iterable");
    }

#if !FALLBACK_ONLY
    private static void VerifyFirstFailure()
    {
        using NativeCollection native = new();
        IEnumerable source = native.Source.Items;
        CheckMarshalledCollection(native, source);
        native.FirstFailure = E_ACCESSDENIED;
        try
        {
            source.GetEnumerator();
            throw new InvalidOperationException("First failure was swallowed");
        }
        catch (Exception exception) when (exception.HResult == native.FirstFailure)
        {
        }
        Check(native.FirstCalls == 1 && native.IterableQueries == 1 && native.TrapCalls == 0,
            "a failed First must not try another provider");
    }

    private static void VerifyConcurrentEnumeration()
    {
        using NativeCollection native = new();
        IEnumerable source = native.Source.Items;
        CheckMarshalledCollection(native, source);
        Parallel.For(0, 32, _ => VerifyEnumeration(source, ElementKind.Int32));
        Check(native.FirstCalls == 32 && native.TrapCalls == 0, "concurrent native calls");
    }

    private static void VerifyNegativeCache()
    {
        using NativeCollection native = new();
        CollectionSource value = native.Source;
        IEnumerable source = value.AmbiguousItems;
        CheckMarshalledCollection(native, source);
        CheckEnumerationFails(source);
        CheckEnumerationFails(source);
        Check(native.FirstCalls == 0 && native.IterableQueries == 0, "failed lookup must not call First or query a typed iterable");

        IList<int> items = value.Items;
        Check(ReferenceEquals(source, items), "the later collection cast must use the same RCW");
        CheckEnumerationFails(items);
        Check(native.FirstCalls == 0 && native.IterableQueries == 0, "a cached failure must not rescan newly available list context");

        // Typed enumeration still works, but even an exact IEnumerable<int> cast must not undo the cached failure.
        using IEnumerator<int> iterator = items.GetEnumerator();
        Check(iterator.MoveNext() && iterator.Current == 10, "the native typed iterable must remain usable");
        Parallel.For(0, 32, _ => CheckEnumerationFails(source));
        Check(native.FirstCalls == 1 && native.IterableQueries == 1 && native.TrapCalls == 0,
            "non-generic enumeration must retain its cached failure after successful typed enumeration");

        static void CheckEnumerationFails(IEnumerable collection)
        {
            try
            {
                collection.GetEnumerator();
                throw new InvalidOperationException("Non-generic enumeration unexpectedly succeeded");
            }
            catch (ArgumentException)
            {
            }
        }
    }

    private static void VerifyAdapterRoundTrip(string scenario)
    {
        TrackingEnumerator original = scenario switch
        {
            "generic-adapter" => new TrackingEnumerator<int>(10, 20),
            "object-adapter" => new TrackingEnumerator<object>("first", "second"),
            _ => new TrackingEnumerator("first", "second")
        };
        using NativeCollection native = new(bindable: scenario != "generic-adapter", firstIterator: MarshalIterator(original));
        IEnumerable source = native.Source.Items;
        CheckMarshalledCollection(native, source);
        IEnumerator iterator = source.GetEnumerator();
        Check(ReferenceEquals(original, iterator), "adapter round-trip must return the original iterator");
        Check(original.MoveNextCalls == 0, "unwrapping must not advance the iterator");
        Check(iterator.MoveNext() && Equals(iterator.Current, scenario == "generic-adapter" ? (object)10 : "first"), "Current/MoveNext");
        iterator.Reset();
        Check(original.ResetCalls == 1 && iterator.MoveNext(), "Reset must reach the original iterator");
        ((IDisposable)iterator).Dispose();
        Check(original.DisposeCalls == 1, "Dispose must reach the original iterator");
        Check(native.FirstCalls == 1 && native.IterableQueries == (scenario == "generic-adapter" ? 1 : 0) && native.TrapCalls == 0,
            "adapter native calls");
    }

    private static void* MarshalIterator(TrackingEnumerator iterator)
    {
        if (iterator is IEnumerator<int> integerIterator)
        {
            Guid iid = new("bfea7f78-50c2-5f1d-a6ea-9e978d2699ff");
            IEnumeratorAdapter<int> adapter = IEnumeratorAdapter<int>.GetInstance(integerIterator);
            return WindowsRuntimeInterfaceMarshaller<IEnumerator<int>>.ConvertToUnmanaged(adapter, in iid).DetachThisPtrUnsafe();
        }
        if (iterator is IEnumerator<object> objectIterator)
        {
            Guid iid = new("6a1d6c07-076d-49f2-8314-f52c9c9a8331");
            IEnumeratorAdapter<object> adapter = IEnumeratorAdapter<object>.GetInstance(objectIterator);
            return WindowsRuntimeInterfaceMarshaller<IEnumerator>.ConvertToUnmanaged(adapter, in iid).DetachThisPtrUnsafe();
        }

        // Bindable First creates the double adapter; round-trip it through the actual CCW entry point.
        using WindowsRuntimeObjectReferenceValue enumerable = ABI.System.Collections.IEnumerableMarshaller.ConvertToUnmanaged(new ManagedEnumerable(iterator));
        void* pointer = enumerable.GetThisPtrUnsafe();
        void* result;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)pointer)[6])(pointer, &result));
        return result;
    }

    private sealed class ManagedEnumerable(IEnumerator iterator) : IEnumerable
    {
        public IEnumerator GetEnumerator() => iterator;
    }

    [WindowsRuntimeManagedOnlyType]
    private class TrackingEnumerator(params object[] values) : IEnumerator, IDisposable
    {
        private int index = -1;
        public int MoveNextCalls;
        public int ResetCalls;
        public int DisposeCalls;
        public object Current => values[index];

        public bool MoveNext()
        {
            MoveNextCalls++;
            return ++index < values.Length;
        }

        public void Reset()
        {
            ResetCalls++;
            index = -1;
        }

        public void Dispose() => DisposeCalls++;
    }

    private sealed class TrackingEnumerator<T>(T first, T second) : TrackingEnumerator(first, second), IEnumerator<T>
    {
        public new T Current => (T)base.Current;
    }
#endif

    private static void CheckMarshalledCollection(NativeCollection native, IEnumerable value)
    {
        Check(value.GetType() == typeof(CollectionSource), "return marshalling must retain the non-collection runtime-class wrapper");
        Check(native.IterableQueries == 0, "return marshalling queried the generic iterable");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VerifyEnumeration(IEnumerable source, ElementKind element, bool bindable = false)
    {
        IEnumerator iterator = source.GetEnumerator();
        try
        {
            int count = 0;
            while (iterator.MoveNext())
            {
                object expected = bindable ? null : element switch
                {
                    ElementKind.Int32 => (count + 1) * 10,
                    ElementKind.String => "item" + count,
                    ElementKind.Pair => new KeyValuePair<int, string>(count, "item" + count),
                    _ => throw new ArgumentOutOfRangeException(nameof(element))
                };
                Check(Equals(iterator.Current, expected), $"unexpected Current at index {count}");
                count++;
            }
            Check(count == 3, "element count");
        }
        finally
        {
            (iterator as IDisposable)?.Dispose();
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
