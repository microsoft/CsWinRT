// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.IO;
using AsmResolver.PE.DotNet.Cil;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_IidLookup
{
    private const string ProjectionSource = """
        using System;
        using System.Collections.Generic;
        using System.Runtime.InteropServices;
        using WindowsRuntime;

        namespace IidProjection
        {
            [WindowsRuntimeType, Guid("69c58f47-a268-4a55-aad8-97d111a2b01a")]
            public interface IExample { }
            [WindowsRuntimeType]
            public class Example { }
            [WindowsRuntimeType]
            public class GenericDefault { }
            [WindowsRuntimeType, Guid("71f0284e-cc83-4c12-95ea-89749cdfc7f5")]
            internal interface IHidden { }
            [WindowsRuntimeType]
            internal class HiddenClass { }
            [WindowsRuntimeType, WindowsRuntimeClassName("Windows.Foundation.IReference`1<IidProjection.Value>")]
            public struct Value { public int Number; public string Text; }
            [WindowsRuntimeType, WindowsRuntimeClassName("Windows.Foundation.IReference`1<IidProjection.Choice>")]
            public enum Choice { First, Second }
            [WindowsRuntimeType, Guid("60a8e0c5-7371-4b96-ab04-b2ccfafce870")]
            public delegate void Handler();
            [WindowsRuntimeType]
            public sealed class MetadataAttribute : Attribute { }
            [WindowsRuntimeType]
            public static class StaticClass { }
            public enum Contract { }
            public static class HiddenQueries
            {
                public static Guid InterfaceIid() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<IHidden>();
                public static Guid ClassIid() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<HiddenClass>();
                public static Guid ExclusiveIid() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<ABI.Impl.IExclusive>();
            }
        }
        namespace ABI
        {
            [WindowsRuntimeMetadata(typeof(global::IidProjection.IExample), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.Example), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.GenericDefault), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.IHidden), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.HiddenClass), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.Value), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.Choice), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.Handler), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.MetadataAttribute), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.StaticClass), "Fixture")]
            [WindowsRuntimeMetadata(typeof(ABI.Impl.IExclusive), "Fixture")]
            internal static class WindowsRuntimeMetadataTypes { }
            [WindowsRuntimeDefaultInterface(typeof(global::IidProjection.Example), typeof(global::IidProjection.IExample))]
            [WindowsRuntimeDefaultInterface(typeof(global::IidProjection.GenericDefault), typeof(IDictionary<string, object>))]
            [WindowsRuntimeDefaultInterface(typeof(global::IidProjection.HiddenClass), typeof(global::IidProjection.IHidden))]
            internal static class WindowsRuntimeDefaultInterfaces { }
        }
        namespace ABI.Impl
        {
            [WindowsRuntimeType, Guid("71f0284e-cc83-4c12-95ea-89749cdfc7f5")]
            internal interface IExclusive { }
        }
        namespace ABI.IidProjection
        {
            using global::WindowsRuntime.InteropServices;
            using global::WindowsRuntime.InteropServices.Marshalling;

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct Value { public int Number; public void* Text; }
            public static unsafe class ValueMarshaller
            {
                private static readonly Guid Iid = new("513dbf61-50a0-5a16-bb0e-96ae18a9f125");
                public static Value ConvertToUnmanaged(global::IidProjection.Value value) =>
                    new() { Number = value.Number, Text = HStringMarshaller.ConvertToUnmanaged(value.Text) };
                public static global::IidProjection.Value ConvertToManaged(Value value) =>
                    new() { Number = value.Number, Text = HStringMarshaller.ConvertToManaged(value.Text) };
                public static void Dispose(Value value) => HStringMarshaller.Free(value.Text);
                public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::IidProjection.Value? value) =>
                    WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, global::System.Runtime.InteropServices.CreateComInterfaceFlags.TrackerSupport, in Iid);
                public static global::IidProjection.Value? UnboxToManaged(void* value)
                {
                    Value? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<Value>(value);
                    return abi.HasValue ? ConvertToManaged(abi.GetValueOrDefault()) : null;
                }
            }
            public static unsafe class IExampleMarshaller
            {
                private static readonly Guid Iid = new("69c58f47-a268-4a55-aad8-97d111a2b01a");
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::IidProjection.IExample value) =>
                    WindowsRuntimeInterfaceMarshaller<global::IidProjection.IExample>.ConvertToUnmanaged(value, in Iid);
                public static global::IidProjection.IExample ConvertToManaged(void* value) =>
                    (global::IidProjection.IExample)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
            }
            public static unsafe class ExampleMarshaller
            {
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::IidProjection.Example value) =>
                    WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value);
                public static global::IidProjection.Example ConvertToManaged(void* value) =>
                    (global::IidProjection.Example)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
            }
            public static unsafe class HandlerMarshaller
            {
                private static readonly Guid Iid = new("60a8e0c5-7371-4b96-ab04-b2ccfafce870");
                public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::IidProjection.Handler value) =>
                    WindowsRuntimeDelegateMarshaller.ConvertToUnmanaged(value, in Iid);
                public static global::IidProjection.Handler ConvertToManaged(void* value) =>
                    (global::IidProjection.Handler)WindowsRuntimeObjectMarshaller.ConvertToManaged(value);
            }
        }
        """;

    [TestMethod]
    public async Task ExactTypes_DoNotImplicitlyBoxValues()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using IidProjection;
            using Windows.Foundation;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    if (WindowsRuntimeMarshal.GetIid<int?>() != new Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4") ||
                        WindowsRuntimeMarshal.GetIid<Value?>() != new Guid("513dbf61-50a0-5a16-bb0e-96ae18a9f125") ||
                        WindowsRuntimeMarshal.GetIid<IList<int>>() != new Guid("b939af5b-b45d-5489-9149-61442c1905fe"))
                        return 1;
                    Reject<int>();
                    Reject<string>();
                    Reject<Guid>();
                    Reject<Point>();
                    Reject<Value>();
                    Reject<Choice>();
                    Reject<Exception>();
                    Reject<ArgumentException>();
                    Reject<Type>();
                    Reject<KeyValuePair<string, int>?>();
                    Reject<sbyte?>();
                    Reject<UserValue?>();
                    return 0;
                }
                static void Reject<T>()
                {
                    try { WindowsRuntimeMarshal.GetIid<T>(); }
                    catch (NotSupportedException) { return; }
                    throw new Exception("Implicit IID mapping for " + typeof(T));
                }
            }
            public struct UserValue { }
            """);
        string output = await runner.GenerateAsync("exact-types");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        string[] keys = GetIidKeys(module);
        Assert.Contains("[#System]System.Nullable`1[[#System]System.Int32]", keys);
        Assert.Contains("[#System]System.Nullable`1[[WinRT.Sdk.Projection]IidProjection.Value]", keys);
        foreach (string type in new[] { "Int32", "String", "Guid", "Exception", "Type" })
        {
            Assert.DoesNotContain("[#System]System." + type, keys);
        }
        Assert.DoesNotContain("[WinRT.Sdk.Projection]IidProjection.Value", keys);
        Assert.DoesNotContain("[WinRT.Sdk.Projection]IidProjection.Choice", keys);
        Assert.IsFalse(keys.Any(key => key.StartsWith(
            "[#System]System.Nullable`1[[#System]System.Collections.Generic.KeyValuePair`2", StringComparison.Ordinal)));
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task IidEntries_AreExternalAndConditionalOnReferencedTypes()
    {
        using InteropGeneratorRunner runner = new();
        string output = await runner.GenerateAsync("unused");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        CustomAttribute[] entries = GetIidEntries(module);

        Assert.IsNotEmpty(entries);
        Assert.Contains("[#System]System.Nullable`1[[#System]System.Int32]", GetIidKeys(module));
        Assert.DoesNotContain("[#System]System.Int32", GetIidKeys(module));
        Assert.IsFalse(module.TopLevelTypes.Any(type => type.Name == "WindowsRuntimeIidData"));

        foreach (CustomAttribute entry in entries)
        {
            Assert.HasCount(3, entry.Signature!.FixedArguments);
            Assert.AreNotEqual(entry.Signature.FixedArguments[1].Element, entry.Signature.FixedArguments[2].Element);
            TypeSignature target = (TypeSignature)entry.Signature.FixedArguments[1].Element!;
            TypeDefinition provider = target.Resolve(module.RuntimeContext);
            Assert.HasCount(1, provider.CustomAttributes);
            Assert.AreEqual("System.Runtime.InteropServices.GuidAttribute", provider.CustomAttributes[0].Constructor!.DeclaringType!.FullName);
            Assert.IsEmpty(provider.Fields);
            Assert.IsEmpty(provider.Methods);
        }
    }

    [TestMethod]
    public async Task IidOnlyGenericCall_UsesSharedGenericDiscovery()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System.Collections.Generic;
            using IidProjection;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main() =>
                    WindowsRuntimeMarshal.GetIid<IList<Value>>() == new System.Guid("41e293d8-754c-516b-9a87-53728753adad") ? 0 : 1;
            }
            """);
        string output = await runner.GenerateAsync("iid-only");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        Assert.Contains("[#System]System.Collections.Generic.IList`1[[WinRT.Sdk.Projection]IidProjection.Value]", GetIidKeys(module));
        Assert.IsTrue(module.TopLevelTypes.Any(type =>
            type.Name!.ToString().Contains("IList'1", StringComparison.Ordinal) &&
            type.Name.ToString().Contains("IidProjection-Value", StringComparison.Ordinal)),
            "Closed method arguments participate in the shared marshalling discovery.");
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task GenericWrappers_ResolveConstructedArguments()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    Guid expected = new("b939af5b-b45d-5489-9149-61442c1905fe");
                    if (Direct<IList<int>>() != expected || BuildList<int>() != expected ||
                        Wrapper<int>.GetList() != expected || Indirect<int>() != expected ||
                        Cached<int>() != expected || Async<int>().GetAwaiter().GetResult() != expected ||
                        System.Linq.Enumerable.Single(Iterator<int>()) != expected)
                        return 1;
                    return 0;
                }
                static Guid Direct<T>() => WindowsRuntimeMarshal.GetIid<T>();
                static Guid BuildList<T>() => WindowsRuntimeMarshal.GetIid<IList<T>>();
                static Guid Indirect<T>() => BuildList<T>();
                static Guid Cached<T>() => Cache<T>.Iid;
                static async System.Threading.Tasks.Task<Guid> Async<T>()
                {
                    await System.Threading.Tasks.Task.Yield();
                    return WindowsRuntimeMarshal.GetIid<IList<T>>();
                }
                static IEnumerable<Guid> Iterator<T>()
                {
                    yield return WindowsRuntimeMarshal.GetIid<IList<T>>();
                }
            }
            public static class Wrapper<T>
            {
                public static Guid GetList() => WindowsRuntimeMarshal.GetIid<IList<T>>();
            }
            public static class Cache<T>
            {
                public static readonly Guid Iid = WindowsRuntimeMarshal.GetIid<IList<T>>();
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("wrappers"));
    }

    [TestMethod]
    public async Task ProjectedTypes_UseInterfaceDefaultAndBoxingIids()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using IidProjection;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    Guid example = new("69c58f47-a268-4a55-aad8-97d111a2b01a");
                    if (WindowsRuntimeMarshal.GetIid<IExample>() != example ||
                        WindowsRuntimeMarshal.GetIid<Example>() != example ||
                        WindowsRuntimeMarshal.GetIid<GenericDefault>() != WindowsRuntimeMarshal.GetIid<IDictionary<string, object>>() ||
                        WindowsRuntimeMarshal.GetIid<Handler>() != new Guid("60a8e0c5-7371-4b96-ab04-b2ccfafce870") ||
                        WindowsRuntimeMarshal.GetIid<Value?>() != new Guid("513dbf61-50a0-5a16-bb0e-96ae18a9f125") ||
                        WindowsRuntimeMarshal.GetIid<Choice?>() == Guid.Empty ||
                        HiddenQueries.InterfaceIid() != new Guid("71f0284e-cc83-4c12-95ea-89749cdfc7f5") ||
                        HiddenQueries.ClassIid() != HiddenQueries.InterfaceIid() ||
                        HiddenQueries.ExclusiveIid() != HiddenQueries.InterfaceIid())
                        return 1;
                    Reject<MetadataAttribute>();
                    Reject<Contract>();
                    Reject<Value>();
                    Reject<Choice>();
                    return 0;
                }
                static void Reject<T>()
                {
                    try { WindowsRuntimeMarshal.GetIid<T>(); }
                    catch (NotSupportedException) { return; }
                    throw new Exception("Unexpected IID for " + typeof(T));
                }
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("projected"));
    }

    [TestMethod]
    public async Task BuiltInTypes_MatchRuntimeIidConstants()
    {
        using InteropGeneratorRunner runner = new(applicationSource: ExpandRuntimeIids("""
            using System;
            using System.Collections.Generic;
            using System.Numerics;
            using Windows.Foundation;
            using Windows.Foundation.Collections;
            using WindowsRuntime.InteropServices;
            using A = System.Collections.Generic.IEnumerable<Windows.Foundation.IStringable>;
            using B = System.Collections.Generic.KeyValuePair<string, Windows.Foundation.IAsyncOperationWithProgress<System.Collections.Generic.IEnumerable<Windows.Foundation.IStringable>, float>>;
            public static class Program
            {
                public static int Main()
                {
                    Check<bool?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfBool);
                    Check<byte?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfByte);
                    Check<char?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfChar);
                    Check<short?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfShort);
                    Check<ushort?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfUShort);
                    Check<int?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfInt);
                    Check<uint?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfUInt);
                    Check<long?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfLong);
                    Check<ulong?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfULong);
                    Check<float?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfFloat);
                    Check<double?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfDouble);
                    Check<Guid?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfGuid);
                    Check<DateTimeOffset?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfDateTimeOffset);
                    Check<TimeSpan?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfTimeSpan);
                    Check<Point?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfPoint);
                    Check<Rect?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfRect);
                    Check<Size?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfSize);
                    Check<Vector3?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfVector3);
                    Check<Matrix3x2?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfMatrix3x2);
                    Check<Matrix4x4?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfMatrix4x4);
                    Check<Plane?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfPlane);
                    Check<Quaternion?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfQuaternion);
                    Check<Vector2?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfVector2);
                    Check<Vector4?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfVector4);
                    Check<EventRegistrationToken?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfEventRegistrationToken);
                    Check<PropertyType?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfPropertyType);
                    Check<AsyncStatus?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfAsyncStatus);
                    Check<CollectionChange?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfCollectionChange);
                    Check<object>(WellKnownWindowsInterfaceIIDs.IID_IInspectable);
                    Check<IDisposable>(new("30d5a829-7fa4-4026-83bb-d75bae4ea99e"));
                    Check<IStringable>(new("96369f54-8eb6-48f0-abce-c1b211e627c3"));
                    Check<Uri>(new("9e365e57-48b2-4160-956f-c7385120bbfc"));
                    Check<int?>(WellKnownWindowsInterfaceIIDs.IID_IReferenceOfInt);
                    Check<int[]>(WellKnownWindowsInterfaceIIDs.IID_IReferenceArrayOfInt32);
                    Check<IList<int>>(new("b939af5b-b45d-5489-9149-61442c1905fe"));
                    Check<IAsyncOperation<int>>(new("968b9665-06ed-5774-8f53-8edeabd5f7b5"));
                    Check<IAsyncActionWithProgress<A>>(new("dd725452-2da3-5103-9c7d-22ee9bb14ad3"));
                    Check<IAsyncOperationWithProgress<A, B>>(new("94645425-b9e5-5b91-b509-8da4df6a8916"));
                    Check<AsyncActionProgressHandler<A>>(new("c261d8d0-71ba-5f38-a239-872342253a18"));
                    Check<AsyncActionWithProgressCompletedHandler<A>>(new("9a0d211c-0374-5d23-9e15-eaa3570fae63"));
                    Check<AsyncOperationCompletedHandler<A>>(new("9d534225-231f-55e7-a6d0-6c938e2d9160"));
                    Check<AsyncOperationProgressHandler<A, B>>(new("264f1e0c-abe4-590b-9d37-e1cc118ecc75"));
                    Check<AsyncOperationWithProgressCompletedHandler<A, B>>(new("c2d078d8-ac47-55ab-83e8-123b2be5bc5a"));
                    Check<EventHandler<A>>(new("fa0b7d80-7efa-52df-9b69-0574ce57ada4"));
                    Check<EventHandler<A, B>>(new("edb31843-b4cf-56eb-925a-d4d0ce97a08d"));
                    Check<IEnumerable<A>>(new("96565eb9-a692-59c8-bcb5-647cde4e6c4d"));
                    Check<IEnumerator<A>>(new("3c9b1e27-8357-590b-8828-6e917f172390"));
                    Check<KeyValuePair<A, B>>(new("89336cd9-8b66-50a7-9759-eb88ccb2e1fe"));
                    Check<IReadOnlyDictionary<A, B>>(new("b78f0653-fa89-59cf-ba95-726938aae666"));
                    Check<IDictionary<A, B>>(new("9962cd50-09d5-5c46-b1e1-3c679c1c8fae"));
                    Check<IReadOnlyList<A>>(new("5f07498b-8e14-556e-9d2e-2e98d5615da9"));
                    Check<IList<A>>(new("0e3f106f-a266-50a1-8043-c90fcf3844f6"));
                    Check<IMapChangedEventArgs<A>>(new("e1aa5138-12bd-51a1-8558-698dfd070abe"));
                    Check<IObservableMap<A, B>>(new("75f99e2a-137e-537e-a5b1-0b5a6245fc02"));
                    Check<IObservableVector<A>>(new("d24c289f-2341-5128-aaa1-292dd0dc1950"));
                    Check<MapChangedEventHandler<A, B>>(new("19046f0b-cf81-5dec-bbb2-7cc250da8b8b"));
                    Check<VectorChangedEventHandler<A>>(new("a1e9acd7-e4df-5a79-aefa-de07934ab0fb"));
                    return 0;
                }
                static void Check<T>(Guid expected)
                {
                    Guid actual = WindowsRuntimeMarshal.GetIid<T>();
                    if (actual != expected) throw new Exception(typeof(T) + ": " + actual + " != " + expected);
                }
            }
            """));
        await runner.RunApplicationAsync(await runner.GenerateAsync("built-in"));
    }

    [TestMethod]
    public async Task InvalidTypes_ThrowWithoutFailingGeneration()
    {
        using InteropGeneratorRunner runner = new(applicationSource: ExpandRuntimeIids("""
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using Windows.Foundation;
            using Windows.Foundation.Metadata;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    Reject<UserClass>();
                    Reject<UserInterface>();
                    Reject<IList<UserClass>>();
                    Reject<ArgumentException[]>();
                    Reject<IList<ArgumentException>>();
                    Reject<EventHandler>();
                    Reject<sbyte>();
                    Reject<nint>();
                    Reject<KeyValuePair<string, int>?>();
                    RejectConstructed(typeof(IList<>).MakeGenericType(typeof(KeyValuePair<string, int>?)));
                    RejectConstructed(typeof(IAsyncOperation<>).MakeGenericType(typeof(KeyValuePair<string, int>?)));
                    Reject<ICollection<KeyValuePair<string, int>>>();
                    if (WindowsRuntimeMarshal.GetIid<int[]>() != WellKnownWindowsInterfaceIIDs.IID_IReferenceArrayOfInt32)
                        throw new Exception("Missing supported vector IID");
                    Reject<int[,]>();
                    Reject<int[][]>();
                    RejectConstructed(typeof(int).MakeArrayType(1));
                    RejectConstructed(typeof(int).MakeArrayType(3));
                    RejectConstructed(typeof(int[]).MakeArrayType());
                    Reject<ApiContractAttribute>();
                    return 0;
                }
                static void Reject<T>()
                {
                    for (int i = 0; i < 2; i++)
                    {
                        try { WindowsRuntimeMarshal.GetIid<T>(); }
                        catch (NotSupportedException) { continue; }
                        throw new Exception("Unexpected IID for " + typeof(T));
                    }
                }
                static void RejectConstructed(Type type)
                {
                    var method = typeof(Program).GetMethod("Reject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    method.MakeGenericMethod(type).Invoke(null, null);
                }
            }
            public class UserClass : IStringable { public string ToString() => ""; }
            [Guid("69c58f47-a268-4a55-aad8-97d111a2b01a")]
            public interface UserInterface { }
            """));
        await runner.RunApplicationAsync(await runner.GenerateAsync("invalid"));
    }

    [TestMethod]
    public async Task UsedApi_IsDeterministicAcrossInputOrderAndParallelism()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            public static class Program
            {
                public static void Main()
                {
                    WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<int?>();
                    WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<IidProjection.Example>();
                }
            }
            """);
        byte[] expected = File.ReadAllBytes(await runner.GenerateAsync("serial"));
        byte[] actual = File.ReadAllBytes(await runner.GenerateAsync("parallel", reverseInputs: true, parallelism: -1));
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task MicrosoftSignedLibrary_UsesPrecomputedGenericMetadata()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, publicSignIidLibrary: true,
            iidLibrarySource: """
                public static class LibraryQueries
                {
                    public static System.Guid ListIid<T>() =>
                        WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<T>>();
                }
                """,
            applicationSource: """
                public static class Program
                {
                    public static int Main() => LibraryQueries.ListIid<IidProjection.Value>() ==
                        new System.Guid("41e293d8-754c-516b-9a87-53728753adad") ? 0 : 1;
                }
                """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("signed-caller", marshallingMode: "Strict"));
    }

    [TestMethod]
    public async Task MarshallingOptOut_DoesNotDisableIidLookups()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource,
            iidLibrarySource: """
                public static class UnrelatedTypes
                {
                    public static System.Collections.Generic.IList<IidProjection.Value> Values;
                    public static object GetValues() => Values;
                }
                """,
            applicationSource: """
                public static class Program
                {
                    public static int Main() =>
                        WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<IidProjection.Value>>() ==
                        new System.Guid("41e293d8-754c-516b-9a87-53728753adad") ? 0 : 1;
                }
                """);
        string output = await runner.GenerateAsync("opted-out", marshallingMode: "Strict", marshallingDisabledAssemblyNames: ["IidLibrary"]);
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        Assert.Contains("[#System]System.Collections.Generic.IList`1[[WinRT.Sdk.Projection]IidProjection.Value]",
            GetIidKeys(module));
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task ForwardedIdentities_HaveOneExternalIidEntryPerKey()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            public static class Program
            {
                public static int Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<int?>() ==
                    new System.Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4") ? 0 : 1;
            }
            """);
        string output = await runner.GenerateAsync("duplicate");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        string[] keys = GetIidEntries(module).Select(entry => entry.Signature!.FixedArguments[0].Element!.ToString()!).ToArray();
        Assert.AreEqual(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());

        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task IidOnlyTypes_SurviveTrimmingAndNativeAot()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using IidProjection;
            using Windows.Foundation;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    if (WindowsRuntimeMarshal.GetIid<IExample>() != new Guid("69c58f47-a268-4a55-aad8-97d111a2b01a") ||
                        WindowsRuntimeMarshal.GetIid<Example>() != WindowsRuntimeMarshal.GetIid<IExample>() ||
                        WindowsRuntimeMarshal.GetIid<GenericDefault>() != WindowsRuntimeMarshal.GetIid<IDictionary<string, object>>() ||
                        WindowsRuntimeMarshal.GetIid<Point?>() != new Guid("84f14c22-a00a-5272-8d3d-82112e66df00") ||
                        WindowsRuntimeMarshal.GetIid<IList<Value>>() != new Guid("41e293d8-754c-516b-9a87-53728753adad") ||
                        WindowsRuntimeMarshal.GetIid<IList<Uri>>() != new Guid("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e") ||
                        WindowsRuntimeMarshal.GetIid<IList<System.ComponentModel.PropertyChangedEventArgs>>() != new Guid("3d1941dd-0165-5e8e-a121-1130d764de6e") ||
                        WindowsRuntimeMarshal.GetIid<KeyValuePair<string, int>>() == WindowsRuntimeMarshal.GetIid<int?>() ||
                        WindowsRuntimeMarshal.GetIid<Value?>() != new Guid("513dbf61-50a0-5a16-bb0e-96ae18a9f125"))
                        return 1;
                    return 0;
                }
            }
            """);
        string output = await runner.GenerateAsync("publish");
        await runner.RunApplicationAsync(output);
        if (Environment.Is64BitProcess)
        {
            await runner.PublishApplicationAsync(output, nativeAot: true);
        }
        await runner.PublishApplicationAsync(output, nativeAot: false, isolateUnsupportedTrimTargets: true);
    }

    [TestMethod]
    public async Task UnusedLookup_IsRemovedByTrimming()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            public static class Program
            {
                public static int Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.NativeReferenceEquals(null, null) ? 0 : 1;
            }
            """);
        string output = await runner.GenerateAsync("unused-publish");
        await runner.RunApplicationAsync(output);
        string published = await runner.PublishApplicationAsync(output, nativeAot: false, isolateUnsupportedTrimTargets: true);
        ModuleDefinition runtime = ModuleDefinition.FromFile(Path.Combine(published, "WinRT.Runtime.dll"));

        Assert.IsFalse(runtime.GetAllTypes().Any(type => type.Name?.ToString() is "WindowsRuntimeIidInfo" or "WindowsRuntimeIidTypeMapGroup"));
        Assert.IsFalse(runtime.GetAllTypes().SelectMany(type => type.Methods).Any(method => method.Name == "GetIid"));
        string interopPath = Path.Combine(published, "WinRT.Interop.dll");
        if (File.Exists(interopPath))
        {
            ModuleDefinition interop = ModuleDefinition.FromFile(interopPath);
            Assert.IsEmpty(GetIidEntries(interop));
            Assert.IsFalse(interop.GetAllTypes().Any(type => type.Name!.ToString().EndsWith("IidProvider", StringComparison.Ordinal)));
        }
    }

    [TestMethod]
    public async Task UnusedLookup_IsRemovedByNativeAot()
    {
        if (!Environment.Is64BitProcess)
        {
            return;
        }
        using InteropGeneratorRunner runner = new(applicationSource: """
            public static class Program
            {
                public static int Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.NativeReferenceEquals(null, null) ? 0 : 1;
            }
            """);
        string output = await runner.GenerateAsync("unused-aot");
        await runner.RunApplicationAsync(output);
        string published = await runner.PublishApplicationAsync(output, nativeAot: true);
        string projectDirectory = Path.GetDirectoryName(published)!;
        string[] maps = Directory.GetFiles(projectDirectory, "*.map.xml", SearchOption.AllDirectories);
        Assert.IsNotEmpty(maps, "Native AOT must produce its dependency map.");
        foreach (string map in maps)
        {
            string contents = File.ReadAllText(map);
            Assert.DoesNotContain("WindowsRuntimeIidInfo", contents);
            Assert.DoesNotContain("WindowsRuntimeIidTypeMapGroup", contents);
            Assert.DoesNotContain("IidProvider", contents);
        }
    }

    [TestMethod]
    public async Task KnownIssue_IlLinkRejectsExistingArrayTypeMapDependencies()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            public static class Program { public static int Main() => 0; }
            """);
        string output = await runner.GenerateAsync("array-trimmer-issue");
        await runner.RunApplicationAsync(output);
        await runner.PublishApplicationAsync(output, nativeAot: false,
            expectedPublishFailure: "TypeDefinition cannot be resolved from 'Mono.Cecil.ArrayType' type");
    }

    [TestMethod]
    [DataRow("IidLibrary", false, "IidLibrary")]
    [DataRow("#System", false, "##System")]
    [DataRow("##System", true, "###System")]
    public async Task SystemNamedImpostors_DoNotMatchLogicalScope(string assemblyName, bool signed, string expectedScope)
    {
        using InteropGeneratorRunner runner = new(
            iidLibraryName: assemblyName,
            publicSignIidLibrary: signed,
            iidLibrarySource: $$"""
                namespace System
                {
                    internal sealed class Uri;
                    internal interface IServiceProvider;
                    [WindowsRuntime.WindowsRuntimeType]
                    internal enum ContractValue { First }
                }
                public static class LibraryQueries
                {
                    public static bool Reject() =>
                        Reject<System.Uri>() &&
                        Reject<System.IServiceProvider>() &&
                        Reject<System.Collections.Generic.IList<System.Uri>>();
                    public static bool CheckProjectedType()
                    {
                        var map = System.Runtime.InteropServices.TypeMapping.GetOrCreateExternalTypeMapping<
                            WindowsRuntime.InteropServices.WindowsRuntimeIidTypeMapGroup>();
                        return map.TryGetValue("[#System]System.Collections.Generic.IList`1[[{{expectedScope}}]System.ContractValue]", out System.Type provider) &&
                            WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<System.ContractValue>>() == provider.GUID;
                    }
                    static bool Reject<T>()
                    {
                        try { WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<T>(); }
                        catch (System.NotSupportedException) { return true; }
                        return false;
                    }
                }
                """,
            applicationSource: """
                public static class Program
                {
                    public static int Main() =>
                        LibraryQueries.Reject() &&
                        LibraryQueries.CheckProjectedType() &&
                        WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.Uri>() ==
                        new System.Guid("9e365e57-48b2-4160-956f-c7385120bbfc") &&
                        WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.IServiceProvider>() ==
                        new System.Guid("68b3a2df-8173-539f-b524-c8a2348f5afb") ? 0 : 1;
                }
                """);
        string output = await runner.GenerateAsync("system-impostors");
        await runner.RunApplicationAsync(output);

        if (assemblyName == "#System" && Environment.Is64BitProcess)
        {
            await runner.PublishApplicationAsync(output, nativeAot: true);
        }
    }

    [TestMethod]
    public async Task MovedFrameworkType_UsesLogicalScopeThroughItsFacade()
    {
        using InteropGeneratorRunner runner = new(
            forwardUriToFutureAssembly: true,
            applicationSource: """
                public static class Program
                {
                    public static int Main() =>
                        WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.Uri>() ==
                        new System.Guid("9e365e57-48b2-4160-956f-c7385120bbfc") &&
                        WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.Collections.Generic.IList<System.Uri>>() ==
                        new System.Guid("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e") ? 0 : 1;
                }
                """);

        // A direct reference to the moved definition must match the well-known reference through its forwarder
        runner.RewriteApplicationTypeScope("System", "Uri", "FutureFramework");
        string output = await runner.GenerateAsync("moved-framework");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        CustomAttribute[] entries = GetIidEntries(module);

        foreach ((string key, string expected) in new[]
        {
            ("[#System]System.Uri", "9e365e57-48b2-4160-956f-c7385120bbfc"),
            ("[#System]System.Collections.Generic.IList`1[[#System]System.Uri]", "0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e")
        })
        {
            CustomAttribute entry = entries.Single(entry => entry.Signature!.FixedArguments[0].Element!.ToString() == key);
            TypeDefinition provider = ((TypeSignature)entry.Signature!.FixedArguments[1].Element!).Resolve(module.RuntimeContext);
            Assert.AreEqual(new Guid(expected), Guid.Parse(provider.CustomAttributes.Single().Signature!.FixedArguments[0].Element!.ToString()!));
        }

        Assert.IsFalse(GetIidKeys(module).Any(key => key.Contains("[FutureFramework]", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task OverlappingFrameworkReferences_HaveOneIidProviderPerIdentity(bool implementations)
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: implementations, overlappingFrameworkReferences: true);
        string output = await runner.GenerateAsync("overlap");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        string[] keys = GetIidEntries(module).Select(entry => entry.Signature!.FixedArguments[0].Element!.ToString()!).ToArray();
        Assert.AreEqual(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    public async Task MappedXamlTypes_FollowProjectionMode()
    {
        foreach (bool useWux in new[] { false, true })
        {
            string interfaceIid = useWux ? "cf75d69c-f2f4-486b-b302-bb4c09baebfa" : "90b17601-b065-586e-83d9-9adc3a695284";
            string classIid = useWux ? "4f33a9a0-5cf4-47a4-b16f-d7faaf17457e" : "63d0c952-396b-54f4-af8c-ba8724a427bf";
            using InteropGeneratorRunner runner = new(applicationSource: $$"""
                using System;
                using System.ComponentModel;
                using WindowsRuntime.InteropServices;
                public static class Program
                {
                    public static int Main() =>
                        WindowsRuntimeMarshal.GetIid<INotifyPropertyChanged>() == new Guid("{{interfaceIid}}") &&
                        WindowsRuntimeMarshal.GetIid<PropertyChangedEventArgs>() == new Guid("{{classIid}}") ? 0 : 1;
                }
                """);
            await runner.RunApplicationAsync(await runner.GenerateAsync("xaml", useWindowsUIXamlProjections: useWux));
        }
    }

    [TestMethod]
    public async Task ReturnedIids_QueryTheActualNativeInterfaces()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using WindowsRuntime.InteropServices;
            [assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime")]
            [assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
            public static unsafe class Program
            {
                public static int Main()
                {
                    Check<IList<int>>(new List<int>());
                    Check<int?>(42);
                    Check<int[]>(new[] { 1, 2, 3 });
                    Check<KeyValuePair<string, int>>(new KeyValuePair<string, int>("key", 42));
                    Check<EventHandler<int>>(new EventHandler<int>((sender, value) => { }));
                    return 0;
                }
                static void Check<T>(object value)
                {
                    void* unknown = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
                    try
                    {
                        Guid iid = WindowsRuntimeMarshal.GetIid<T>();
                        void* result = null;
                        var queryInterface = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)(*(void***)unknown)[0];
                        Marshal.ThrowExceptionForHR(queryInterface(unknown, &iid, &result));
                        WindowsRuntimeMarshal.Free(result);
                    }
                    finally { WindowsRuntimeMarshal.Free(unknown); }
                }
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("query-interface"));
    }

    [TestMethod]
    public async Task AuthoredTypes_UseTheComponentMetadata()
    {
        using InteropGeneratorRunner runner = new(
            applicationSource: """
                using System;
                using System.Runtime.InteropServices;
                using WindowsRuntime.InteropServices;
                [assembly: WindowsRuntimeComponentAssembly]
                public static class Program
                {
                    public static int Main()
                    {
                        if (WindowsRuntimeMarshal.GetIid<Component.Class>() != WindowsRuntimeMarshal.GetIid<Component.IExample>() ||
                            WindowsRuntimeMarshal.GetIid<Component.Value?>() == Guid.Empty ||
                            WindowsRuntimeMarshal.GetIid<Component.Choice?>() == Guid.Empty ||
                            WindowsRuntimeMarshal.GetIid<Component.Handler>() != new Guid("60a8e0c5-7371-4b96-ab04-b2ccfafce870"))
                            return 1;
                        try { WindowsRuntimeMarshal.GetIid<Component.Helper>(); }
                        catch (NotSupportedException) { return 0; }
                        return 2;
                    }
                }
                namespace Component
                {
                    [Guid("69c58f47-a268-4a55-aad8-97d111a2b01a")]
                    public interface IExample { }
                    public class Class { }
                    public struct Value { public int Number; }
                    public enum Choice { First, Second }
                    [Guid("60a8e0c5-7371-4b96-ab04-b2ccfafce870")]
                    public delegate void Handler();
                    public class Helper { }
                }
                """,
            componentProjectionSource: """
                using WindowsRuntime;
                namespace ABI
                {
                    [WindowsRuntimeMetadata(typeof(Component.Class), "Component")]
                    [WindowsRuntimeMetadata(typeof(Component.IExample), "Component")]
                    [WindowsRuntimeMetadata(typeof(Component.Value), "Component")]
                    [WindowsRuntimeMetadata(typeof(Component.Choice), "Component")]
                    [WindowsRuntimeMetadata(typeof(Component.Handler), "Component")]
                    internal static class WindowsRuntimeMetadataTypes { }
                    [WindowsRuntimeDefaultInterface(typeof(Component.Class), typeof(Component.IExample))]
                    internal static class WindowsRuntimeDefaultInterfaces { }
                }
                """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("authored"));
    }

    [TestMethod]
    public async Task AcceptedInputs_FollowTypeNameMappings()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using System.Runtime.InteropServices;
            using IidProjection;
            using Windows.UI.Xaml.Interop;
            using WindowsRuntime.InteropServices;
            using WindowsRuntime.InteropServices.Marshalling;
            [assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Runtime")]
            [assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Interop")]
            [assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime")]
            [assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
            public static unsafe class Program
            {
                public static int Main()
                {
                    Check<int>(false, "Int32");
                    Check<int?>(true, "Windows.Foundation.IReference`1<Int32>");
                    Check<string>(false, "String");
                    Check<Uri>(true, "Windows.Foundation.Uri");
                    Check<Type>(false, "Windows.UI.Xaml.Interop.TypeName");
                    Check<Exception>(false, "Windows.Foundation.HResult");
                    Check<ArgumentException>(false, "Windows.Foundation.HResult");
                    Check<ArgumentException[]>(false, expectedMetadata: false);
                    Check<IList<ArgumentException>>(false, expectedMetadata: false);
                    Check<IExample>(true);
                    Check<Example>(true);
                    Check<Value>(false, "IidProjection.Value");
                    Check<Value?>(true, "Windows.Foundation.IReference`1<IidProjection.Value>");
                    Check<Choice>(false, "IidProjection.Choice");
                    Check<Choice?>(true);
                    Check<Handler>(true);
                    Check<IList<int>>(true);
                    Check<EventHandler<int>>(true);
                    Check<KeyValuePair<string, int>>(true);
                    Check<int[]>(true);
                    Check<EventHandler>(false, expectedMetadata: false);
                    Check<KeyValuePair<string, int>?>(false, expectedMetadata: false);
                    Check<sbyte?>(false, expectedMetadata: false);
                    Check<UserValue?>(false, expectedMetadata: false);
                    Check<UserClass>(false, expectedMetadata: false);
                    Check<IList<UserClass>>(false, expectedMetadata: false);
                    return 0;
                }
                static void Check<T>(bool expectedIid, string expectedName = null, bool expectedMetadata = true)
                {
                    var name = ABI.System.TypeMarshaller.ConvertToUnmanaged(typeof(T));
                    try
                    {
                        if ((name.Kind != TypeKind.Custom) != expectedMetadata ||
                            (expectedIid && name.Kind != TypeKind.Metadata) ||
                            (expectedName != null && HStringMarshaller.ConvertToManaged(name.Name) != expectedName))
                            throw new Exception("Unexpected TypeName for " + typeof(T));
                    }
                    finally { ABI.System.TypeMarshaller.Dispose(name); }
                    bool hasIid;
                    try { WindowsRuntimeMarshal.GetIid<T>(); hasIid = true; }
                    catch (NotSupportedException) { hasIid = false; }
                    if (expectedIid != hasIid) throw new Exception("Unexpected IID availability for " + typeof(T));
                }
            }
            public sealed class UserClass { }
            public struct UserValue { }
            public static class TypeNameInputs
            {
                public static EventHandler<int> Handler;
                public static KeyValuePair<string, int> Pair;
                public static int[] Array;
                public static IList<int> List;
                public static IList<UserClass> CustomList;
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("type-name"));
    }

    [TestMethod]
    public async Task MethodGroups_UseTheExternalIidMap()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using IidProjection;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    Func<Guid> callback = WindowsRuntimeMarshal.GetIid<IList<Value>>;
                    return callback() == new Guid("41e293d8-754c-516b-9a87-53728753adad") ? 0 : 1;
                }
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("method-group"));
    }

    [TestMethod]
    public async Task GenericInterfaceDispatch_UsesPrecomputedClosedTypes()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using IidProjection;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    IQueries query = GetQueries(Environment.TickCount);
                    if (query.Get<Example>() != new Guid("69c58f47-a268-4a55-aad8-97d111a2b01a") ||
                        query.Get<IList<IExample>>() != new Guid("1156320c-ed56-5d57-81e4-014da6ca8875") ||
                        query.Get<IList<Example>>() != new Guid("59bf76d5-ee4c-5b7e-914c-1d014338abd4") ||
                        query.Get<IList<Value>>() != new Guid("41e293d8-754c-516b-9a87-53728753adad") ||
                        query.Get<IList<Choice>>() != new Guid("e31d0ee0-9029-5378-a57d-491571b40774") ||
                        query.Get<IList<Handler>>() != new Guid("9ba12a6c-1865-5404-bcfe-2ed1734b9c4c") ||
                        query.Get<IList<IReadOnlyList<Example>>>() != new Guid("df849283-4e99-50e5-b70f-887fe29cb735") ||
                        query.Get<KeyValuePair<string, Value>>() != new Guid("9d6f0563-e476-576e-85c5-3b1409819162") ||
                        query.Get<Value?>() != new Guid("513dbf61-50a0-5a16-bb0e-96ae18a9f125") ||
                        query.Get<Value[]>() != new Guid("66e75cea-a277-5f41-acc6-f34791d45ffa"))
                        return 1;
                    return query.List<Uri>() == new Guid("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e") ? 0 : 2;
                }
                [MethodImpl(MethodImplOptions.NoInlining)]
                static IQueries GetQueries(int seed) => seed % 2 == 0 ? new Queries() : new OtherQueries();
            }
            public interface IQueries
            {
                Guid Get<T>();
                Guid List<T>();
                Guid Nested<T>();
                Guid Pair<T>();
                Guid Nullable<T>() where T : struct;
                Guid Array<T>();
            }
            public class Queries : IQueries
            {
                public Guid Get<T>() => WindowsRuntimeMarshal.GetIid<T>();
                public Guid List<T>() => WindowsRuntimeMarshal.GetIid<IList<T>>();
                public Guid Nested<T>() => WindowsRuntimeMarshal.GetIid<IList<IReadOnlyList<T>>>();
                public Guid Pair<T>() => WindowsRuntimeMarshal.GetIid<KeyValuePair<string, T>>();
                public Guid Nullable<T>() where T : struct => WindowsRuntimeMarshal.GetIid<T?>();
                public Guid Array<T>() => WindowsRuntimeMarshal.GetIid<T[]>();
            }
            public sealed class OtherQueries : Queries { }
            """);
        string output = await runner.GenerateAsync("generic-dispatch");
        await runner.RunApplicationAsync(output);
        if (Environment.Is64BitProcess)
        {
            await runner.PublishApplicationAsync(output, nativeAot: true);
        }
        await runner.PublishApplicationAsync(output, nativeAot: false, isolateUnsupportedTrimTargets: true);
    }

    [TestMethod]
    [DataRow("interface")]
    [DataRow("explicit-interface")]
    [DataRow("abstract")]
    [DataRow("virtual")]
    [DataRow("constrained")]
    [DataRow("method-group")]
    [DataRow("default-interface")]
    [DataRow("static-abstract")]
    public async Task DispatchedConstruction_UsesExistingDiscoveryState(string scenario)
    {
        const string instanceInterface = """
            public interface IQueries { Guid Get<T>(); }
            public sealed class Queries : IQueries
            {
                public Guid Get<T>() => WindowsRuntimeMarshal.GetIid<IList<T>>();
            }
            """;
        (string declarations, string invocation, string helper) = scenario switch
        {
            "interface" => (instanceInterface, "((IQueries)new Queries()).Get<InputStreamOptions>()", ""),
            "explicit-interface" => (instanceInterface.Replace("public Guid Get<T>()", "Guid IQueries.Get<T>()"),
                "((IQueries)new Queries()).Get<InputStreamOptions>()", ""),
            "abstract" => ("""
                public abstract class Base { public abstract Guid Get<T>(); }
                public sealed class Queries : Base
                {
                    public override Guid Get<T>() => WindowsRuntimeMarshal.GetIid<IList<T>>();
                }
                """, "((Base)new Queries()).Get<InputStreamOptions>()", ""),
            "virtual" => ("""
                public class Base { public virtual Guid Get<T>() => Guid.Empty; }
                public sealed class Queries : Base
                {
                    public override Guid Get<T>() => WindowsRuntimeMarshal.GetIid<IList<T>>();
                }
                """, "new Queries().Get<InputStreamOptions>()", ""),
            "constrained" => (instanceInterface, "Call<Queries, InputStreamOptions>(new Queries())",
                "static Guid Call<TQuery, T>(TQuery query) where TQuery : IQueries => query.Get<T>();"),
            "method-group" => (instanceInterface,
                "new Func<Guid>(((IQueries)new Queries()).Get<InputStreamOptions>)()", ""),
            "default-interface" => ("""
                public interface IQueries<T>
                {
                    Guid Get() => WindowsRuntimeMarshal.GetIid<IList<T>>();
                }
                public sealed class Queries<T> : IQueries<T> { }
                """, "((IQueries<InputStreamOptions>)new Queries<InputStreamOptions>()).Get()", ""),
            "static-abstract" => ("""
                public interface IQueries { static abstract Guid Get<T>(); }
                public sealed class Queries : IQueries
                {
                    public static Guid Get<T>() => WindowsRuntimeMarshal.GetIid<IList<T>>();
                }
                """, "Call<Queries, InputStreamOptions>()",
                "static Guid Call<TQuery, T>() where TQuery : IQueries => TQuery.Get<T>();"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        using InteropGeneratorRunner runner = new(applicationSource: $$"""
            using System;
            using System.Collections.Generic;
            using Windows.Storage.Streams;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main() => {{invocation}} == new Guid("3766c9f9-c6ae-5bd6-aca3-19efd66f89ae") ? 0 : 1;
                {{helper}}
            }
            {{declarations}}
            """);
        string output = await runner.GenerateAsync("dispatched-" + scenario);
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        Assert.Contains("[#System]System.Collections.Generic.IList`1[[WinRT.Runtime]Windows.Storage.Streams.InputStreamOptions]",
            GetIidKeys(module));
        await runner.RunApplicationAsync(output);
        if (Environment.Is64BitProcess && scenario is "default-interface" or "static-abstract")
        {
            await runner.PublishApplicationAsync(output, nativeAot: true);
        }
    }

    [TestMethod]
    public async Task CrossModuleReceiverAndMethodArguments_UseExistingDiscoveryState()
    {
        using InteropGeneratorRunner runner = new(iidLibrarySource: """
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public interface IQueries { Guid Get<T>(); }
            public sealed class Queries<TElement> : IQueries
            {
                public Guid Get<T>() => WindowsRuntimeMarshal.GetIid<KeyValuePair<TElement, T>>();
            }
            public static class QueryFactory
            {
                public static IQueries Create<T>() => new Queries<T>();
            }
            """,
            applicationSource: """
                using System;
                using Windows.Storage.Streams;
                public static class Program
                {
                    public static int Main() => QueryFactory.Create<InputStreamOptions>().Get<int>() ==
                        new Guid("d86200ab-f75f-52af-a4d6-59bb89642adc") ? 0 : 1;
                }
                """);
        string output = await runner.GenerateAsync("cross-module-iid");
        await runner.RunApplicationAsync(output);
        if (Environment.Is64BitProcess)
        {
            await runner.PublishApplicationAsync(output, nativeAot: true);
        }
    }

    [TestMethod]
    [DataRow("IList<T>", "Uri", "0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e")]
    [DataRow("IList<IList<T>>", "Uri", "bb509c13-b7a8-5405-b071-c4e9faa2d8d4")]
    [DataRow("KeyValuePair<string, T>", "Uri", "7b538fa1-784a-5c8e-8e61-03cba77ddad9")]
    [DataRow("EventHandler<T>", "Uri", "fa4b7b25-477f-55c6-9f8c-888c26d180c2")]
    [DataRow("IList<T>", "string", "98b9acc1-4b56-532e-ac73-03d5291cca90")]
    [DataRow("IList<T>[]", "Uri", "65067b0a-677e-5240-a076-7067394bdb28")]
    public async Task SharedReferenceGenericConstruction_KeepsIidProviders(string type, string argument, string expected)
    {
        using InteropGeneratorRunner runner = new(applicationSource: $$"""
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    IQueries query = new Queries();
                    return query.Get<{{argument}}>() == new Guid("{{expected}}") ? 0 : 1;
                }
            }
            public interface IQueries { Guid Get<T>(); }
            public sealed class Queries : IQueries
            {
                public Guid Get<T>() => WindowsRuntimeMarshal.GetIid<{{type}}>();
            }
            """);
        string output = await runner.GenerateAsync("shared-reference");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        foreach (CustomAttribute entry in GetIidEntries(module))
        {
            Assert.IsTrue(entry.Signature!.FixedArguments[2].Element is CorLibTypeSignature or TypeDefOrRefSignature);
        }
        await runner.RunApplicationAsync(output);
        if (Environment.Is64BitProcess)
        {
            await runner.PublishApplicationAsync(output, nativeAot: true);
        }
        await runner.PublishApplicationAsync(output, nativeAot: false, isolateUnsupportedTrimTargets: true);
    }

    [TestMethod]
    public async Task ReflectionSelectedGenericArguments_DoNotRequireStaticIidCalls()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using System.Reflection;
            using IidProjection;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    Type type = typeof(IList<Example>);
                    MethodInfo method = typeof(WindowsRuntimeMarshal).GetMethod(nameof(WindowsRuntimeMarshal.GetIid));
                    Guid iid = (Guid)method.MakeGenericMethod(type).Invoke(null, null);
                    return iid == new Guid("59bf76d5-ee4c-5b7e-914c-1d014338abd4") ? 0 : 1;
                }
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("reflection"));
    }

    [TestMethod]
    public async Task UndiscoveredRuntimeGenericType_ThrowsWithoutComputingAnIid()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            using System;
            using System.Collections.Generic;
            using System.Reflection;
            using IidProjection;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main()
                {
                    Type type = typeof(IList<>).MakeGenericType(typeof(Example));
                    MethodInfo method = typeof(WindowsRuntimeMarshal).GetMethod(nameof(WindowsRuntimeMarshal.GetIid));
                    try { method.MakeGenericMethod(type).Invoke(null, null); }
                    catch (TargetInvocationException e) when (e.InnerException is NotSupportedException) { return 0; }
                    return 1;
                }
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("undiscovered"));
    }

    [TestMethod]
    public async Task SameNameManagedType_DoesNotMatchAProvider()
    {
        using InteropGeneratorRunner runner = new(
            iidLibrarySource: """
                namespace System { internal interface IServiceProvider { } }
                public static class LibraryQueries
                {
                    public static bool Reject()
                    {
                        try { WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.IServiceProvider>(); }
                        catch (System.NotSupportedException) { return true; }
                        return false;
                    }
                }
                """,
            applicationSource: """
                public static class Program
                {
                    public static int Main() =>
                        LibraryQueries.Reject() &&
                        WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<System.IServiceProvider>() ==
                        new System.Guid("68b3a2df-8173-539f-b524-c8a2348f5afb") ? 0 : 1;
                }
                """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("same-name"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task StructuralKeys_UseLogicalSystemScopeAcrossFrameworkFacades(bool implementations)
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: implementations, sdkProjectionSource: ProjectionSource,
            applicationSource: """
                using System;
                using System.Collections.Generic;
                using System.ComponentModel;
                using System.Collections.Specialized;
                using System.Numerics;
                using System.Runtime.InteropServices;
                using System.Windows.Input;
                using IidProjection;
                using WindowsRuntime.InteropServices;
                public static class Program
                {
                    public static int Main()
                    {
                        CheckSystemValue<bool>();
                        CheckSystemValue<byte>();
                        CheckSystemValue<char>();
                        CheckSystemValue<short>();
                        CheckSystemValue<ushort>();
                        CheckSystemValue<int>();
                        CheckSystemValue<uint>();
                        CheckSystemValue<long>();
                        CheckSystemValue<ulong>();
                        CheckSystemValue<float>();
                        CheckSystemValue<double>();
                        CheckSystem<object>();
                        CheckSystemValue<Guid>();
                        CheckSystem<Uri>();
                        Check<IList<string>>("[#System]System.Collections.Generic.IList`1[[#System]System.String]");
                        Check<IList<Exception>>("[#System]System.Collections.Generic.IList`1[[#System]System.Exception]");
                        Check<IList<Type>>("[#System]System.Collections.Generic.IList`1[[#System]System.Type]");
                        CheckSystemValue<DateTimeOffset>();
                        CheckSystemValue<TimeSpan>();
                        CheckSystem<IDisposable>();
                        CheckSystem<IServiceProvider>();
                        CheckSystem<System.Collections.IEnumerable>();
                        CheckSystem<System.Collections.IEnumerator>();
                        CheckSystem<System.Collections.IList>();
                        CheckSystem<ICommand>();
                        CheckSystem<INotifyPropertyChanged>();
                        CheckSystem<INotifyDataErrorInfo>();
                        CheckSystem<INotifyCollectionChanged>();
                        CheckSystem<PropertyChangedEventArgs>();
                        CheckSystem<PropertyChangedEventHandler>();
                        CheckSystem<DataErrorsChangedEventArgs>();
                        CheckSystemValue<NotifyCollectionChangedAction>();
                        CheckSystem<NotifyCollectionChangedEventArgs>();
                        CheckSystem<NotifyCollectionChangedEventHandler>();
                        CheckSystemValue<Matrix3x2>();
                        CheckSystemValue<Matrix4x4>();
                        CheckSystemValue<Plane>();
                        CheckSystemValue<Quaternion>();
                        CheckSystemValue<Vector2>();
                        CheckSystemValue<Vector3>();
                        CheckSystemValue<Vector4>();
                        Check<IList<Uri>>("[#System]System.Collections.Generic.IList`1[[#System]System.Uri]");
                        Check<IReadOnlyList<int>>("[#System]System.Collections.Generic.IReadOnlyList`1[[#System]System.Int32]");
                        Check<IEnumerable<int>>("[#System]System.Collections.Generic.IEnumerable`1[[#System]System.Int32]");
                        Check<IEnumerator<int>>("[#System]System.Collections.Generic.IEnumerator`1[[#System]System.Int32]");
                        Check<IDictionary<string, int>>("[#System]System.Collections.Generic.IDictionary`2[[#System]System.String,[#System]System.Int32]");
                        Check<IReadOnlyDictionary<string, int>>("[#System]System.Collections.Generic.IReadOnlyDictionary`2[[#System]System.String,[#System]System.Int32]");
                        Check<EventHandler<int>>("[#System]System.EventHandler`1[[#System]System.Int32]");
                        Check<EventHandler<object, int>>("[#System]System.EventHandler`2[[#System]System.Object,[#System]System.Int32]");
                        Check<IList<int?>>("[#System]System.Collections.Generic.IList`1[[#System]System.Nullable`1[[#System]System.Int32]]");
                        Check<KeyValuePair<string, IList<int>>>(
                            "[#System]System.Collections.Generic.KeyValuePair`2[[#System]System.String,[#System]System.Collections.Generic.IList`1[[#System]System.Int32]]");
                        Check<IList<IReadOnlyList<Value>>>(
                            "[#System]System.Collections.Generic.IList`1[[#System]System.Collections.Generic.IReadOnlyList`1[[WinRT.Sdk.Projection]IidProjection.Value]]");
                        Check<Value[]>("[WinRT.Sdk.Projection]IidProjection.Value[]");
                        Check<Windows.Foundation.Point?>("[#System]System.Nullable`1[[WinRT.Runtime]Windows.Foundation.Point]");
                        Check<IList<int>[]>("[#System]System.Collections.Generic.IList`1[[#System]System.Int32][]");
                        return 0;
                    }
                    static void CheckSystem<T>() => Check<T>("[#System]" + typeof(T).FullName);
                    static void CheckSystemValue<T>() where T : struct =>
                        Check<T?>("[#System]System.Nullable`1[[#System]" + typeof(T).FullName + "]");
                    static void Check<T>(string key)
                    {
                        Type type = typeof(T);
                        var map = TypeMapping.GetOrCreateExternalTypeMapping<WindowsRuntimeIidTypeMapGroup>();
                        if (!map.TryGetValue(key, out Type provider)) throw new Exception("Missing structural key: " + key);
                        Guid iid = WindowsRuntimeMarshal.GetIid<T>();
                        if (iid != provider.GUID) throw new Exception("Runtime and generated keys disagree for " + type);
                    }
                }
                """);
        string output = await runner.GenerateAsync("structural-keys");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        Assert.IsFalse(GetIidKeys(module).Any(key =>
            key.Contains("[System.", StringComparison.Ordinal) || key.Contains("[Microsoft.", StringComparison.Ordinal)));
        await runner.RunApplicationAsync(output);

        if (!implementations && Environment.Is64BitProcess)
        {
            await runner.PublishApplicationAsync(output, nativeAot: true);
        }
    }

    [TestMethod]
    public async Task ReferenceProjectionArguments_UseTheImplementationAssemblyName()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource,
            sdkReferenceSource: """
                [assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssembly]
                namespace ProjectionInput
                {
                    public class Base { }
                    public class Derived : Base { }
                }
                namespace IidProjection { public class Example { } }
                """,
            applicationSource: """
                using System;
                using System.Collections.Generic;
                using IidProjection;
                using WindowsRuntime.InteropServices;
                public static class Program
                {
                    public static int Main() =>
                        WindowsRuntimeMarshal.GetIid<IList<Example>>() ==
                        new Guid("59bf76d5-ee4c-5b7e-914c-1d014338abd4") ? 0 : 1;
                }
                """);
        runner.RewriteApplicationTypeScope("IidProjection", "Example", "Microsoft.Windows.SDK.NET");
        string output = await runner.GenerateAsync("forwarded-projection");
        runner.StageSdkTypeForwarder(output, "IidProjection.Example");
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public void RuntimeIidLookup_HasNoHashingOrSignatureMetadata()
    {
        ModuleDefinition runtime = ModuleDefinition.FromFile(typeof(Windows.Foundation.IStringable).Assembly.Location);
        TypeDefinition info = runtime.TopLevelTypes.Single(type => type.FullName == "WindowsRuntime.InteropServices.WindowsRuntimeIidInfo");
        string[] calls = info.Methods.SelectMany(method => method.CilMethodBody?.Instructions.AsEnumerable() ?? [])
            .Select(instruction => instruction.Operand).OfType<IMethodDescriptor>()
            .Select(method => method.FullName).ToArray();

        Assert.IsFalse(calls.Any(call => call.Contains("Cryptography", StringComparison.Ordinal) ||
            call.Contains("Guid::Parse", StringComparison.Ordinal) || call.Contains("SignatureGenerator", StringComparison.Ordinal)));
        Assert.IsFalse(runtime.GetAllTypes().Any(type => type.Name == "WindowsRuntimeIidProviderAttribute"));
        Assert.IsFalse(info.Methods.Any(method => method.Name == "CreateGuidFromSignature"));
    }

    [TestMethod]
    public async Task OptedOutLibrary_MissingGenericArgumentDependencyDoesNotBreakIidGeneration()
    {
        using InteropGeneratorRunner runner = new(iidLibrarySource: """
            using System;
            using System.Collections.Generic;
            public static class OptionalLibrary
            {
                public static IList<Uri> Values;
                public static object GetValues() => Values;
            }
            """,
            applicationSource: """
                public static class Program
                {
                    public static int Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<int?>() ==
                        new System.Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4") ? 0 : 1;
                }
                """);
        runner.RewriteLibraryTypeScope("System", "Uri", "MissingOptionalDependency");
        string output = await runner.GenerateAsync("missing-argument", marshallingDisabledAssemblyNames: ["IidLibrary"]);
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task UnrelatedGenericLibrary_DoesNotExhaustTheApplicationDiscoveryBudget()
    {
        string declarations = string.Join("\n", Enumerable.Range(0, 1100).Select(index => $"public sealed class Marker{index} {{ }}"));
        string calls = string.Join("\n", Enumerable.Range(0, 1100).Select(index => $"Use<Marker{index}>();"));
        string library = $$"""
            {{declarations}}
            public static class UnrelatedGenericLibrary
            {
                public static void Run() { {{calls}} }
                public static System.Type Use<T>() => typeof(T);
            }
            """;
        using InteropGeneratorRunner runner = new(iidLibrarySource: library, applicationSource: """
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main() => Outer<Uri>() == new Guid("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e") ? 0 : 1;
                static Guid Outer<T>() => Inner<T>();
                static Guid Inner<T>() => WindowsRuntimeMarshal.GetIid<IList<T>>();
            }
            """);
        await runner.RunApplicationAsync(await runner.GenerateAsync("unrelated-generics", marshallingMode: "Strict"));
    }

    [TestMethod]
    public async Task SharedExpansionLimit_DoesNotHideExplicitMarshallingRoots()
    {
        string methods = string.Join("\n", Enumerable.Range(0, 40).Select(index =>
            index == 39 ? $"static void M{index}<T>() {{ }}" : $"static void M{index}<T>() => M{index + 1}<T>();"));
        using InteropGeneratorRunner runner = new(applicationSource: $$"""
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static IList<double> NormalValues;
                public static int Main()
                {
                    M0<int>();
                    return WindowsRuntimeMarshal.GetIid<int?>() == new System.Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4") ? 0 : 1;
                }
                {{methods}}
            }
            """);
        string output = await runner.GenerateAsync("metadata-budget", treatWarningsAsErrors: false);
        Assert.Contains("CSWINRTINTEROPGEN0104", runner.LastGeneratorLog);
        Assert.DoesNotContain("CSWINRTINTEROPGEN0105", runner.LastGeneratorLog);
        Assert.DoesNotContain("CSWINRTINTEROPGEN0106", runner.LastGeneratorLog);
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        Assert.IsTrue(module.TopLevelTypes.Any(type => type.Name!.ToString().Contains("IList'1<double>Impl", StringComparison.Ordinal)));
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task ExcludedLibrary_DoesNotContributeItsOwnGenericUsages()
    {
        using InteropGeneratorRunner runner = new(iidLibrarySource: """
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class ExcludedQueries
            {
                public static bool HasIid()
                {
                    try { WindowsRuntimeMarshal.GetIid<IList<Uri>>(); }
                    catch (NotSupportedException) { return false; }
                    return true;
                }
            }
            """,
            applicationSource: """
                public static class Program
                {
                    public static int Main() => ExcludedQueries.HasIid() ? 1 : 0;
                }
                """);
        string output = await runner.GenerateAsync("excluded-metadata", marshallingDisabledAssemblyNames: ["IidLibrary"]);
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task SharedSignatureComplexity_ReportsNormalDiscoveryDiagnostics()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static readonly Action UnusedExpansion = Expand<int>;
                public static IList<double> NormalValues;
                public static int Main() =>
                    WindowsRuntimeMarshal.GetIid<int?>() == new Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4") ? 0 : 1;
                static void Expand<T>() => Expand<KeyValuePair<T, T>>();
            }
            """);
        string output = await runner.GenerateAsync("metadata-complexity", treatWarningsAsErrors: false);
        Assert.Contains("CSWINRTINTEROPGEN0105", runner.LastGeneratorLog);
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        Assert.IsTrue(module.TopLevelTypes.Any(type => type.Name!.ToString().Contains("IList'1<double>Impl", StringComparison.Ordinal)));
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task RuntimeCatalog_IsAvailableWhenRuntimeMarshallingIsExcluded()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            using System;
            using System.Windows.Input;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static int Main() =>
                    WindowsRuntimeMarshal.GetIid<int?>() == new Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4") &&
                    WindowsRuntimeMarshal.GetIid<ICommand>() == new Guid("e5af3542-ca67-4081-995b-709dd13792df") ? 0 : 1;
            }
            """);
        string output = await runner.GenerateAsync("excluded-runtime", marshallingDisabledAssemblyNames: ["WinRT.Runtime"],
            treatWarningsAsErrors: false);
        await runner.RunApplicationAsync(output);
    }

    [TestMethod]
    public async Task ExistingMarshallerIids_EqualTheGuidOnlyProviderValues()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            public static class Program
            {
                public static IList<int> Values;
                public static IDictionary<string, int> Mapping;
                public static int Main() =>
                    WindowsRuntimeMarshal.GetIid<IList<int>>() == new System.Guid("b939af5b-b45d-5489-9149-61442c1905fe") ? 0 : 1;
            }
            """);
        string output = await runner.GenerateAsync("shared-iids");
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(output);
        var guids = GetIidEntries(module).Select(entry =>
        {
            TypeSignature target = (TypeSignature)entry.Signature!.FixedArguments[1].Element!;
            CustomAttribute attribute = target.Resolve(context).CustomAttributes.Single();
            return Guid.Parse(attribute.Signature!.FixedArguments[0].Element!.ToString()!);
        }).ToHashSet();
        TypeDefinition iids = module.TopLevelTypes.Single(type => type.FullName == "ABI.InterfaceIIDs");
        foreach (MethodDefinition method in iids.Methods.Where(method => !method.Name!.ToString().EndsWith("Reference", StringComparison.Ordinal)))
        {
            FieldDefinition field = (FieldDefinition)method.CilMethodBody!.Instructions.First(instruction => instruction.OpCode == CilOpCodes.Ldsflda).Operand!;
            IReadableSegment segment = (IReadableSegment)field.FieldRva!;
            BinaryStreamReader reader = segment.CreateReader(segment.Offset, 16);
            byte[] data = new byte[16];
            _ = reader.ReadBytes(data);
            Assert.Contains(new Guid(data), guids, method.FullName);
        }
        await runner.RunApplicationAsync(output);
    }

    private static CustomAttribute[] GetIidEntries(ModuleDefinition module)
    {
        return module.Assembly!.CustomAttributes.Where(attribute =>
            attribute.Constructor?.DeclaringType?.FullName == "System.Runtime.InteropServices.TypeMapAttribute`1<WindowsRuntime.InteropServices.WindowsRuntimeIidTypeMapGroup>").ToArray();
    }

    private static string[] GetIidKeys(ModuleDefinition module)
    {
        return GetIidEntries(module).Select(entry => entry.Signature!.FixedArguments[0].Element!.ToString()!).ToArray();
    }

    private static string ExpandRuntimeIids(string source)
    {
        Type constants = typeof(Windows.Foundation.IStringable).Assembly.GetType("WindowsRuntime.InteropServices.WellKnownWindowsInterfaceIIDs")!;

        return Regex.Replace(source, @"WellKnownWindowsInterfaceIIDs\.(IID_\w+)", match =>
        {
            PropertyInfo? property = constants.GetProperty(match.Groups[1].Value, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(property, match.Value);
            return $"new Guid(\"{property.GetValue(null)}\")";
        });
    }
}
