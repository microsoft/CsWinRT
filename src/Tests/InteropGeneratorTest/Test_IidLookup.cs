// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    private const string IidApplicationSource = """
        public static class Program
        {
            public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
        }
        """;

    private const string ProjectionSource = """
        using System;
        using System.Collections.Generic;
        using System.Runtime.InteropServices;
        using WindowsRuntime;

        namespace IidProjection
        {
            [WindowsRuntimeType, Guid("69c58f47-a268-4a55-aad8-97d111a2b01a")]
            public interface IExample { }
            [WindowsRuntimeType, Guid("a0c20f96-4e7b-4c21-9c6c-7ba480bf0e59")]
            public interface IGeneric<T> { }
            [WindowsRuntimeType]
            public class Example { }
            [WindowsRuntimeType]
            public class GenericDefault { }
            [WindowsRuntimeType]
            public class UnmarkedDefault { }
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
            [WindowsRuntimeMetadata(typeof(global::IidProjection.IGeneric<>), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.Example), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.GenericDefault), "Fixture")]
            [WindowsRuntimeMetadata(typeof(global::IidProjection.UnmarkedDefault), "Fixture")]
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
            [WindowsRuntimeDefaultInterface(typeof(global::IidProjection.UnmarkedDefault), typeof(ABI.Impl.IUnmarked))]
            [WindowsRuntimeDefaultInterface(typeof(global::IidProjection.HiddenClass), typeof(global::IidProjection.IHidden))]
            internal static class WindowsRuntimeDefaultInterfaces { }
        }
        namespace ABI.Impl
        {
            [WindowsRuntimeType, Guid("71f0284e-cc83-4c12-95ea-89749cdfc7f5")]
            internal interface IExclusive { }
            [Guid("59d49473-21e5-4f1a-870c-6c93a1d8206d")]
            internal interface IUnmarked { }
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
    [DataRow(false)]
    [DataRow(true)]
    public async Task NoCompiledReference_DoesNotEmitSidecarIidMetadata(bool reflectionOnly)
    {
        using InteropGeneratorRunner runner = new(
            sdkProjectionSource: ProjectionSource,
            applicationSource: reflectionOnly ? """
                public static class Program
                {
                    public static void Main()
                    {
                        _ = typeof(WindowsRuntime.InteropServices.WindowsRuntimeMarshal).GetMethod("GetIid");
                    }
                }
                """ : null);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("unused-iid"));

        Assert.IsEmpty(GetIidEntries(module));
        Assert.IsFalse(module.TopLevelTypes.Any(type => type.Name?.ToString().EndsWith("IidProvider", StringComparison.Ordinal) is true));
    }

    [TestMethod]
    [DataRow("public static void Main() { WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>(); }")]
    [DataRow("public static System.Guid Lookup<T>() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<T>(); public static void Main() { }")]
    [DataRow("public static void Main() { System.Func<System.Guid> lookup = WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>; }")]
    [DataRow("public static void Main() { System.Linq.Expressions.Expression<System.Func<System.Guid>> lookup = () => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>(); }")]
    public async Task CompiledReferences_EnableTheCompleteCatalog(string members)
    {
        using InteropGeneratorRunner runner = new(
            sdkProjectionSource: ProjectionSource,
            applicationSource: "public static class Program { " + members + " }");
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("used-iid"));

        Assert.AreEqual(new Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4"),
            GetProviderIid(module, "[#System]System.Nullable`1[[#System]System.Int32]"));
        Assert.AreEqual(new Guid("90b17601-b065-586e-83d9-9adc3a695284"),
            GetProviderIid(module, "[#System]System.ComponentModel.INotifyPropertyChanged"));
    }

    [TestMethod]
    public async Task ExcludedAssemblyReference_StillEnablesIidMetadata()
    {
        using InteropGeneratorRunner runner = new(iidLibrarySource: """
            public static class Library
            {
                public static System.Guid Lookup<T>() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<T>();
            }
            """);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync(
            "excluded-iid", marshallingDisabledAssemblyNames: ["IidLibrary"]));

        Assert.AreEqual(new Guid("548cefbd-bc8a-5fa0-8df2-957440fc8bf4"),
            GetProviderIid(module, "[#System]System.Nullable`1[[#System]System.Int32]"));
        Assert.Contains("[#System]System.ComponentModel.INotifyPropertyChanged",
            GetIidEntries(module).Select(entry => entry.Signature!.FixedArguments[0].Element!.ToString()!));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RuntimeIidEntries_AreConditionalAndDisjointFromGeneratedEntries(bool useWindowsUIXamlProjections)
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: IidApplicationSource);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync(
            "runtime-ownership", useWindowsUIXamlProjections: useWindowsUIXamlProjections));
        CustomAttribute[] runtimeEntries = GetRuntimeIidEntries(module);
        string[] runtimeKeys = runtimeEntries.Select(entry => entry.Signature!.FixedArguments[0].Element!.ToString()!).ToArray();

        Assert.HasCount(50, runtimeEntries);
        Assert.HasCount(runtimeEntries.Length, runtimeKeys.Distinct());
        Assert.IsEmpty(GetIidEntries(module)
            .Select(entry => entry.Signature!.FixedArguments[0].Element!.ToString()!)
            .Intersect(runtimeKeys));

        foreach (CustomAttribute entry in runtimeEntries)
        {
            Assert.HasCount(3, entry.Signature!.FixedArguments);
            TypeDefinition provider = ((TypeSignature)entry.Signature.FixedArguments[1].Element!).Resolve(context);
            TypeDefinition trimTarget = ((TypeSignature)entry.Signature.FixedArguments[2].Element!).Resolve(context);
            Assert.IsFalse(trimTarget.HasGenericParameters);
            Assert.IsTrue(provider.IsAbstract && provider.IsSealed);
            Assert.HasCount(1, provider.CustomAttributes);
            Assert.AreEqual("System.Runtime.InteropServices.GuidAttribute", provider.CustomAttributes[0].Constructor!.DeclaringType!.FullName);
            Assert.IsEmpty(provider.Fields);
            Assert.IsEmpty(provider.Methods);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RuntimeIidEntries_MatchTheCompleteRuntimeCatalog(bool useWindowsUIXamlProjections)
    {
        using InteropGeneratorRunner runner = new(applicationSource: IidApplicationSource);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync(
            "runtime-catalog", useWindowsUIXamlProjections: useWindowsUIXamlProjections));
        ModuleDefinition runtime = context.LoadAssembly(typeof(WindowsRuntime.WindowsRuntimeObject).Assembly.Location).Modules[0];
        CustomAttribute[] runtimeEntries = GetIidEntries(runtime);
        string[] modeDependentTypes = [
            "System.ComponentModel.INotifyPropertyChanged",
            "System.ComponentModel.PropertyChangedEventArgs",
            "System.ComponentModel.PropertyChangedEventHandler",
            "System.Collections.Specialized.INotifyCollectionChanged",
            "System.Collections.Specialized.NotifyCollectionChangedEventArgs",
            "System.Collections.Specialized.NotifyCollectionChangedEventHandler",
            "System.Collections.Specialized.NotifyCollectionChangedAction"];

        // Runtime-owned registrations must cover every supported, non-generic runtime identity
        IEnumerable<TypeSignature> types = runtime.TopLevelTypes
            .Where(type => !type.HasGenericParameters && type.CustomAttributes.Any(attribute =>
                attribute.Constructor?.DeclaringType?.FullName == "WindowsRuntime.WindowsRuntimeTypeAttribute"))
            .Select(type => type.CustomAttributes.FirstOrDefault(attribute =>
                attribute.Constructor?.DeclaringType?.FullName == "WindowsRuntime.InteropServices.WindowsRuntimeMappedTypeAttribute")
                ?.Signature?.FixedArguments[0].Element is TypeSignature mappedType ? mappedType : type.ToTypeSignature())
            .Where(type => !modeDependentTypes.Contains(type.FullName));
        HashSet<string> expected = [];
        foreach (TypeSignature type in types)
        {
            TypeDefinition definition = type.Resolve(context);
            if (definition.HasGenericParameters || (definition.IsAbstract && definition.IsSealed) ||
                definition.BaseType?.FullName == "System.Attribute" ||
                type.FullName is "System.String" or "System.Exception" or "System.Type")
            {
                continue;
            }
            string scope = definition.DeclaringModule!.Assembly!.Name == "WinRT.Runtime" ? "WinRT.Runtime" : "#System";
            string key = "[" + scope + "]" + definition.FullName;
            _ = expected.Add(definition.IsValueType ? "[#System]System.Nullable`1[" + key + "]" : key);
        }
        expected.UnionWith(new[] { "IEnumerable", "IEnumerator", "IList" }.Select(name => "[#System]System.Collections." + name));
        _ = expected.Add("[#System]System.Windows.Input.ICommand");
        CollectionAssert.AreEquivalent(expected.ToArray(), runtimeEntries.Select(entry =>
            entry.Signature!.FixedArguments[0].Element!.ToString()!).ToArray());
    }

    [TestMethod]
    public async Task FirstCompiledReference_InvalidatesTheIncrementalCache()
    {
        // Keep the runtime assembly reference unchanged when adding the first IID call
        using InteropGeneratorRunner unused = new(applicationSource: """
            public static class Program
            {
                public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.NativeReferenceEquals(null, null);
            }
            """);
        using InteropGeneratorRunner used = new(applicationSource: IidApplicationSource);
        using InteropGeneratorRunner changedBody = new(applicationSource: """
            public static class Program
            {
                public static void Main()
                {
                    WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
                    WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
                }
            }
            """);
        string output = await unused.GenerateAsync("cached-iid");
        string response = Path.Combine(Path.GetDirectoryName(output)!, "interop.rsp");
        string application = Path.Combine(unused.Root, "Application.dll");
        byte[] originalApplication = File.ReadAllBytes(application);
        File.WriteAllText(response, File.ReadAllText(response).Replace(
            "--enable-incremental-generation False", "--enable-incremental-generation True", StringComparison.Ordinal));

        (int exitCode, string log) = await InteropGeneratorRunner.InvokeGeneratorAsync(response);
        Assert.AreEqual(0, exitCode, log);
        Assert.Contains("Generating interop code", log);
        Assert.IsEmpty(GetIidEntries(unused.LoadOutput(output).Module));

        (exitCode, log) = await InteropGeneratorRunner.InvokeGeneratorAsync(response);
        Assert.AreEqual(0, exitCode, log);
        Assert.Contains("Cached interop code ->", log);

        File.Copy(Path.Combine(used.Root, "Application.dll"), application, overwrite: true);
        (exitCode, log) = await InteropGeneratorRunner.InvokeGeneratorAsync(response);
        Assert.AreEqual(0, exitCode, log);
        Assert.Contains("Generating interop code", log);
        Assert.IsNotEmpty(GetIidEntries(unused.LoadOutput(output).Module));

        (exitCode, log) = await InteropGeneratorRunner.InvokeGeneratorAsync(response);
        Assert.AreEqual(0, exitCode, log);
        Assert.Contains("Cached interop code ->", log);

        File.Copy(Path.Combine(changedBody.Root, "Application.dll"), application, overwrite: true);
        (exitCode, log) = await InteropGeneratorRunner.InvokeGeneratorAsync(response);
        Assert.AreEqual(0, exitCode, log);
        Assert.Contains("Cached interop code ->", log);

        File.WriteAllBytes(application, originalApplication);
        (exitCode, log) = await InteropGeneratorRunner.InvokeGeneratorAsync(response);
        Assert.AreEqual(0, exitCode, log);
        Assert.Contains("Generating interop code", log);
        Assert.IsEmpty(GetIidEntries(unused.LoadOutput(output).Module));
    }

    [TestMethod]
    public async Task IidEntries_AreExternalAndConditionalOnReferencedTypes()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: IidApplicationSource);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("conditional"));
        CustomAttribute[] entries = GetAllIidEntries(module).ToArray();
        string[] keys = GetIidKeys(module);

        Assert.IsNotEmpty(entries);
        Assert.Contains("[#System]System.Nullable`1[[#System]System.Int32]", keys);
        Assert.DoesNotContain("[#System]System.Nullable`1[[WinRT.Sdk.Projection]IidProjection.Value]", keys);
        foreach (string type in new[] { "Int32", "String", "Guid", "Exception", "Type" })
        {
            Assert.DoesNotContain("[#System]System." + type, keys);
        }
        Assert.DoesNotContain("[WinRT.Sdk.Projection]IidProjection.Value", keys);
        Assert.DoesNotContain("[WinRT.Sdk.Projection]IidProjection.Choice", keys);
        Assert.DoesNotContain("[WinRT.Sdk.Projection]IidProjection.IGeneric`1", keys);
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
    public async Task ProjectionCatalog_IsNotReemittedByTheInteropGenerator()
    {
        using InteropGeneratorRunner runner = new(sdkProjectionSource: ProjectionSource, applicationSource: """
            public static class Program
            {
                public static System.Collections.Generic.IDictionary<string, object> Values;
                public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("projection-catalog"));
        string[] keys = GetIidKeys(module);

        Assert.Contains("[#System]System.Collections.Generic.IDictionary`2[[#System]System.String,[#System]System.Object]", keys);
        Assert.IsFalse(keys.Any(key => key.StartsWith("[WinRT.Sdk.Projection]", StringComparison.Ordinal)));
        Assert.IsFalse(keys.Any(key => key.StartsWith("[#System]System.Nullable`1[[WinRT.Sdk.Projection]", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task ProjectionCatalogIids_AreNotRecomputed()
    {
        using InteropGeneratorRunner runner = new(
            sdkProjectionSource: """
                [WindowsRuntime.WindowsRuntimeType]
                public interface IMissingIid { }
                namespace ABI
                {
                    [global::WindowsRuntime.WindowsRuntimeMetadata(typeof(IMissingIid), "Fixture")]
                    internal static class WindowsRuntimeMetadataTypes { }
                }
                """,
            applicationSource: IidApplicationSource);

        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("projection-owned-iid"));
        Assert.DoesNotContain("[WinRT.Sdk.Projection]IMissingIid", GetIidKeys(module));
    }

    [TestMethod]
    public async Task ComponentCatalog_IsNotReemittedByTheInteropGenerator()
    {
        using InteropGeneratorRunner runner = new(
            applicationSource: """
                using System;
                using System.Runtime.InteropServices;
                using WindowsRuntime.InteropServices;
                [assembly: WindowsRuntimeComponentAssembly]
                public static class Program
                {
                    public static void Main() => WindowsRuntimeMarshal.GetIid<object>();
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
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("component-catalog"));

        Assert.IsFalse(GetIidKeys(module).Any(key => key.Contains("[Application]Component.", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task RuntimeIids_AreAvailableWhenRuntimeMarshallingIsExcluded()
    {
        using InteropGeneratorRunner runner = new(applicationSource: IidApplicationSource);
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
        using InteropGeneratorRunner runner = new(applicationSource: """
            using System;
            using System.Collections.Generic;
            using System.Collections.Specialized;
            using System.ComponentModel;
            public static class Program
            {
                public static IList<Uri> Uris;
                public static IList<PropertyChangedEventArgs> PropertyChanges;
                public static IList<NotifyCollectionChangedEventArgs> CollectionChanges;
                public static IList<DataErrorsChangedEventArgs> DataErrors;
                public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """);
        (_, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync(
            "xaml", useWindowsUIXamlProjections: useWindowsUIXamlProjections));

        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "cf75d69c-f2f4-486b-b302-bb4c09baebfa" : "90b17601-b065-586e-83d9-9adc3a695284"),
            GetProviderIid(module, "[#System]System.ComponentModel.INotifyPropertyChanged"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "4f33a9a0-5cf4-47a4-b16f-d7faaf17457e" : "63d0c952-396b-54f4-af8c-ba8724a427bf"),
            GetProviderIid(module, "[#System]System.ComponentModel.PropertyChangedEventArgs"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "50f19c16-0a22-4d8e-a089-1ea9951657d2" : "e3de52f6-1e32-5da6-bb2d-b5b6096c962d"),
            GetProviderIid(module, "[#System]System.ComponentModel.PropertyChangedEventHandler"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "28b167d5-1a31-465b-9b25-d5c3ae686c40" : "530155e1-28a5-5693-87ce-30724d95a06d"),
            GetProviderIid(module, "[#System]System.Collections.Specialized.INotifyCollectionChanged"));
        Assert.AreEqual(new Guid("9e365e57-48b2-4160-956f-c7385120bbfc"),
            GetProviderIid(module, "[#System]System.Uri"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "4cf68d33-e3f2-4964-b85e-945b4f7e2f21" : "da049ff2-d2e0-5fe8-8c7b-f87f26060b6f"),
            GetProviderIid(module, "[#System]System.Collections.Specialized.NotifyCollectionChangedEventArgs"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "ca10b37c-f382-4591-8557-5e24965279b0" : "8b0909dc-2005-5d93-bf8a-725f017baa8d"),
            GetProviderIid(module, "[#System]System.Collections.Specialized.NotifyCollectionChangedEventHandler"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "0ffb101a-bc7d-50c3-8193-55cdf7448fa2" : "5f754c9d-bc42-56ea-96c0-70113dbd62a4"),
            GetProviderIid(module, "[#System]System.Nullable`1[[#System]System.Collections.Specialized.NotifyCollectionChangedAction]"));
        Assert.AreEqual(new Guid("d026dd64-5f26-5f15-a86a-0dec8a431796"),
            GetProviderIid(module, "[#System]System.ComponentModel.DataErrorsChangedEventArgs"));
        Assert.AreEqual(new Guid("0d82bd8d-fe62-5d67-a7b9-7886dd75bc4e"),
            GetProviderIid(module, "[#System]System.Collections.Generic.IList`1[[#System]System.Uri]"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "66af7157-76bf-5ddd-8fb5-eae1638b8309" : "3d1941dd-0165-5e8e-a121-1130d764de6e"),
            GetProviderIid(module, "[#System]System.Collections.Generic.IList`1[[#System]System.ComponentModel.PropertyChangedEventArgs]"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "3e5d933a-1220-5d32-aeee-262e2a7053dd" : "e77c5a02-ed7b-5bd9-b9cc-12c8708c157d"),
            GetProviderIid(module, "[#System]System.Collections.Generic.IList`1[[#System]System.Collections.Specialized.NotifyCollectionChangedEventArgs]"));
        Assert.AreEqual(new Guid(useWindowsUIXamlProjections ? "68ab49b0-d5cf-5702-9fb1-f99202ce961f" : "11a29269-041c-5820-a5c5-218828271514"),
            GetProviderIid(module, "[#System]System.Collections.Generic.IList`1[[#System]System.ComponentModel.DataErrorsChangedEventArgs]"));
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
                    public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
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
            CustomAttribute entry = GetAllIidEntries(module).Single(entry => entry.Signature!.FixedArguments[0].Element!.ToString() == key);
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
                    public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
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
            applicationSource: IidApplicationSource,
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
                    public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
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
        using InteropGeneratorRunner runner = new(useFrameworkImplementations: implementations, overlappingFrameworkReferences: true,
            applicationSource: IidApplicationSource);
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
                public static void Main() => WindowsRuntime.InteropServices.WindowsRuntimeMarshal.GetIid<object>();
            }
            """);
        (RuntimeContext context, ModuleDefinition module) = runner.LoadOutput(await runner.GenerateAsync("marshaller-iids"));
        var guids = GetAllIidEntries(module).Select(entry =>
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
        return GetAllIidEntries(module).Select(entry => entry.Signature!.FixedArguments[0].Element!.ToString()!).ToArray();
    }

    private static CustomAttribute[] GetRuntimeIidEntries(ModuleDefinition module)
    {
        ModuleDefinition runtime = module.RuntimeContext!.LoadAssembly(typeof(WindowsRuntime.WindowsRuntimeObject).Assembly.Location).Modules[0];

        return GetIidEntries(runtime);
    }

    private static IEnumerable<CustomAttribute> GetAllIidEntries(ModuleDefinition module)
    {
        return GetIidEntries(module).Concat(GetRuntimeIidEntries(module));
    }

    private static Guid GetProviderIid(ModuleDefinition module, string key)
    {
        CustomAttribute entry = GetAllIidEntries(module).Single(entry => entry.Signature!.FixedArguments[0].Element!.ToString() == key);

        return GetProviderIid(entry, module.RuntimeContext!);
    }

    private static Guid GetProviderIid(CustomAttribute entry, RuntimeContext context)
    {
        TypeDefinition provider = ((TypeSignature)entry.Signature!.FixedArguments[1].Element!).Resolve(context);

        return Guid.Parse(provider.CustomAttributes.Single().Signature!.FixedArguments[0].Element!.ToString()!);
    }
}
