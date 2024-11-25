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

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source);
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

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source);
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

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source);
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

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source);
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
                    IEnumerable<int> a = {|CsWinRT1031:[1, 2, 3]|};
                    IEnumerable<int> b = {|CsWinRT1031:[x]|};
                    IEnumerable<int> c = {|CsWinRT1031:[1, x, ..y]|};
                    IReadOnlyCollection<int> d = {|CsWinRT1031:[1, 2, 3]|};
                    IReadOnlyCollection<int> e = {|CsWinRT1031:[x]|};
                    IReadOnlyCollection<int> f = {|CsWinRT1031:[1, x, ..y]|};
                    IReadOnlyList<int> g = {|CsWinRT1031:[1, 2, 3]|};
                    IReadOnlyList<int> h = {|CsWinRT1031:[x]|};
                    IReadOnlyList<int> i = {|CsWinRT1031:[1, x, ..y]|};
                }
            }
            """;

        await CSharpAnalyzerTest<CollectionExpressionAnalyzer>.VerifyAnalyzerAsync(source);
    }
}