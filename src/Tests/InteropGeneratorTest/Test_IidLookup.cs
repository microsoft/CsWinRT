// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
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
            [WindowsRuntimeType]
            public struct Value { public int Number; public string Text; }
            [WindowsRuntimeType]
            public enum Choice { First, Second }
            [WindowsRuntimeType, Guid("60a8e0c5-7371-4b96-ab04-b2ccfafce870")]
            public delegate void Handler();
            [WindowsRuntimeType]
            public sealed class MetadataAttribute : Attribute { }
            [WindowsRuntimeType]
            public static class StaticClass { }
            public enum Contract { }
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
        }
        """;

    [TestMethod]
    public async Task IidEntries_AreExternalAndConditionalOnReferencedTypes()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("conditional"));
        CustomAttribute[] entries = GetIidEntries(module);
        string[] keys = GetIidKeys(module);

        Assert.IsNotEmpty(entries);
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

        foreach (CustomAttribute entry in entries)
        {
            Assert.HasCount(3, entry.Signature!.FixedArguments);
            Assert.IsTrue(entry.Signature.FixedArguments[2].Element is CorLibTypeSignature or TypeDefOrRefSignature);
            Assert.AreNotEqual(entry.Signature.FixedArguments[1].Element, entry.Signature.FixedArguments[2].Element);
            TypeDefinition provider = ((TypeSignature)entry.Signature.FixedArguments[1].Element!).Resolve(module.RuntimeContext);
            Assert.HasCount(1, provider.CustomAttributes);
            Assert.AreEqual("System.Runtime.InteropServices.GuidAttribute", provider.CustomAttributes[0].Constructor!.DeclaringType!.FullName);
            Assert.IsEmpty(provider.Fields);
            Assert.IsEmpty(provider.Methods);
        }
    }

    [TestMethod]
    public async Task ProjectionCatalog_UsesInterfaceDefaultAndExplicitReferenceIids()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            public static class Program
            {
                public static System.Collections.Generic.IDictionary<string, object> Values;
                public static void Main() { }
            }
            """);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("projection-catalog"));
        string[] keys = GetIidKeys(module);

        Assert.AreEqual(new Guid("69c58f47-a268-4a55-aad8-97d111a2b01a"),
            GetProviderIid(module, "[WinRT.Sdk.Projection]IidProjection.IExample"));
        Assert.AreEqual(GetProviderIid(module, "[WinRT.Sdk.Projection]IidProjection.IExample"),
            GetProviderIid(module, "[WinRT.Sdk.Projection]IidProjection.Example"));
        Assert.AreEqual(GetProviderIid(module, "[#System]System.Collections.Generic.IDictionary`2[[#System]System.String,[#System]System.Object]"),
            GetProviderIid(module, "[WinRT.Sdk.Projection]IidProjection.GenericDefault"));
        Assert.AreEqual(new Guid("60a8e0c5-7371-4b96-ab04-b2ccfafce870"),
            GetProviderIid(module, "[WinRT.Sdk.Projection]IidProjection.Handler"));
        Assert.AreEqual(new Guid("513dbf61-50a0-5a16-bb0e-96ae18a9f125"),
            GetProviderIid(module, "[#System]System.Nullable`1[[WinRT.Sdk.Projection]IidProjection.Value]"));
        Assert.Contains("[#System]System.Nullable`1[[WinRT.Sdk.Projection]IidProjection.Choice]", keys);
        foreach (string type in new[] { "IidProjection.IHidden", "IidProjection.HiddenClass", "ABI.Impl.IExclusive" })
        {
            Assert.AreEqual(new Guid("71f0284e-cc83-4c12-95ea-89749cdfc7f5"),
                GetProviderIid(module, "[WinRT.Sdk.Projection]" + type));
        }
        foreach (string type in new[] { "ABI.IidProjection.Value", "IidProjection.MetadataAttribute", "IidProjection.StaticClass", "IidProjection.Contract" })
        {
            Assert.DoesNotContain("[WinRT.Sdk.Projection]" + type, keys);
        }
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
                public static class Program { public static void Main() { } }
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
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("component-catalog"));

        Assert.AreEqual(GetProviderIid(module, "[Application]Component.IExample"), GetProviderIid(module, "[Application]Component.Class"));
        Assert.Contains("[#System]System.Nullable`1[[Application]Component.Value]", GetIidKeys(module));
        Assert.Contains("[#System]System.Nullable`1[[Application]Component.Choice]", GetIidKeys(module));
        Assert.AreEqual(new Guid("60a8e0c5-7371-4b96-ab04-b2ccfafce870"), GetProviderIid(module, "[Application]Component.Handler"));
        Assert.DoesNotContain("[Application]Component.Helper", GetIidKeys(module));
    }

    [TestMethod]
    public async Task RuntimeCatalog_IsAvailableWhenRuntimeMarshallingIsExcluded()
    {
        using InteropGeneratorRunner runner = new();
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync(
            "excluded-runtime", marshallingDisabledAssemblyNames: ["WinRT.Runtime"], treatWarningsAsErrors: false));

        Assert.AreEqual(new Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4"),
            GetProviderIid(module, "[#System]System.Nullable`1[[#System]System.Int32]"));
        Assert.AreEqual(new Guid("e5af3542-ca67-4081-995b-709dd13792df"),
            GetProviderIid(module, "[#System]System.Windows.Input.ICommand"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MappedXamlTypes_FollowProjectionMode(bool useWindowsUIXamlProjections)
    {
        using InteropGeneratorRunner runner = new();
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync(
            "xaml", useWindowsUIXamlProjections: useWindowsUIXamlProjections));

        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "cf75d69c-f2f4-486b-b302-bb4c09baebfa" : "90b17601-b065-586e-83d9-9adc3a695284"),
            GetProviderIid(module, "[#System]System.ComponentModel.INotifyPropertyChanged"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "4f33a9a0-5cf4-47a4-b16f-d7faaf17457e" : "63d0c952-396b-54f4-af8c-ba8724a427bf"),
            GetProviderIid(module, "[#System]System.ComponentModel.PropertyChangedEventArgs"));
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
                using IidProjection;
                public static class Program
                {
                    public static IList<Uri> UriList;
                    public static IList<Exception> Exceptions;
                    public static IList<Type> Types;
                    public static EventHandler<object, int> Handler;
                    public static IDictionary<string, int> Mapping;
                    public static KeyValuePair<string, IList<int>> Pair;
                    public static IList<IReadOnlyList<Value>> Nested;
                    public static IList<int?> NullableValues;
                    public static Value[] Array;
                    public static IList<int>[] GenericArray;
                    public static void Main() { }
                }
                """);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("framework-keys"));
        string[] keys = GetIidKeys(module);

        foreach (string name in new[]
        {
            "Boolean", "Byte", "Char", "Double", "Single", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64",
            "Guid", "DateTimeOffset", "TimeSpan", "Numerics.Matrix3x2", "Numerics.Matrix4x4", "Numerics.Plane",
            "Numerics.Quaternion", "Numerics.Vector2", "Numerics.Vector3", "Numerics.Vector4",
            "Collections.Specialized.NotifyCollectionChangedAction"
        })
        {
            Assert.Contains("[#System]System.Nullable`1[[#System]System." + name + "]", keys);
        }
        foreach (string name in new[]
        {
            "Object", "Uri", "IDisposable", "IServiceProvider", "Windows.Input.ICommand",
            "Collections.IEnumerable", "Collections.IEnumerator", "Collections.IList",
            "ComponentModel.INotifyPropertyChanged", "ComponentModel.INotifyDataErrorInfo",
            "ComponentModel.PropertyChangedEventArgs", "ComponentModel.PropertyChangedEventHandler",
            "ComponentModel.DataErrorsChangedEventArgs", "Collections.Specialized.INotifyCollectionChanged",
            "Collections.Specialized.NotifyCollectionChangedEventArgs", "Collections.Specialized.NotifyCollectionChangedEventHandler"
        })
        {
            Assert.Contains("[#System]System." + name, keys);
        }
        foreach (string key in new[]
        {
            "[#System]System.Collections.Generic.IList`1[[#System]System.Uri]",
            "[#System]System.Collections.Generic.IList`1[[#System]System.Exception]",
            "[#System]System.Collections.Generic.IList`1[[#System]System.Type]",
            "[#System]System.Collections.Generic.IDictionary`2[[#System]System.String,[#System]System.Int32]",
            "[#System]System.EventHandler`2[[#System]System.Object,[#System]System.Int32]",
            "[#System]System.Collections.Generic.KeyValuePair`2[[#System]System.String,[#System]System.Collections.Generic.IList`1[[#System]System.Int32]]",
            "[#System]System.Collections.Generic.IList`1[[#System]System.Collections.Generic.IReadOnlyList`1[[WinRT.Sdk.Projection]IidProjection.Value]]",
            "[#System]System.Collections.Generic.IList`1[[#System]System.Nullable`1[[#System]System.Int32]]",
            "[WinRT.Sdk.Projection]IidProjection.Value[]",
            "[#System]System.Collections.Generic.IList`1[[#System]System.Int32][]",
            "[#System]System.Nullable`1[[WinRT.Runtime]Windows.Foundation.Point]"
        })
        {
            Assert.Contains(key, keys);
        }

        foreach ((string key, string target) in new[]
        {
            ("[#System]System.Collections.Generic.KeyValuePair`2[[#System]System.String,[#System]System.Collections.Generic.IList`1[[#System]System.Int32]]", "System.Int32"),
            ("[#System]System.Collections.Generic.IList`1[[#System]System.Int32][]", "System.Int32"),
            ("[#System]System.Nullable`1[[WinRT.Runtime]Windows.Foundation.Point]", "Windows.Foundation.Point")
        })
        {
            CustomAttribute entry = GetIidEntries(module).Single(entry => entry.Signature!.FixedArguments[0].Element!.ToString() == key);
            Assert.AreEqual(target, ((TypeSignature)entry.Signature!.FixedArguments[2].Element!).FullName);
        }

        Assert.IsFalse(keys.Any(key => key.Contains("[System.", StringComparison.Ordinal) || key.Contains("[Microsoft.", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task MovedFrameworkType_UsesLogicalScopeThroughItsFacade()
    {
        using InteropGeneratorRunner runner = new(
            forwardUriToFutureAssembly: true,
            applicationSource: """
                public static class Program
                {
                    public static System.Collections.Generic.IList<System.Uri> Values;
                    public static void Main() { }
                }
                """);
        runner.RewriteApplicationTypeScope("System", "Uri", "FutureFramework");
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("moved-framework"));

        Assert.AreEqual(new Guid("9e365e57-48b2-4160-956f-c7385120bbfc"), GetProviderIid(module, "[#System]System.Uri"));
        Assert.AreEqual(new Guid("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e"),
            GetProviderIid(module, "[#System]System.Collections.Generic.IList`1[[#System]System.Uri]"));
        Assert.IsFalse(GetIidKeys(module).Any(key => key.Contains("[FutureFramework]", StringComparison.Ordinal)));
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
            iidLibrarySource: """
                namespace System
                {
                    internal sealed class Uri;
                    internal interface IServiceProvider;
                    [WindowsRuntime.WindowsRuntimeType]
                    internal enum ContractValue { First }
                }
                public static class LibraryInputs
                {
                    private static System.Collections.Generic.IList<System.ContractValue> Values;
                    private static System.Collections.Generic.IList<System.Uri> InvalidValues;
                    private static System.Collections.Generic.IList<System.IServiceProvider> InvalidProviders;
                    private static System.IServiceProvider InvalidProvider;
                }
                """);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("impostor-keys"));
        string[] keys = GetIidKeys(module);

        Assert.Contains("[#System]System.Collections.Generic.IList`1[[" + expectedScope + "]System.ContractValue]", keys);
        Assert.DoesNotContain("[#System]System.Collections.Generic.IList`1[[#System]System.ContractValue]", keys);
        Assert.DoesNotContain("[" + expectedScope + "]System.Uri", keys);
        Assert.DoesNotContain("[" + expectedScope + "]System.IServiceProvider", keys);
        Assert.DoesNotContain("[#System]System.Collections.Generic.IList`1[[#System]System.Uri]", keys);
        Assert.DoesNotContain("[#System]System.Collections.Generic.IList`1[[#System]System.IServiceProvider]", keys);
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
                public static class Program
                {
                    public static System.Collections.Generic.IList<IidProjection.Example> Values;
                    public static void Main() { }
                }
                """);
        runner.RewriteApplicationTypeScope("IidProjection", "Example", "Microsoft.Windows.SDK.NET", originalAssemblyName: "WinRT.Sdk.Projection");
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("reference-projection"));
        string[] keys = GetIidKeys(module);

        Assert.Contains("[#System]System.Collections.Generic.IList`1[[WinRT.Sdk.Projection]IidProjection.Example]", keys);
        Assert.IsFalse(keys.Any(key => key.Contains("[Microsoft.Windows.SDK.NET]", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task OverlappingFrameworkReferences_HaveOneIidProviderPerIdentity(bool implementations)
    {
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: implementations, overlappingFrameworkReferences: true);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("overlapping-frameworks"));
        string[] keys = GetIidKeys(module);

        Assert.AreEqual(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
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
        byte[] expected = File.ReadAllBytes(await runner.GenerateAsync("ordered"));
        foreach ((bool reverse, int parallelism) in new[] { (true, 1), (false, 2), (true, -1) })
        {
            CollectionAssert.AreEqual(expected, File.ReadAllBytes(await runner.GenerateAsync(
                $"order-{reverse}-{parallelism}", reverseInputs: reverse, parallelism: parallelism)));
        }
    }

    [TestMethod]
    public async Task ExistingMarshallerIids_EqualTheGuidOnlyProviderValues()
    {
        using InteropGeneratorRunner runner = new(applicationSource: """
            public static class Program
            {
                public static System.Collections.Generic.IList<int> Values;
                public static System.Collections.Generic.IDictionary<string, int> Mapping;
                public static void Main() { }
            }
            """);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("marshaller-iids"));
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

    private static Guid GetProviderIid(ModuleDefinition module, string key)
    {
        CustomAttribute entry = GetIidEntries(module).Single(entry => entry.Signature!.FixedArguments[0].Element!.ToString() == key);
        TypeDefinition provider = ((TypeSignature)entry.Signature!.FixedArguments[1].Element!).Resolve(module.RuntimeContext);
        return Guid.Parse(provider.CustomAttributes.Single().Signature!.FixedArguments[0].Element!.ToString()!);
    }
}
