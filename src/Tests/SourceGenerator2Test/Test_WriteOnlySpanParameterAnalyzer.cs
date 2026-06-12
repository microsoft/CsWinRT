// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<WriteOnlySpanParameterAnalyzer>;

/// <summary>
/// Tests for <see cref="WriteOnlySpanParameterAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_WriteOnlySpanParameterAnalyzer
{
    // --- Tests where the analyzer should NOT warn ---

    [TestMethod]
    public async Task WriteOnlyUsages_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    span[0] = 1;                    // Writing to an element is valid
                    int length = span.Length;       // Reading non-indexer properties is valid
                    bool empty = span.IsEmpty;
                    Out(out span[1]);               // Writing through an 'out' argument is valid
                    Ref(ref span[2]);               // Passing by 'ref' may write, so it is allowed
                    ref int slot = ref span[3];     // A writable 'ref' alias may write, so it is allowed
                    slot = 4;

                    foreach (ref int x in span)     // A writable 'ref' loop variable may write
                    {
                        x = 5;
                    }
                }

                private static void Out(out int x) => x = 0;

                private static void Ref(ref int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ForEachWithWritableRefVariable_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    // A writable 'ref' loop variable can be used to write to all elements sequentially
                    foreach (ref var x in span)
                    {
                    }

                    foreach (ref int y in span)
                    {
                        y = 1;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadOnlySpanParameter_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Read(ReadOnlySpan<int> span)
                {
                    int x = span[0];
                    ReadOnlySpan<int> other = span;

                    foreach (int y in span)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NotComponent_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int x = span[0];
                    ReadOnlySpan<int> other = span;

                    foreach (int y in span)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PrivateMethod_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                private void Fill(Span<int> span)
                {
                    int x = span[0];
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task InternalClass_DoesNotWarn()
    {
        const string source = """
            using System;

            internal class Sample
            {
                public void Fill(Span<int> span)
                {
                    int x = span[0];
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NestedClass_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Outer
            {
                public class Inner
                {
                    public void Fill(Span<int> span)
                    {
                        int x = span[0];
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task Struct_DoesNotWarn()
    {
        const string source = """
            using System;

            public struct Sample
            {
                public void Fill(Span<int> span)
                {
                    int x = span[0];
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task LocalFunction_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Method()
                {
                    static void Fill(Span<int> span)
                    {
                        int x = span[0];
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    // --- Tests where the analyzer SHOULD warn ---

    [TestMethod]
    public async Task IndexerReads_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int value = {|CSWINRT2021:span[0]|};
                    Value({|CSWINRT2021:span[1]|});
                    ref readonly int readOnlyReference = ref {|CSWINRT2021:span[2]|};
                    In(in {|CSWINRT2021:span[3]|});
                    {|CSWINRT2021:span[4]|}++;
                    {|CSWINRT2021:span[5]|} += 1;
                }

                private static void Value(int x)
                {
                }

                private static void In(in int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadOnlySpanConversions_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    ReadOnlySpan<int> implicitConversion = {|CSWINRT2021:span|};
                    ReadOnlySpan<int> explicitConversion = {|CSWINRT2021:(ReadOnlySpan<int>)span|};

                    Read({|CSWINRT2021:span|});
                }

                private static void Read(ReadOnlySpan<int> span)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ForEachReads_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    foreach (int x in {|CSWINRT2021:span|})
                    {
                    }

                    foreach (ref readonly int y in {|CSWINRT2021:span|})
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task StaticMethod_Warns()
    {
        const string source = """
            using System;

            public class Sample
            {
                public static void Fill(Span<int> span)
                {
                    int x = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task Constructor_Warns()
    {
        const string source = """
            using System;

            public class Sample
            {
                public Sample(Span<int> span)
                {
                    int x = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExplicitInterfaceImplementation_Warns()
    {
        const string source = """
            using System;

            public interface IFillable
            {
                void Fill(Span<int> span);
            }

            public class Sample : IFillable
            {
                void IFillable.Fill(Span<int> span)
                {
                    int x = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
