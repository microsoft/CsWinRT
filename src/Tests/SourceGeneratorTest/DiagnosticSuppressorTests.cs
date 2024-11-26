using System.Threading.Tasks;
using Generator;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SourceGeneratorTest.Helpers;

namespace SourceGeneratorTest;

[TestClass]
public sealed class DiagnosticSuppressorTests
{
    public static readonly DiagnosticResult IDE0300 = DiagnosticResult.CompilerWarning("IDE0300");

    [TestMethod]
    public async Task CollectionExpression_IDE0300_ArrayInitializer_TargetingArray_NotSuppressed()
    {
        await new CSharpSuppressorTest<CollectionExpressionIDE0300Suppressor>(
            """
            class TestClass
            {
                private int[] f = {|IDE0300:{|} 1, 2, 3 };

                void TestMethod()
                {
                    int[] a = {|IDE0300:{|} 1, 2, 3 };
                }

                public int[] P { get; } = {|IDE0300:{|} 1, 2, 3 };
            }
            """)
            .WithAnalyzer("Microsoft.CodeAnalysis.CSharp.UseCollectionExpression.CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer, Microsoft.CodeAnalysis.CSharp.CodeStyle")
            .WithSpecificDiagnostics(IDE0300)
            .WithEditorconfig(("CsWinRTAotOptimizerEnabled", "auto"))
            .RunAsync();
    }

    [TestMethod]
    [Ignore("Bug in the Roslyn test runner that throws a 'TypeInitializationException'")]
    public async Task CollectionExpression_IDE0300_ArrayCreation_TargetingInterfaceType_Suppressed()
    {
        await new CSharpSuppressorTest<CollectionExpressionIDE0300Suppressor>(
            """
            using System.Collections.Generic;

            class TestClass
            {
                void TestMethod()
                {
                    IEnumerable<int> a = {|IDE0300:new[] {|} 1, 2, 3 };
                }
            }
            """)
            .WithAnalyzer("Microsoft.CodeAnalysis.CSharp.UseCollectionExpression.CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer, Microsoft.CodeAnalysis.CSharp.CodeStyle")
            .WithSpecificDiagnostics(IDE0300)
            .WithEditorconfig(("CsWinRTAotOptimizerEnabled", "auto"))
            .RunAsync();
    }
}