using System.Threading.Tasks;
using Generator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SourceGeneratorTest.Helpers;

namespace SourceGeneratorTest;

[TestClass]
public class DiagnosticAnalyzerTests
{
    [TestMethod]
    public async Task CollectionExpression_TargetingConcreteType_DoesNotWarn()
    {
        const string source = """
            using System.Collections.Generic;

            class Test
            {
                void M()
                {
                    List<int> a = [];
                    List<int> b = [1, 2, 3];
                    int[] c = [];
                    int[] d = [1, 2, 3];
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_Empty_DoesNotWarn()
    {
        const string source = """
            using System.Collections.Generic;

            class Test
            {
                void M()
                {
                    IEnumerable<int> a = [];
                    ICollection<int> b = [];
                    IReadOnlyCollection<int> c = [];
                    IList<int> d = [];
                    IReadOnlyList<int> e = [];
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_Mutable_NotEmpty_DoesNotWarn()
    {
        const string source = """
            using System.Collections.Generic;

            class Test
            {
                void M(int x, IEnumerable<int> y)
                {
                    ICollection<int> a = [1, 2, 3];
                    ICollection<int> b = [x];
                    ICollection<int> c = [1, x, ..y];
                    IList<int> d = [1, 2, 3];
                    IList<int> e = [x];
                    IList<int> f = [1, x, ..y];
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_WithCollectionBuilder_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            class Test
            {
                void M(int x, IEnumerable<int> y)
                {
                    IMyInterface<int> a = [];
                    IMyInterface<int> b = [1, 2, 3];
                    IMyInterface<int> c = [x];
                    IMyInterface<int> d = [1, x, ..y];
                }
            }

            [CollectionBuilder(typeof(MyInterfaceBuilder), nameof(MyInterfaceBuilder.Create))]
            interface IMyInterface<T> : IEnumerable<T>
            {
            }

            class MyInterface<T> : IMyInterface<T>
            {
                public IEnumerator<T> GetEnumerator()
                {
                    throw new NotImplementedException();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    throw new NotImplementedException();
                }
            }

            class MyInterfaceBuilder
            {
                public static IMyInterface<T> Create<T>(ReadOnlySpan<T> span)
                {
                    return new MyInterface<T>();
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_ReadOnly_NotEmpty_LocalOnly_DoesNotWarn()
    {
        const string source = """
            using System.Collections.Generic;

            class Test
            {
                void M(int x, IEnumerable<int> y)
                {
                    IEnumerable<int> a = [1, 2, 3];
                    IEnumerable<int> b = [x];
                    IEnumerable<int> c = [1, x, ..y];
                    IReadOnlyCollection<int> d = [1, 2, 3];
                    IReadOnlyCollection<int> e = [x];
                    IReadOnlyCollection<int> f = [1, x, ..y];
                    IReadOnlyList<int> g = [1, 2, 3];
                    IReadOnlyList<int> h = [x];
                    IReadOnlyList<int> i = [1, x, ..y];
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_ReadOnly_NotEmpty_WithMultipleBuilderTypes_Warns()
    {
        const string source = """
            using System.Collections.Generic;

            namespace MyApp
            {
                class Test
                {
                    void M(int x, IEnumerable<int> y)
                    {
                        IEnumerable<int> a = {|CsWinRT1032:[1, 2, 3]|};
                        new RuntimeClass().SetItems(a);
                    }
                }

                [WinRT.WindowsRuntimeType]
                class RuntimeClass
                {
                    public void SetItems(IEnumerable<int> value)
                    {
                    }
                }
            }

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
                internal sealed class CollectionBuilderAttribute : Attribute
                {
                    public CollectionBuilderAttribute(Type builderType, string methodName)
                    {
                        BuilderType = builderType;
                        MethodName = methodName;
                    }

                    public Type BuilderType { get; }
                    public string MethodName { get; }
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_ReadOnly_NotEmpty_FlowingToWinRT_Warns()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class Test
            {
                private IEnumerable<int> stored = {|CsWinRT1032:[1]|};
                private IEnumerable<int> StoredProperty { get; set; } = {|CsWinRT1032:[2]|};

                void Direct(RuntimeClass api)
                {
                    api.SetItems({|CsWinRT1032:[3]|});
                }

                void ThroughAliases(RuntimeClass api)
                {
                    IEnumerable<int> value = {|CsWinRT1032:[4]|};
                    IEnumerable<int> alias = value;
                    api.SetItems(alias);
                }

                static IEnumerable<int> Create()
                {
                    return {|CsWinRT1032:[5]|};
                }

                void ThroughReturn(RuntimeClass api)
                {
                    api.SetItems(Create());
                }

                void ThroughStorage(RuntimeClass api)
                {
                    api.SetItems(stored);
                    api.SetItems(StoredProperty);
                }

                void ThroughForwarder(RuntimeClass api)
                {
                    Forward(api, {|CsWinRT1032:[6]|});
                }

                static void Forward(RuntimeClass api, IEnumerable<int> value)
                {
                    api.SetItems(value);
                }

                void ThroughDelegate(RuntimeClass api)
                {
                    Func<IEnumerable<int>> factory = () => {|CsWinRT1032:[7]|};
                    api.SetItems(factory());
                }

                static async Task<IEnumerable<int>> CreateAsync()
                {
                    await Task.Yield();
                    return {|CsWinRT1032:[8]|};
                }

                async Task ThroughAsyncReturn(RuntimeClass api)
                {
                    api.SetItems(await CreateAsync());
                }

                void ThroughArrayStorage(RuntimeClass api)
                {
                    IEnumerable<int>[] values = new IEnumerable<int>[1];
                    values[0] = {|CsWinRT1032:[9]|};
                    api.SetItems(values[0]);
                }

                void ThroughWinRTProperty(RuntimeClass api)
                {
                    api.Items = {|CsWinRT1032:[10]|};
                }

                void ThroughDelegateParameter(RuntimeClass api)
                {
                    Action<IEnumerable<int>> forward = value => api.SetItems(value);
                    forward({|CsWinRT1032:[11]|});
                }

                void ThroughMethodGroupParameter(RuntimeClass api)
                {
                    Action<IEnumerable<int>> forward = api.SetItems;
                    forward({|CsWinRT1032:[12]|});
                }

                static void CreateOut(out IEnumerable<int> value)
                {
                    value = {|CsWinRT1032:[13]|};
                }

                void ThroughOutParameter(RuntimeClass api)
                {
                    CreateOut(out IEnumerable<int> value);
                    api.SetItems(value);
                }

                static void CreateRef(ref IEnumerable<int> value)
                {
                    value = {|CsWinRT1032:[14]|};
                }

                void ThroughRefParameter(RuntimeClass api)
                {
                    IEnumerable<int> value = Array.Empty<int>();
                    CreateRef(ref value);
                    api.SetItems(value);
                }

                void ThroughArrayAlias(RuntimeClass api)
                {
                    IEnumerable<int>[] original = new IEnumerable<int>[1];
                    IEnumerable<int>[] alias = original;
                    alias[0] = {|CsWinRT1032:[15]|};
                    api.SetItems(original[0]);
                }

                void ThroughForwardedDelegate(RuntimeClass api)
                {
                    Action<IEnumerable<int>> forward = value => api.SetItems(value);
                    Invoke(forward, {|CsWinRT1032:[16]|});
                }

                static void Invoke(Action<IEnumerable<int>> action, IEnumerable<int> value)
                {
                    action(value);
                }

                void ThroughLocalFunction(RuntimeClass api)
                {
                    static IEnumerable<int> Create()
                    {
                        return {|CsWinRT1032:[17]|};
                    }

                    api.SetItems(Create());
                }

                void ThroughInterfaceReturn(RuntimeClass api, IFactory factory)
                {
                    api.SetItems(factory.Create());
                }

                void ThroughInterfaceParameter(IForwarder forwarder)
                {
                    forwarder.Forward({|CsWinRT1032:[19]|});
                }

                void ThroughManagedPropertySetter(PropertyForwarder forwarder)
                {
                    forwarder.Items = {|CsWinRT1032:[20]|};
                }

                void ThroughDelegateField(DelegateForwarder forwarder)
                {
                    forwarder.Forward({|CsWinRT1032:[21]|});
                }

                void ThroughReturnedDelegate(RuntimeClass api)
                {
                    GetForwarder(api)({|CsWinRT1032:[22]|});
                }

                static Action<IEnumerable<int>> GetForwarder(RuntimeClass api)
                {
                    return api.SetItems;
                }

                void ThroughArrayFieldAlias(RuntimeClass api, ArrayStorage storage)
                {
                    IEnumerable<int>[] original = new IEnumerable<int>[1];
                    storage.Values = original;
                    storage.Values[0] = {|CsWinRT1032:[23]|};
                    api.SetItems(original[0]);
                }

                void ThroughInterfaceProperty(RuntimeClass api, IPropertyFactory factory)
                {
                    api.SetItems(factory.Items);
                }

                void ThroughCoalesceAssignment(RuntimeClass api)
                {
                    IEnumerable<int> value = null;
                    value ??= {|CsWinRT1032:[25]|};
                    api.SetItems(value);
                }

                void ThroughDelegateCompoundAssignment(RuntimeClass api)
                {
                    Action<IEnumerable<int>> forward = _ => { };
                    forward += api.SetItems;
                    forward({|CsWinRT1032:[26]|});
                }

                void ThroughArrayFieldInitializerAlias(RuntimeClass api)
                {
                    ArrayInitializerStorage.Alias[0] = {|CsWinRT1032:[27]|};
                    api.SetItems(ArrayInitializerStorage.Original[0]);
                }

                void ThroughDelegateFieldInitializerAlias()
                {
                    DelegateInitializerStorage.Alias({|CsWinRT1032:[28]|});
                }

                void ThroughDynamicCall(dynamic api)
                {
                    IEnumerable<int> value = {|CsWinRT1032:[29]|};
                    api.SetItems(value);
                }

            }

            [WinRT.WindowsRuntimeType]
            class RuntimeClass
            {
                public void SetItems(IEnumerable<int> value)
                {
                }

                public IEnumerable<int> Items { get; set; }
            }

            interface IFactory
            {
                IEnumerable<int> Create();
            }

            class Factory : IFactory
            {
                public IEnumerable<int> Create()
                {
                    return {|CsWinRT1032:[18]|};
                }
            }

            interface IForwarder
            {
                void Forward(IEnumerable<int> value);
            }

            class Forwarder : IForwarder
            {
                private readonly RuntimeClass api = new();

                public void Forward(IEnumerable<int> value)
                {
                    api.SetItems(value);
                }
            }

            class PropertyForwarder
            {
                private readonly RuntimeClass api = new();

                public IEnumerable<int> Items
                {
                    set => api.SetItems(value);
                }
            }

            class DelegateForwarder
            {
                public Action<IEnumerable<int>> Forward;

                public DelegateForwarder(RuntimeClass api)
                {
                    Forward = api.SetItems;
                }
            }

            class ArrayStorage
            {
                public IEnumerable<int>[] Values;
            }

            interface IPropertyFactory
            {
                IEnumerable<int> Items { get; }
            }

            class PropertyFactory : IPropertyFactory
            {
                public IEnumerable<int> Items => {|CsWinRT1032:[24]|};
            }

            static class ArrayInitializerStorage
            {
                public static readonly IEnumerable<int>[] Original = new IEnumerable<int>[1];
                public static readonly IEnumerable<int>[] Alias = Original;
            }

            static class DelegateInitializerStorage
            {
                private static readonly RuntimeClass Api = new();
                public static readonly Action<IEnumerable<int>> Original = Api.SetItems;
                public static readonly Action<IEnumerable<int>> Alias = Original;
            }

            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_ReadOnly_NotEmpty_NotFlowingToWinRT_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class Test
            {
                private IEnumerable<int> stored = [1];
                private IEnumerable<int> StoredProperty { get; set; } = [2];

                static IEnumerable<int> Create()
                {
                    return [3];
                }

                static async Task<IEnumerable<int>> CreateAsync()
                {
                    await Task.Yield();
                    return [6];
                }

                void ManagedOnly()
                {
                    IEnumerable<int> value = [4];
                    IEnumerable<int> alias = value;
                    Consume(alias);

                    Func<IEnumerable<int>> factory = () => [5];
                    _ = factory();

                    IEnumerable<int>[] values = new IEnumerable<int>[1];
                    values[0] = [7];
                    _ = values[0];
                }

                static void Consume(IEnumerable<int> value)
                {
                    foreach (int item in value)
                    {
                        _ = item;
                    }
                }
            }

            [WinRT.WindowsRuntimeType]
            class RuntimeClass
            {
                private IEnumerable<int> stored = [8];
                private IEnumerable<int> StoredProperty { get; set; } = [9];

                void ManagedOnly()
                {
                    Helper([10]);
                }

                private static void Helper(IEnumerable<int> value)
                {
                }

                public void SetItems(IEnumerable<int> value)
                {
                }

                public IEnumerable<int> GetItems()
                {
                    Func<IEnumerable<int>> unused = () => [12];
                    return Array.Empty<int>();
                }
            }

            class DelegateTest
            {
                void NestedDelegateReturnDoesNotFlow(RuntimeClass api)
                {
                    Func<IEnumerable<int>> outer = () =>
                    {
                        Func<IEnumerable<int>> nested = () => [11];
                        return Array.Empty<int>();
                    };

                    api.SetItems(outer());
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_ReadOnly_NotEmpty_MethodGroupWinRTSink_Warns()
    {
        const string source = """
            using System;
            using System.Collections.Generic;

            class Test
            {
                void M(RuntimeClass api)
                {
                    Action<IEnumerable<int>> forward = api.SetItems;
                    forward({|CsWinRT1032:[1]|});
                }
            }

            [WinRT.WindowsRuntimeType]
            class RuntimeClass
            {
                public void SetItems(IEnumerable<int> value)
                {
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_TargetingInterface_ReadOnly_BindableProperty_Warns()
    {
        const string source = """
            using System.Collections.Generic;

            [WinRT.GeneratedBindableCustomProperty]
            partial class ViewModel
            {
                public IEnumerable<int> Items { get; } = {|CsWinRT1032:[1]|};
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto")]);
    }

    [TestMethod]
    public async Task CollectionExpression_CsWinRTComponent_PublicRuntimeClassBoundary_Warns()
    {
        const string source = """
            using System.Collections.Generic;

            public sealed class Component
            {
                public void SetItems(IEnumerable<int> value)
                {
                }

                public IEnumerable<int> GetItems()
                {
                    return {|CsWinRT1032:[1]|};
                }
            }

            class Test
            {
                void M(Component component)
                {
                    component.SetItems({|CsWinRT1032:[2]|});

                    var helper = new ManagedHelper();
                    helper.SetItems([3]);
                }
            }

            class ManagedHelper
            {
                public void SetItems(IEnumerable<int> value)
                {
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(
            source,
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTComponent", "true")
            ]);
    }

    [TestMethod]
    public async Task CollectionExpression_ModuleEscapes_WarnAtLevel3()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;

            public class LibraryApi
            {
                public IEnumerable<int> Field = {|CsWinRT1032:[1]|};
                public IEnumerable<int> Property { get; } = {|CsWinRT1032:[2]|};

                public IEnumerable<int> GetItems()
                {
                    return {|CsWinRT1032:[3]|};
                }

                internal IEnumerable<int> GetInternalItems()
                {
                    return [4];
                }

                public int CallExternal()
                {
                    return Enumerable.Count({|CsWinRT1032:[5]|});
                }

                public IEnumerable<int> Deconstructed { get; private set; }
                public IEnumerable<int>[][] Nested { get; } = new IEnumerable<int>[1][];

                private void StoreThroughOtherAssignmentShapes()
                {
                    IEnumerable<int> local;
                    (Deconstructed, local) = ({|CsWinRT1032:[7]|}, [8]);

                    Nested[0] = new IEnumerable<int>[1];
                    Nested[0][0] = {|CsWinRT1032:[9]|};
                }

                private void ManagedOnly()
                {
                    IEnumerable<int> value = [6];
                    Consume(value);

                    System.Action<IEnumerable<int>> localDelegate = _ => { };
                    localDelegate([10]);

                    WriteOnly = [11];
                }

                private static void Consume(IEnumerable<int> value)
                {
                }

                public IEnumerable<int> WriteOnly { private get; set; }

                public void InvokeUnknown(System.Action<IEnumerable<int>> callback)
                {
                    callback({|CsWinRT1032:[12]|});
                }

                private void DeconstructThenEscape()
                {
                    (IEnumerable<int> value, int count) = ({|CsWinRT1032:[13]|}, 0);
                    _ = Enumerable.Count(value);
                }

                private void OperatorThenEscape()
                {
                    _ = new OperatorForwarder() + {|CsWinRT1032:[15]|};
                }
            }

            public interface IExternalContract
            {
                IEnumerable<int> GetItems();
            }

            internal sealed class InternalImplementation : IExternalContract
            {
                public IEnumerable<int> GetItems()
                {
                    return {|CsWinRT1032:[14]|};
                }
            }

            internal sealed class OperatorForwarder
            {
                public static OperatorForwarder operator +(OperatorForwarder forwarder, IEnumerable<int> value)
                {
                    _ = Enumerable.Count(value);
                    return forwarder;
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(
            source,
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTAotWarningLevel", "3")
            ]);
    }

    [TestMethod]
    public async Task CollectionExpression_ModuleEscapes_DoNotWarnAtLevel2()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;

            public class LibraryApi
            {
                public IEnumerable<int> Field = [1];
                public IEnumerable<int> Property { get; } = [2];

                public IEnumerable<int> GetItems()
                {
                    return [3];
                }

                public int CallExternal()
                {
                    return Enumerable.Count([4]);
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(
            source,
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTAotWarningLevel", "2")
            ]);
    }

    [TestMethod]
    public async Task ComImportInterfaceCast_ValidCast_DoesNotWarn()
    {
        const string source = """
            class Test
            {
                void M(object obj)
                {
                    IC c1 = (IC)obj;
                    IC c2 = obj as IC;

                    if (obj is IC)
                    {
                    }

                    if (obj is IC c3)
                    {
                    }

                    if ((object[])obj is [IC c4])
                    {
                    }

                    if ((object[])obj is [IC])
                    {
                    }
                }
            }

            interface IC;
            """;

        await CSharpAnalyzerTest<ComImportInterfaceAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto"), ("EnableAotAnalyzer", "true")]);
    }

    [TestMethod]
    [DataRow("true")]
    [DataRow("false")]
    [DataRow("OptIn")]
    public async Task ComImportInterfaceCast_InvalidCast_NotAutoMode_DoesNotWarn(string propertyValue)
    {
        const string source = """
            using System.Runtime.InteropServices;

            class Test
            {
                void M(object obj)
                {
                    IC c1 = (IC)obj;
                    IC c2 = obj as IC;

                    if (obj is IC)
                    {
                    }

                    if (obj is IC c3)
                    {
                    }

                    if ((object[])obj is [IC c4])
                    {
                    }

                    if ((object[])obj is [IC])
                    {
                    }
                }
            }

            [Guid("8FA8A526-F93B-4891-97D2-E1CC83D1C463")]
            [ComImport]
            interface IC;
            """;

        await CSharpAnalyzerTest<ComImportInterfaceAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", propertyValue), ("EnableAotAnalyzer", "true")]);
    }

    [TestMethod]
    public async Task ComImportInterfaceCast_InvalidCast_NoEnableAotAnalyzer_DoesNotWarn()
    {
        const string source = """
            using System.Runtime.InteropServices;

            class Test
            {
                void M(object obj)
                {
                    IC c1 = (IC)obj;
                    IC c2 = obj as IC;

                    if (obj is IC)
                    {
                    }

                    if (obj is IC c3)
                    {
                    }

                    if ((object[])obj is [IC c4])
                    {
                    }

                    if ((object[])obj is [IC])
                    {
                    }
                }
            }

            [Guid("8FA8A526-F93B-4891-97D2-E1CC83D1C463")]
            [ComImport]
            interface IC;
            """;

        await CSharpAnalyzerTest<ComImportInterfaceAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto"), ("EnableAotAnalyzer", "false")]);
    }

    [TestMethod]
    public async Task ComImportInterfaceCast_InvalidCast_NoProperty_DoesNotWarn()
    {
        const string source = """
            using System.Runtime.InteropServices;

            class Test
            {
                void M(object obj)
                {
                    IC c1 = (IC)obj;
                    IC c2 = obj as IC;

                    if (obj is IC)
                    {
                    }

                    if (obj is IC c3)
                    {
                    }

                    if ((object[])obj is [IC c4])
                    {
                    }

                    if ((object[])obj is [IC])
                    {
                    }
                }
            }

            [Guid("8FA8A526-F93B-4891-97D2-E1CC83D1C463")]
            [ComImport]
            interface IC;
            """;

        await CSharpAnalyzerTest<ComImportInterfaceAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ComImportInterfaceCast_InvalidCast_Warns()
    {
        const string source = """
            using System.Runtime.InteropServices;

            class Test
            {
                void M(object obj)
                {
                    IC c1 = {|CsWinRT1033:(IC)obj|};
                    IC c2 = {|CsWinRT1033:obj as IC|};

                    if ({|CsWinRT1033:obj is IC|})
                    {
                    }

                    if ({|CsWinRT1033:obj is IC c3|})
                    {
                    }

                    if ((object[])obj is [{|CsWinRT1033:IC c4|}])
                    {
                    }

                    if ((object[])obj is [{|CsWinRT1033:IC|}])
                    {
                    }
                }
            }

            [Guid("8FA8A526-F93B-4891-97D2-E1CC83D1C463")]
            [ComImport]
            interface IC;
            """;

        await CSharpAnalyzerTest<ComImportInterfaceAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotOptimizerEnabled", "auto"), ("EnableAotAnalyzer", "true")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_ValidCast_DoesNotWarn()
    {
        const string source = """
            class Test
            {
                void M(object obj)
                {
                    C c1 = (C)obj;
                    C c2 = obj as C;

                    if (obj is C)
                    {
                    }

                    if (obj is C c3)
                    {
                    }

                    if ((object[])obj is [C c4])
                    {
                    }

                    if ((object[])obj is [C])
                    {
                    }
                }
            }

            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("0")]
    [DataRow("1")]
    [DataRow("2")]
    public async Task RuntimeClassCast_InvalidCast_NotLevel3_DoesNotWarn(string propertyValue)
    {
        const string source = """
            using WinRT;

            class Test
            {
                void M(object obj)
                {
                    C c1 = (C)obj;
                    C c2 = obj as C;

                    if (obj is C)
                    {
                    }

                    if (obj is C c3)
                    {
                    }

                    if ((object[])obj is [C c4])
                    {
                    }

                    if ((object[])obj is [C])
                    {
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", propertyValue)]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_NoProperty_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                void M(object obj)
                {
                    C c1 = (C)obj;
                    C c2 = obj as C;

                    if (obj is C)
                    {
                    }

                    if (obj is C c3)
                    {
                    }

                    if ((object[])obj is [C c4])
                    {
                    }

                    if ((object[])obj is [C])
                    {
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task RuntimeClassCast_VerifyNoFalsePositives_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                void M(object obj)
                {
                    A a1 = null;
                    A a2 = (A)null;
                    B b1 = null;
                    B b2 = (B)null;
                    B b3 = (B)(A)null;

                    if (a1 is null)
                    {
                    }

                    if (a1 is not null)
                    {
                    }

                    if (a1 == null)
                    {
                    }

                    if (a1 != null)
                    {
                    }

                    A a3 = new();
                    B b4 = new();

                    a3 = b4;
                    a3 = new B();
                    a3 = (A)b4;

                    object obj2 = (A)b4;

                    if (a3 == b4)
                    {
                    }

                    int i = 42;
                    E e = (E)i;
                    int i2 = (int)e;
                    E e2 = (E)(int)obj;

                    E? ne1 = (E?)null;
                    E? ne2 = (E?)E.A;

                    if (ne1 is E)
                    {
                    }

                    if (ne1 is E e3)
                    {
                    }

                    if ((E?[])obj is [E])
                    {
                    }

                    if ((E?[])obj is [E e4])
                    {
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class A;

            [WindowsRuntimeType("SomeContract")]
            class B : A;

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_Method_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                void M(object obj)
                {
                    C c1 = (C)obj;
                    C c2 = obj as C;

                    if (obj is C)
                    {
                    }

                    if (obj is C c3)
                    {
                    }

                    if ((object[])obj is [C c4])
                    {
                    }

                    if ((object[])obj is [C])
                    {
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_EnumType_WithDynamicWindowsRuntimeCast_Method_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(E))]
                void M(object obj)
                {
                    E e1 = (E)obj;

                    if (obj is E)
                    {
                    }

                    if (obj is E e2)
                    {
                    }

                    if ((object[])obj is [E e3])
                    {
                    }

                    if ((object[])obj is [E])
                    {
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_Lambda_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                void M()
                {
                    Action<object> l = [DynamicWindowsRuntimeCast(typeof(C))] (obj) =>
                    {
                        C c1 = (C)obj;
                        C c2 = obj as C;

                        if (obj is C)
                        {
                        }

                        if (obj is C c3)
                        {
                        }

                        if ((object[])obj is [C c4])
                        {
                        }

                        if ((object[])obj is [C])
                        {
                        }
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_Lambda_AttributeOnParent_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                void M()
                {
                    Action<object> l = obj =>
                    {
                        C c1 = (C)obj;
                        C c2 = obj as C;

                        if (obj is C)
                        {
                        }

                        if (obj is C c3)
                        {
                        }

                        if ((object[])obj is [C c4])
                        {
                        }

                        if ((object[])obj is [C])
                        {
                        }
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_LocalMethod_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                void M()
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    void F(object obj)
                    {
                        C c1 = (C)obj;
                        C c2 = obj as C;

                        if (obj is C)
                        {
                        }

                        if (obj is C c3)
                        {
                        }

                        if ((object[])obj is [C c4])
                        {
                        }

                        if ((object[])obj is [C])
                        {
                        }
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_LocalMethod_AttributeOnParent_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                void M()
                {
                    void F(object obj)
                    {
                        C c1 = (C)obj;
                        C c2 = obj as C;

                        if (obj is C)
                        {
                        }

                        if (obj is C c3)
                        {
                        }

                        if ((object[])obj is [C c4])
                        {
                        }

                        if ((object[])obj is [C])
                        {
                        }
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_LambdaInDictionaryInitializer_AttributeOnParentMethod_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                void M1()
                {
                    var x = new Dictionary<int, Action<object>>
                    {
                        { 42, obj => Console.WriteLine(obj is C) }
                    };
                }

                [DynamicWindowsRuntimeCast(typeof(C))]
                void M2()
                {
                    var x = new Dictionary<int, Action<object>>
                    {
                        [42] = obj => Console.WriteLine(obj is C)
                    };
                }

                [DynamicWindowsRuntimeCast(typeof(C))]
                void M3()
                {
                    var x = new Dictionary<int, (Type, Action<object>)>
                    {
                        { 42, (typeof(int), obj => Console.WriteLine(obj is C)) }
                    };
                }

                [DynamicWindowsRuntimeCast(typeof(C))]
                void M4()
                {
                    var x = new Dictionary<int, (Type, Action<object>)>
                    {
                        [42] = (typeof(int), obj => Console.WriteLine(obj is C))
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_LambdaInDictionaryInitializer_AttributeOnParentField_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                private static readonly Dictionary<int, Action<object>> F1 = new()
                {
                    { 42, obj => Console.WriteLine(obj is C) }
                };

                [DynamicWindowsRuntimeCast(typeof(C))]
                private static readonly Dictionary<int, Action<object>> F2 = new()
                {
                    [42] = obj => Console.WriteLine(obj is C)
                };

                [DynamicWindowsRuntimeCast(typeof(C))]
                private static readonly Dictionary<int, (Type, Action<object>)> F3 = new()
                {
                    { 42, (typeof(int), obj => Console.WriteLine(obj is C)) }
                };

                [DynamicWindowsRuntimeCast(typeof(C))]
                private static readonly Dictionary<int, (Type, Action<object>)> F4 = new Dictionary<int, (Type, Action<object>)>
                {
                    [42] = (typeof(int), obj => Console.WriteLine(obj is C))
                };
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_PropertyAccessors_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                private object _obj;
                private C _c;

                public C P1
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    get => (C)_obj;
                }

                public C P2
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    get => (C)_obj;

                    [DynamicWindowsRuntimeCast(typeof(C))]
                    set => _c = (C)_obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InterpolatedHandlerArgument_DoesNotWarn()
    {
        const string source = """
            using System.Runtime.CompilerServices;
            using WinRT;

            class Test
            {
                public void M()
                {
                    D d = null;

                    d.UseC($"");
                }
            }

            public static class DExtensions
            {
                public static void UseC(this D d, [InterpolatedStringHandlerArgument("d")] ref CHandler handler)
                {
                }
            }

            [InterpolatedStringHandler]
            public ref struct CHandler
            {
                public CHandler(int literalLength, int formattedCount, C arg2)
                {
                }
            }

            [WindowsRuntimeType("SomeContract")]
            public class C;

            [WindowsRuntimeType("SomeContract")]
            public class D : C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicWindowsRuntimeCast_Method_WrongType_Warns()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(string))]
                void M(object obj)
                {
                    C c1 = {|CsWinRT1034:(C)obj|};
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                private object _obj;
                private C _c;

                public C P1 => {|CsWinRT1034:(C)_obj|};

                public C P2
                {
                    get => {|CsWinRT1034:(C)_obj|};
                    set => _c = {|CsWinRT1034:(C)_obj|};
                }

                void M(object obj)
                {
                    C c1 = {|CsWinRT1034:(C)obj|};
                    C c2 = {|CsWinRT1034:obj as C|};

                    if ({|CsWinRT1034:obj is C|})
                    {
                    }

                    if ({|CsWinRT1034:obj is C c3|})
                    {
                    }

                    if ((object[])obj is [{|CsWinRT1034:C c4|}])
                    {
                    }

                    if ((object[])obj is [{|CsWinRT1034:C|}])
                    {
                    }

                    D d1 = {|CsWinRT1034:(D)c1|};
                    D d2 = {|CsWinRT1034:c1 as D|};

                    if ({|CsWinRT1034:c1 is D|})
                    {
                    }

                    if ({|CsWinRT1034:c1 is D d3|})
                    {
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;

            [WindowsRuntimeType("SomeContract")]
            class D : C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_EnumType_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                void M(object obj)
                {
                    E e1 = {|CsWinRT1035:(E)obj|};

                    if ({|CsWinRT1035:obj is E|})
                    {
                    }

                    if ({|CsWinRT1035:obj is E e2|})
                    {
                    }

                    if ((object[])obj is [{|CsWinRT1035:E e3|}])
                    {
                    }

                    if ((object[])obj is [{|CsWinRT1035:E|}])
                    {
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_EnumType_Nullable_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                void M(object obj)
                {
                    E? e1 = {|CsWinRT1035:(E?)obj|};
                }
            }

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchStatement_NoAttribute_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                int M(object obj)
                {
                    switch (obj)
                    {
                        case {|CsWinRT1034:C|}: return 42;
                        default: return 0;
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchStatement_WithAttribute_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                int M(object obj)
                {
                    switch (obj)
                    {
                        case C: return 42;
                        default: return 0;
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchStatement_WithDeclaration_NoAttribute_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                int M(object obj)
                {
                    switch (obj)
                    {
                        case {|CsWinRT1034:C c|}: return c.GetHashCode();
                        default: return 0;
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchStatement_WithDeclaration_WithAttribute_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                int M(object obj)
                {
                    switch (obj)
                    {
                        case C c: return c.GetHashCode();
                        default: return 0;
                    }
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_NoAttribute_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                int M(object obj)
                {
                    return obj switch
                    {
                        {|CsWinRT1034:C|} => 42,
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithAttribute_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                int M(object obj)
                {
                    return obj switch
                    {
                        C => 42,
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithCondition_NoAttribute_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                int M(object obj)
                {
                    return obj switch
                    {
                        { } when {|CsWinRT1034:obj is C|} => 42,
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithCondition_WithAttribute_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                int M(object obj)
                {
                    return obj switch
                    {
                        { } when obj is C => 42,
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithConditionAndDeclaration_NoAttribute_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                int M(object obj)
                {
                    return obj switch
                    {
                        { } when {|CsWinRT1034:obj is C c|} => c.GetHashCode(),
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithConditionAndDeclaration_WithAttribute_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                int M(object obj)
                {
                    return obj switch
                    {
                        { } when obj is C c => c.GetHashCode(),
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithTuple_NoAttribute_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                int M(object obj)
                {
                    return obj switch
                    {
                        ({|CsWinRT1034:C|}, _) => 42,
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithTuple_WithAttribute_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                int M(object obj)
                {
                    return obj switch
                    {
                        (C, _) => 42,
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithTuple2_NoAttribute_Warns()
    {
        const string source = """
            using WinRT;

            class Test
            {
                int M(object obj, object obj2)
                {
                    return (obj, obj2) switch
                    {
                        ({|CsWinRT1034:C c|}, _) => c.GetHashCode(),
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_SwitchExpression_WithTuple2_WithAttribute_DoesNotWarn()
    {
        const string source = """
            using WinRT;

            class Test
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                int M(object obj, object obj2)
                {
                    return (obj, obj2) switch
                    {
                        (C c, _) => c.GetHashCode(),
                        _ => 0
                    };
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task AotWarningSuppressedInterfaces_OnlyImplementsSuppressedInterface_DoesNotWarn()
    {
        const string source = """
            using System;

            class Test : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """;

        await CSharpAnalyzerTest<WinRT.SourceGenerator.WinRTAotDiagnosticAnalyzer>.VerifyAnalyzerAsync(
            source,
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTAotWarningLevel", "2")
            ],
            analyzerConfigOptions:
            [
                ("cswinrt_aot_warning_suppressed_interfaces", "System.IDisposable")
            ]);
    }

    [TestMethod]
    public async Task AotWarningSuppressedInterfaces_NotConfigured_OnlyImplementsIDisposable_Warns()
    {
        const string source = """
            using System;

            class {|CsWinRT1028:Test|} : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """;

        await CSharpAnalyzerTest<WinRT.SourceGenerator.WinRTAotDiagnosticAnalyzer>.VerifyAnalyzerAsync(
            source,
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTAotWarningLevel", "2")
            ]);
    }

    [TestMethod]
    public async Task AotWarningSuppressedInterfaces_AlsoImplementsNonSuppressedInterface_Warns()
    {
        const string source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class {|CsWinRT1028:Test|} : IDisposable, IEnumerable<int>
            {
                public void Dispose()
                {
                }

                public IEnumerator<int> GetEnumerator() => throw null;

                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            """;

        await CSharpAnalyzerTest<WinRT.SourceGenerator.WinRTAotDiagnosticAnalyzer>.VerifyAnalyzerAsync(
            source,
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTAotWarningLevel", "2")
            ],
            analyzerConfigOptions:
            [
                ("cswinrt_aot_warning_suppressed_interfaces", "System.IDisposable")
            ]);
    }

    [TestMethod]
    public async Task AotWarningSuppressedInterfaces_ScopedToFolder_OnlySuppressesInThatFolder()
    {
        const string suppressedSource = """
            using System;

            class Suppressed : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """;

        const string otherSource = """
            using System;

            class {|CsWinRT1028:Other|} : IDisposable
            {
                public void Dispose()
                {
                }
            }
            """;

        // The scoped .editorconfig only applies to files under the 'Suppressed' folder, so the type declared
        // there does not warn while the one under the 'Other' folder still does.
        const string scopedEditorConfig = """
            [*.cs]
            cswinrt_aot_warning_suppressed_interfaces = System.IDisposable
            """;

        await CSharpAnalyzerTest<WinRT.SourceGenerator.WinRTAotDiagnosticAnalyzer>.VerifyAnalyzerAsync(
            sources:
            [
                ("/Suppressed/Suppressed.cs", suppressedSource),
                ("/Other/Other.cs", otherSource)
            ],
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTAotWarningLevel", "2")
            ],
            scopedEditorConfigs:
            [
                ("/Suppressed/.editorconfig", scopedEditorConfig)
            ]);
    }

    [TestMethod]
    public async Task AotWarningSuppressedInterfaces_GenericCollection_RequiresAllTransitiveMappedInterfaces()
    {
        const string source = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            sealed class TestDisposable : IDisposable, IList<int>
            {
                public void Dispose() { }
                public int this[int index] { get => throw null; set { } }
                public int Count => 0;
                public bool IsReadOnly => false;
                public void Add(int item) { }
                public void Clear() { }
                public bool Contains(int item) => false;
                public void CopyTo(int[] array, int arrayIndex) { }
                public IEnumerator<int> GetEnumerator() => throw null;
                public int IndexOf(int item) => 0;
                public void Insert(int index, int item) { }
                public bool Remove(int item) => false;
                public void RemoveAt(int index) { }
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }
            """;

        // Implementing IList<int> transitively implements IEnumerable<int> and IEnumerable, which are also custom
        // mapped WinRT interfaces, so all of them (along with IDisposable) need to be listed to suppress the warning.
        // The list is comma separated.
        await CSharpAnalyzerTest<WinRT.SourceGenerator.WinRTAotDiagnosticAnalyzer>.VerifyAnalyzerAsync(
            source,
            editorconfig:
            [
                ("CsWinRTAotOptimizerEnabled", "auto"),
                ("CsWinRTAotWarningLevel", "2")
            ],
            analyzerConfigOptions:
            [
                ("cswinrt_aot_warning_suppressed_interfaces", "System.IDisposable, System.Collections.Generic.IList<int>, System.Collections.Generic.IEnumerable<int>, System.Collections.IEnumerable")
            ]);
    }
}