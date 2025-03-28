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
    public async Task CollectionExpression_TargetingInterface_ReadOnly_NotEmpty_Warns()
    {
        const string source = """
            using System.Collections.Generic;

            class Test
            {
                void M(int x, IEnumerable<int> y)
                {
                    IEnumerable<int> a = {|CsWinRT1032:[1, 2, 3]|};
                    IEnumerable<int> b = {|CsWinRT1032:[x]|};
                    IEnumerable<int> c = {|CsWinRT1032:[1, x, ..y]|};
                    IReadOnlyCollection<int> d = {|CsWinRT1032:[1, 2, 3]|};
                    IReadOnlyCollection<int> e = {|CsWinRT1032:[x]|};
                    IReadOnlyCollection<int> f = {|CsWinRT1032:[1, x, ..y]|};
                    IReadOnlyList<int> g = {|CsWinRT1032:[1, 2, 3]|};
                    IReadOnlyList<int> h = {|CsWinRT1032:[x]|};
                    IReadOnlyList<int> i = {|CsWinRT1032:[1, x, ..y]|};
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
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class A;

            [WindowsRuntimeType("SomeContract")]
            class B : A;
            """;

        await CSharpAnalyzerTest<RuntimeClassCastAnalyzer>.VerifyAnalyzerAsync(source, editorconfig: [("CsWinRTAotWarningLevel", "3")]);
    }

    [TestMethod]
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_Method_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
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
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_Lambda_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                void M()
                {
                    Action<object> l = [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))] (obj) =>
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
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_Lambda_AttributeOnParent_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
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
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_LocalMethod_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                void M()
                {
                    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
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
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_LocalMethod_AttributeOnParent_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
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
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_LambdaInDictionaryInitializer_AttributeOnParentMethod_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
                void M1()
                {
                    var x = new Dictionary<int, Action<object>>
                    {
                        { 42, obj => Console.WriteLine(obj is C) }
                    };
                }

                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
                void M2()
                {
                    var x = new Dictionary<int, Action<object>>
                    {
                        [42] = obj => Console.WriteLine(obj is C)
                    };
                }

                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
                void M3()
                {
                    var x = new Dictionary<int, (Type, Action<object>)>
                    {
                        { 42, (typeof(int), obj => Console.WriteLine(obj is C)) }
                    };
                }

                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
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
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_LambdaInDictionaryInitializer_AttributeOnParentField_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
                private static readonly Dictionary<int, Action<object>> F1 = new()
                {
                    { 42, obj => Console.WriteLine(obj is C) }
                };

                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
                private static readonly Dictionary<int, Action<object>> F2 = new()
                {
                    [42] = obj => Console.WriteLine(obj is C)
                };

                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
                private static readonly Dictionary<int, (Type, Action<object>)> F3 = new()
                {
                    { 42, (typeof(int), obj => Console.WriteLine(obj is C)) }
                };

                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(C))]
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
    public async Task RuntimeClassCast_InvalidCast_WithDynamicDependency_Method_WrongType_Warns()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;
            using WinRT;

            class Test
            {
                [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(string))]
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
}