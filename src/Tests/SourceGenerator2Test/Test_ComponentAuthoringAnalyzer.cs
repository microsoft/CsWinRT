// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<ComponentAuthoringAnalyzer>;

/// <summary>
/// Tests for the Windows Runtime component API surface diagnostics.
/// </summary>
[TestClass]
public sealed class Test_ComponentAuthoringAnalyzer
{
    [TestMethod]
    public async Task OrdinaryLibrary_DoesNotReportAuthoringRestrictions()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            public class Generic<T>
            {
                public System.Collections.Generic.List<T> Values { private get; set; }
                public void Method(ref int[] array) { }
            }
            """);
    }

    [TestMethod]
    public async Task EmptyComponent_DoesNotWarn()
    {
        await VerifyAsync("");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedMappings_AreValidInBothXamlModes(bool useWindowsUIXamlProjections)
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace Component;
            public interface IContract
            {
                System.ComponentModel.INotifyPropertyChanged GetChanges();
                System.Windows.Input.ICommand GetCommand();
                System.Numerics.Vector3 GetVector();
            }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            useWindowsUIXamlProjections: useWindowsUIXamlProjections);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WinUIOnlyMapping_IsNotAvailableInUwp(bool useWindowsUIXamlProjections)
    {
        string method = useWindowsUIXamlProjections ? "{|CSWINRT2033:GetServiceProvider|}" : "GetServiceProvider";

        await VerifyCS.VerifyAnalyzerAsync($$"""
            namespace Component;
            public interface IContract
            {
                System.IServiceProvider {{method}}();
            }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            useWindowsUIXamlProjections: useWindowsUIXamlProjections);
    }

    [TestMethod]
    public async Task ValidSurface_DoesNotWarn()
    {
        await VerifyAsync("""
            using System;
            using System.Collections.Generic;
            namespace Component;
            public enum Choice { One, Two }
            public struct Data
            {
                public int Value;
                public Guid Id;
                public int? Optional;
                public DateTimeOffset Timestamp;
                public string Text;
                public Choice Choice;
                public int Helper => Value;
                public void Reset() => Value = 0;
            }
            public delegate Data Callback(Data value);
            public interface IContract
            {
                Data Get(ReadOnlySpan<int> input, Span<string> buffer, out Data[] result);
                void Query(ref Guid id);
                IList<string> Items { get; }
                event EventHandler<int> Changed;
            }
            public sealed class Service
            {
                public Service() { }
                public Service(int value) { }
                public int Value { get; set; }
                public static int StaticValue { get; }
                public void DifferentArity() { }
                public void DifferentArity(int value) { }
                public void Fill(Span<int> values) { }
                public void Read(ReadOnlySpan<int> values) { }
                public void Receive(out int[] values) => values = null;
                public IList<KeyValuePair<string, int>> GetItems() => null;
                public Windows.Foundation.IAsyncAction WorkAsync() => null;
            }
            """);
    }

    [TestMethod]
    public async Task NonExportedDeclarations_DoesNotWarn()
    {
        await VerifyAsync("""
            namespace Other
            {
                internal class Internal<T> { }
            }
            namespace ABI.Other
            {
                public class Helper<T> { }
            }
            namespace Component
            {
                public sealed class Outer
                {
                    public class Nested<T> { }
                }
                [WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(Outer))]
                public abstract class Factory
                {
                    public System.Collections.Generic.List<int> Helper() => null;
                }
            }
            """);
    }

    [TestMethod]
    public async Task ImplementedExistingRuntimeClass_DoesNotWarn()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace Other;
            public class Implementation : ABI.External.Widget
            {
                public System.Collections.Generic.List<int> Helper() => null;
            }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            referenceProjectionSource: """
                [assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyMetadata(
                    "CsWinRT.ImplementableClass.v1", "External.Widget")]
                namespace External
                {
                    public sealed class Widget { }
                }
                namespace ABI.External
                {
                    public abstract class Widget { }
                }
                """);
    }

    [TestMethod]
    public async Task GeneratedPartialMembers_UnsupportedSignaturesWarn()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace Component;
            public sealed partial class Service { }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            generatedSource: """
                namespace Component;
                public sealed partial class Service
                {
                    public System.Collections.Generic.List<int> {|CSWINRT2033:Generated|}() => null;
                }
                """);
    }

    [TestMethod]
    public async Task GeneratedPublicType_IsValidated()
    {
        await VerifyCS.VerifyAnalyzerAsync("",
            isCsWinRTComponent: true, assemblyName: "Component",
            generatedSource: """
                // <auto-generated/>
                namespace Component;
                public class {|CSWINRT2032:GeneratedService|} { }
                """);
    }

    [TestMethod]
    public async Task GeneratedCodeAttribute_DoesNotExcludeExportedType()
    {
        await VerifyAsync("""
            namespace Component;
            [System.CodeDom.Compiler.GeneratedCode("TestGenerator", "1.0")]
            public class {|CSWINRT2032:GeneratedService|} { }
            """);
    }

    [TestMethod]
    public async Task GeneratedStructField_SatisfiesNonemptyRequirement()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace Component;
            public partial struct Data { }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            generatedSource: """
                namespace Component;
                public partial struct Data
                {
                    public int Value;
                }
                """);
    }

    [TestMethod]
    public async Task GeneratedStructField_InvalidTypeWarns()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace Component;
            public partial struct Data { }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            generatedSource: """
                namespace Component;
                public partial struct Data
                {
                    public object {|CSWINRT2038:Value|};
                }
                """);
    }

    [TestMethod]
    public async Task GeneratedOverload_ContributesToDefaultOverloadRequirement()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace Component;
            public sealed partial class Service
            {
                public void {|CSWINRT2041:Use|}(int value) { }
            }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            generatedSource: """
                namespace Component;
                public sealed partial class Service
                {
                    public void Use(string value) { }
                }
                """);
    }

    [TestMethod]
    public async Task GeneratedDefaultOverload_IsHonored()
    {
        await VerifyCS.VerifyAnalyzerAsync("""
            namespace Component;
            public sealed partial class Service
            {
                public void Use(int value) { }
                public void Use(bool value) { }
            }
            """,
            isCsWinRTComponent: true, assemblyName: "Component",
            generatedSource: """
                namespace Component;
                public sealed partial class Service
                {
                    [Windows.Foundation.Metadata.DefaultOverload]
                    public void Use(string value) { }
                }
                """);
    }

    [TestMethod]
    public async Task GeneratedImplementationScaffolding_IsStillExcluded()
    {
        await VerifyCS.VerifyAnalyzerAsync("",
            isCsWinRTComponent: true, assemblyName: "Component",
            generatedSource: """
                namespace ABI.Other
                {
                    public class Helper<T> { }
                }
                namespace Component
                {
                    public sealed class Service
                    {
                        public class Nested<T> { }
                    }
                    [WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(Service))]
                    public abstract class Factory { }
                    internal class InternalHelper<T> { }
                }
                """);
    }

    [TestMethod]
    [DataRow("public int {|CSWINRT2028:Value|} { set { } }")]
    [DataRow("public int {|CSWINRT2028:Value|} { private get; set; }")]
    public async Task MissingPublicGetter_Warns(string member)
    {
        await VerifyAsync($$"""
            namespace Component;
            public sealed class Service { {{member}} }
            """);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("namespace Other;")]
    [DataRow("namespace ComponentExtra;")]
    public async Task InvalidNamespace_Warns(string ns)
    {
        await VerifyAsync($$"""
            {{ns}}
            public sealed class {|CSWINRT2029:Service|} { }
            """);
    }

    [TestMethod]
    public async Task NamespaceCaseCollision_ExaminesPrefixes()
    {
        await VerifyAsync("""
            namespace Component.Foo.A { public sealed class First { } }
            namespace Component.foo.B { public sealed class {|CSWINRT2030:Second|} { } }
            """);
    }

    [TestMethod]
    public async Task RepeatedNamespaceAndPartialDeclarations_DoNotWarn()
    {
        await VerifyAsync("""
            namespace Component.Sub { public sealed partial class Service { } }
            namespace Component.Sub { public sealed partial class Service { } }
            """);
    }

    [TestMethod]
    [DataRow("public sealed class {|CSWINRT2031:Generic|}<T> { }")]
    [DataRow("public interface {|CSWINRT2031:Generic|}<T> { }")]
    [DataRow("public struct {|CSWINRT2031:Generic|}<T> { public T Value; }")]
    [DataRow("public delegate void {|CSWINRT2031:Generic|}<T>(T value);")]
    [DataRow("public sealed class Service { public T {|CSWINRT2031:Method|}<T>(T value) => value; }")]
    public async Task GenericDeclaration_Warns(string declaration)
    {
        await VerifyAsync("namespace Component;\n" + declaration);
    }

    [TestMethod]
    [DataRow("class")]
    [DataRow("abstract class")]
    [DataRow("record")]
    public async Task UnsealedAuthoredClass_Warns(string kind)
    {
        await VerifyAsync($"namespace Component; public {kind} {{|CSWINRT2032:Service|}} {{ }}");
    }

    [TestMethod]
    [DataRow("System.Collections.Generic.List<int>")]
    [DataRow("System.Collections.Generic.Dictionary<string, int>")]
    [DataRow("System.Array")]
    [DataRow("System.Collections.Generic.IList<System.Collections.Generic.List<int>>")]
    [DataRow("System.Collections.Generic.IList<int[]>")]
    [DataRow("System.Threading.Tasks.Task")]
    [DataRow("decimal")]
    [DataRow("sbyte")]
    [DataRow("System.Span<int>")]
    public async Task UnsupportedReturnType_Warns(string type)
    {
        await VerifyAsync($$"""
            namespace Component;
            public interface IContract
            {
                {{type}} {|CSWINRT2033:Get|}();
            }
            """);
    }

    [TestMethod]
    public async Task UnsupportedParameterPropertyAndDelegateTypes_Warn()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract
            {
                void {|CSWINRT2033:Use|}(System.Collections.Generic.List<int> values);
                decimal {|CSWINRT2033:Value|} { get; }
            }
            public delegate decimal {|CSWINRT2033:Callback|}();
            """);
    }

    [TestMethod]
    public async Task StructShape_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public struct {|CSWINRT2034:Empty|} { }
            public struct Data
            {
                public int Valid;
                private int {|CSWINRT2036:_hidden|};
                public const int {|CSWINRT2037:Constant|} = 1;
                public object {|CSWINRT2038:Object|};
                public int[] {|CSWINRT2038:Array|};
                public System.Collections.Generic.KeyValuePair<int, int> {|CSWINRT2038:Pair|};
            }
            """);
    }

    [TestMethod]
    public async Task ConstructorsAcrossPartialDeclarations_Warn()
    {
        await VerifyAsync("""
            namespace Component;
            public sealed partial class Service
            {
                public Service(int value) { }
            }
            public sealed partial class Service
            {
                public {|CSWINRT2035:Service|}(string value) { }
                private Service(bool value) { }
            }
            """);
    }

    [TestMethod]
    public async Task OperatorAndConversion_Warn()
    {
        await VerifyAsync("""
            namespace Component;
            public sealed class Service
            {
                public static Service operator {|CSWINRT2039:+|}(Service left, Service right) => left;
                public static implicit operator {|CSWINRT2039:int|}(Service value) => 0;
            }
            """);
    }

    [TestMethod]
    public async Task MissingDefaultOverloadAcrossPartialDeclarations_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public sealed partial class Service
            {
                public void {|CSWINRT2041:Use|}(int value) { }
            }
            public sealed partial class Service
            {
                public void Use(string value) { }
            }
            """);
    }

    [TestMethod]
    public async Task DuplicateDefaultOverload_Warns()
    {
        await VerifyAsync("""
            using Windows.Foundation.Metadata;
            namespace Component;
            public interface IContract
            {
                [DefaultOverload] void Use(int value);
                [DefaultOverload] void {|CSWINRT2040:Use|}(string value);
            }
            """);
    }

    [TestMethod]
    public async Task DefaultOverloads_AreGroupedByInputArityAndStaticness()
    {
        await VerifyAsync("""
            using Windows.Foundation.Metadata;
            namespace Component;
            public sealed class Service
            {
                [DefaultOverload] public void Use(int value) { }
                public void Use(string value, out int result) => result = 0;
                [DefaultOverload] public void Use(int first, int second) { }
                public void Use(string first, string second) { }
                public static void Use(bool value) { }
            }
            """);
    }

    [TestMethod]
    public async Task DefaultOverloadOnImplementedInterface_IsHonored()
    {
        await VerifyAsync("""
            using Windows.Foundation.Metadata;
            namespace Component;
            public interface IContract
            {
                [DefaultOverload] void Use(int value);
                void Use(string value);
            }
            public sealed class Service : IContract
            {
                public void Use(int value) { }
                public void Use(string value) { }
            }
            """);
    }

    [TestMethod]
    [DataRow("int[][]", "CSWINRT2042")]
    [DataRow("int[,]", "CSWINRT2043")]
    [DataRow("int[,,]", "CSWINRT2043")]
    public async Task UnsupportedArrayShape_Warns(string type, string diagnostic)
    {
        await VerifyAsync($$"""
            namespace Component;
            public interface IContract { {{type}} {|{{diagnostic}}:Get|}(); }
            """);
    }

    [TestMethod]
    public async Task SpanOfArrays_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract { void {|CSWINRT2042:Use|}(System.ReadOnlySpan<int[]> values); }
            """);
    }

    [TestMethod]
    [DataRow("ref int[]", "CSWINRT2045")]
    [DataRow("in int[]", "CSWINRT2045")]
    [DataRow("out System.Span<int>", "CSWINRT2046")]
    [DataRow("ref System.ReadOnlySpan<int>", "CSWINRT2046")]
    public async Task UnsupportedByReferenceArrayConvention_Warns(string type, string diagnostic)
    {
        await VerifyAsync($$"""
            namespace Component;
            public interface IContract { void Use({{type}} {|{{diagnostic}}:values|}); }
            """);
    }

    [TestMethod]
    [DataRow("System.Collections.CollectionBase")]
    [DataRow("System.Collections.DictionaryBase")]
    public async Task AbstractManagedBaseClass_DoesNotWarn(string baseType)
    {
        await VerifyAsync($$"""
            namespace Component;
            public sealed class Service : {{baseType}} { }
            """);
    }

    [TestMethod]
    [DataRow("System.Exception")]
    [DataRow("System.Collections.ObjectModel.Collection<int>")]
    public async Task UnsupportedBaseClass_Warns(string baseType)
    {
        await VerifyAsync($$"""
            namespace Component;
            public sealed class {|CSWINRT2044:Service|} : {{baseType}} { }
            """);
    }

    [TestMethod]
    public async Task UnsupportedExternalInterface_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public interface {|CSWINRT2044:IContract|} : System.ICloneable { }
            """);
    }

    [TestMethod]
    public async Task OnlyMappedAndOmittedInterfaceMembers_AreExcluded()
    {
        await VerifyAsync("""
            using System.Collections.Generic;
            namespace Component;
            public sealed class Service : System.IEquatable<List<int>>, System.IDisposable
            {
                public bool Equals(List<int> value) => true;
                public void Dispose() { }
                public bool {|CSWINRT2033:Compare|}(List<int> value) => false;
            }
            """);
    }

    [TestMethod]
    [DataRow("__retval")]
    [DataRow("thisReference")]
    [DataRow("thisPtr")]
    [DataRow("ThisPtr")]
    [DataRow("__return_value__")]
    public async Task ReservedParameterName_Warns(string name)
    {
        await VerifyAsync($$"""
            namespace Component;
            public interface IContract { int Get(int {|CSWINRT2047:{{name}}|}); }
            """);
    }

    [TestMethod]
    public async Task DerivedParameterNameCollision_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract { void Get(string {|CSWINRT2047:value|}, int __value); }
            """);
    }

    [TestMethod]
    public async Task ArrayLengthParameterNameCollision_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract
            {
                void Get(System.Span<int> {|CSWINRT2047:values|}, int __valuesSize);
            }
            """);
    }

    [TestMethod]
    public async Task PrimitiveParameterAndUnusedReturnNames_DoNotWarn()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract
            {
                void Get(int value, int __value, int __valueSize, int __retval, int __return_value__);
            }
            """);
    }

    [TestMethod]
    public async Task RefReturn_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract { ref int {|CSWINRT2033:Get|}(); }
            """);
    }

    [TestMethod]
    public async Task CustomAttributeBase_DoesNotWarn()
    {
        await VerifyAsync("""
            namespace Component;
            public sealed class CustomAttribute : System.Attribute
            {
                public CustomAttribute(System.Type type) { }
            }
            """);
    }

    [TestMethod]
    public async Task InternalImplementationInterfaces_DoNotWarn()
    {
        await VerifyAsync("""
            namespace Component;
            internal interface IHelper { void Run(); }
            internal class Outer
            {
                public interface IHidden { int Get(); }
            }
            public sealed class Service : IHelper, Outer.IHidden
            {
                void IHelper.Run() { }
                int Outer.IHidden.Get() => 42;
            }
            """);
    }

    [TestMethod]
    public async Task AsyncInterfaceRequirement_DoesNotWarn()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract : Windows.Foundation.IAsyncAction { }
            """);
    }

    [TestMethod]
    [DataRow("System.DateTimeOffset")]
    [DataRow("System.TimeSpan")]
    [DataRow("Info")]
    [DataRow("in Info")]
    public async Task MappedAndNonBlittableInputNameCollision_Warns(string type)
    {
        await VerifyAsync($$"""
            namespace Component;
            public struct Info { public string Name; }
            public interface IContract
            {
                void Use({{type}} {|CSWINRT2047:value|}, int __value);
            }
            """);
    }

    [TestMethod]
    [DataRow("out string[]", "__items_length")]
    [DataRow("out string[]", "__items_data")]
    [DataRow("System.ReadOnlySpan<string>", "__items_span")]
    [DataRow("System.Span<string>", "__items_inlineArray")]
    [DataRow("System.ReadOnlySpan<string>", "__items_arrayFromPool")]
    [DataRow("System.ReadOnlySpan<string>", "__items_inlineHeaderArray")]
    [DataRow("System.ReadOnlySpan<string>", "_items_inlineHeaderArray")]
    [DataRow("System.ReadOnlySpan<string>", "__items_headerSpan")]
    [DataRow("System.ReadOnlySpan<string>", "__items_pinnedHandleSpan")]
    public async Task ArrayTemporaryNameCollision_Warns(string type, string name)
    {
        await VerifyAsync($$"""
            namespace Component;
            public interface IContract
            {
                void Use({{type}} {|CSWINRT2047:items|}, int {{name}});
            }
            """);
    }

    [TestMethod]
    [DataRow("__arg_values")]
    [DataRow("ConvertToUnmanaged_values")]
    [DataRow("ConvertToManaged_arg_values")]
    public async Task GenericInputNameCollision_Warns(string name)
    {
        await VerifyAsync($$"""
            namespace Component;
            public interface IContract
            {
                void Use(System.Collections.Generic.IList<int> {|CSWINRT2047:values|}, int {{name}});
            }
            """);
    }

    [TestMethod]
    public async Task TypeInputPinNameCollision_Warns()
    {
        await VerifyAsync("""
            namespace Component;
            public interface IContract
            {
                void Use(System.Type {|CSWINRT2047:type|}, int _type);
            }
            """);
    }

    [TestMethod]
    public async Task BlittableStructAndArrayUnusedTemporaryNames_DoNotWarn()
    {
        await VerifyAsync("""
            namespace Component;
            public struct Info { public int Value; }
            public interface IContract
            {
                void Use(Info info, int __info, System.Span<int> items, int __items_span);
                void Fill(System.Span<string> values, int __values_headerSpan);
                void Receive(out int[] values, int _values);
            }
            """);
    }

    [TestMethod]
    public async Task LegacyDirectionAttributes_AreNotDiagnosed()
    {
        await VerifyAsync("""
            using System.Runtime.InteropServices;
            namespace Component;
            public interface IContract
            {
                void Use([In] System.ReadOnlySpan<int> input, [Out] System.Span<int> buffer);
                void Other([In] int input, [Out] int output);
            }
            """);
    }

    private static Task VerifyAsync(string source)
    {
        return VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true, assemblyName: "Component");
    }
}