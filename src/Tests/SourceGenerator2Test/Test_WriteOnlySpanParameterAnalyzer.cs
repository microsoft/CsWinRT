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

    [TestMethod]
    public async Task ReadsAfterClear_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool condition)
                {
                    // Every element is initialized, so any subsequent read is fine
                    span.Clear();

                    int value = span[0];
                    In(in span[1]);
                    ref readonly int readOnlyReference = ref span[2];
                    ReadOnlySpan<int> readOnlySpan = span;

                    Read(span);

                    foreach (int x in span)
                    {
                    }

                    foreach (ref readonly int y in span)
                    {
                    }

                    foreach (ref int z in span)
                    {
                        int nested = z;
                    }

                    if (condition)
                    {
                        int inBranch = span[3];
                    }
                }

                private static void In(in int x)
                {
                }

                private static void Read(ReadOnlySpan<int> span)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterFill_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    span.Fill(42);

                    int value = span[0];
                    ReadOnlySpan<int> readOnlySpan = span;

                    foreach (int x in span)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ElementReadsAfterWriteToSameIndex_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    const int Index = 3;

                    span[0] = 1;
                    int value = span[0];            // Reading back an element that was just written is fine
                    In(in span[0]);
                    RefReadOnly(in span[0]);
                    ref readonly int readOnlyReference = ref span[0];
                    span[0]++;
                    span[0] += 1;

                    Out(out span[1]);               // An 'out' argument is also a definite write
                    int other = span[1];

                    span[Index] = 2;                // Constant indices are compared by value
                    int constant = span[3];

                    for (int i = 0; i < span.Length; i++)
                    {
                        span[i] = i;
                        int inLoop = span[i];
                    }
                }

                private static void In(in int x)
                {
                }

                private static void RefReadOnly(ref readonly int x)
                {
                }

                private static void Out(out int x) => x = 0;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task LoopVariableReadsAfterWrite_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool condition)
                {
                    foreach (ref int x in span)
                    {
                        x = 0;                      // The element is initialized through the alias

                        int value = x;
                        Value(x);
                        In(in x);
                        RefReadOnly(in x);
                        ref readonly int readOnlyReference = ref x;
                        x++;
                        x += 1;

                        if (condition)
                        {
                            Value(x);               // Nested reads are fine as well
                        }
                    }

                    foreach (ref int y in span)
                    {
                        Out(out y);                 // An 'out' argument is also a definite write

                        int value = y;
                    }
                }

                private static void Value(int x)
                {
                }

                private static void In(in int x)
                {
                }

                private static void RefReadOnly(ref readonly int x)
                {
                }

                private static void Out(out int x) => x = 0;
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
    public async Task ForEachWritableRefVariableReads_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    foreach (ref int x in span)
                    {
                        int value = {|CSWINRT2021:x|};
                        Value({|CSWINRT2021:x|});
                        In(in {|CSWINRT2021:x|});
                        RefReadOnly(in {|CSWINRT2021:x|});
                        ref readonly int readOnlyReference = ref {|CSWINRT2021:x|};
                        {|CSWINRT2021:x|}++;
                        {|CSWINRT2021:x|} += 1;

                        if (value > 0)
                        {
                            Value({|CSWINRT2021:x|});               // Nested reads are detected as well
                        }

                        x = 0;                                      // Writing through the alias is valid
                        Out(out x);                                 // Writing through an 'out' argument is valid
                        Ref(ref x);                                 // Passing by 'ref' may write, so it is allowed
                        ref int slot = ref x;                       // A writable 'ref' alias may write, so it is allowed
                        slot = 1;
                    }
                }

                private static void Value(int x)
                {
                }

                private static void In(in int x)
                {
                }

                private static void RefReadOnly(ref readonly int x)
                {
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

    [TestMethod]
    public async Task ReadsBeforeWrite_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    // The write only covers reads that come after it
                    int value = {|CSWINRT2021:span[0]|};

                    span[0] = 1;
                    span.Clear();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ElementReadsAfterWriteToOtherIndex_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, int index)
                {
                    span[0] = 1;
                    int value = {|CSWINRT2021:span[1]|};         // A different constant index

                    span[index] = 2;
                    int other = {|CSWINRT2021:span[index + 1]|}; // Not provably the same index

                    // A single element being initialized says nothing about all the other ones
                    ReadOnlySpan<int> readOnlySpan = {|CSWINRT2021:span|};

                    foreach (int x in {|CSWINRT2021:span|})
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ElementReadsAfterModifiedIndex_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int i = 0;

                    span[i] = 1;
                    i++;                                    // The two accesses no longer target the same element

                    int value = {|CSWINRT2021:span[i]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterReassignedSpan_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    span.Clear();
                    span = default;                         // The write applies to a different buffer

                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterConditionalWrite_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool condition)
                {
                    if (condition)
                    {
                        span.Clear();
                    }

                    int value = {|CSWINRT2021:span[0]|};

                    while (condition)
                    {
                        span.Fill(1);                       // A loop body might never run
                    }

                    int other = {|CSWINRT2021:span[1]|};

                    try
                    {
                        span.Clear();                       // An exception might skip the rest of the block
                    }
                    catch (Exception)
                    {
                    }

                    int last = {|CSWINRT2021:span[2]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterNonDefiniteWrite_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    Ref(ref span[0]);                       // The callee might never write to the element
                    int value = {|CSWINRT2021:span[0]|};

                    ref int slot = ref span[1];             // The alias might never be written to
                    int other = {|CSWINRT2021:span[1]|};

                    foreach (ref int x in span)
                    {
                        Ref(ref x);

                        int nested = {|CSWINRT2021:x|};
                    }
                }

                private static void Ref(ref int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterWriteToOtherSpan_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, Span<int> other)
                {
                    other.Clear();
                    other[0] = 1;

                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterWriteSkippedByGoto_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool condition)
                {
                    if (condition)
                    {
                        goto Read;
                    }

                    span.Clear();

                Read:
                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task LoopVariableReadsBeforeWrite_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool condition)
                {
                    foreach (ref int x in span)
                    {
                        int value = {|CSWINRT2021:x|};      // The write only covers later reads

                        x = 0;
                    }

                    foreach (ref int y in span)
                    {
                        if (condition)
                        {
                            y = 0;                          // The write might not happen
                        }

                        int value = {|CSWINRT2021:y|};
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterWriteInSwitchSection_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, int selector)
                {
                    switch (selector)
                    {
                        case 0:
                            span.Clear();
                            break;
                        default:
                            span.Fill(1);
                            break;
                    }

                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterShortCircuitedWrite_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool condition)
                {
                    // The write is only reached when the left operand is 'true'
                    bool result = condition && Set(out span[0]);

                    int value = {|CSWINRT2021:span[0]|};
                }

                private static bool Set(out int x)
                {
                    x = 0;

                    return true;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterExtensionMethodCall_Warn()
    {
        const string source = """
            using System;

            public static class SpanExtensions
            {
                public static void Clear(this Span<int> span, bool flag)
                {
                }
            }

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    span.Clear(true);                       // Not the 'Span<T>.Clear()' method

                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PrimaryConstructorInitializers_Warn()
    {
        const string source = """
            using System;

            public class Sample(Span<int> span)
            {
                private int _field = {|CSWINRT2021:span[0]|};

                private int Property { get; } = {|CSWINRT2021:span[1]|};

                private int _converted = Read({|CSWINRT2021:span|});

                private static int Read(ReadOnlySpan<int> span) => span.Length;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterDeconstructedIndex_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int i = 0;

                    span[i] = 1;
                    (i, _) = (5, 0);                        // A deconstruction also modifies the index

                    int value = {|CSWINRT2021:span[i]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsWithAliasedIndexOrSpan_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void FillWithAliasedIndex(Span<int> span)
                {
                    int i = 0;
                    ref int alias = ref i;                  // The alias can modify the index from anywhere

                    span[i] = 1;
                    alias = 5;

                    int value = {|CSWINRT2021:span[i]|};
                }

                public void FillWithAliasedSpan(Span<int> span, Span<int> other)
                {
                    ref Span<int> alias = ref span;         // The alias can redirect the span from anywhere

                    span.Clear();
                    alias = other;

                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsWithCapturedIndex_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int i = 0;

                    void Advance() => i++;                  // The capture can modify the index when invoked

                    span[i] = 1;
                    Advance();

                    int value = {|CSWINRT2021:span[i]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterConditionalMethodWrite_Warn()
    {
        const string source = """
            using System;
            using System.Diagnostics;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    // The whole call, arguments included, is removed when 'DEBUG' is not defined
                    Log(span[0] = 1);

                    int value = {|CSWINRT2021:span[0]|};
                }

                [Conditional("DEBUG")]
                private static void Log(int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterConditionalOverrideWrite_Warn()
    {
        const string source = """
            using System;
            using System.Diagnostics;

            public class Logger
            {
                [Conditional("DEBUG")]
                public virtual void Log(int x)
                {
                }
            }

            public class Sample : Logger
            {
                // An override inherits the conditional symbols of the method it overrides
                public override void Log(int x)
                {
                }

                public void Fill(Span<int> span)
                {
                    Log(span[0] = 1);

                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsWithIndirectlyAliasedIndex_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void FillWithConditionalAlias(Span<int> span, bool condition)
                {
                    int i = 0;
                    int j = 0;
                    ref int alias = ref (condition ? ref i : ref j);

                    span[i] = 1;
                    alias = 5;

                    int value = {|CSWINRT2021:span[i]|};
                }

                public void FillWithReturnedAlias(Span<int> span)
                {
                    int i = 0;
                    ref int alias = ref Identity(ref i);

                    span[i] = 1;
                    alias = 5;

                    int value = {|CSWINRT2021:span[i]|};
                }

                public unsafe void FillWithPointer(Span<int> span)
                {
                    int i = 0;
                    int* pointer = &i;

                    span[i] = 1;
                    *pointer = 5;

                    int value = {|CSWINRT2021:span[i]|};
                }

                private static ref int Identity(ref int x) => ref x;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsWithRefLocalIndex_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                private int _index;

                public void FillWithFieldIndex(Span<int> span)
                {
                    // The index aliases the field, so it changes with no write to 'i' in sight
                    ref int i = ref _index;

                    span[i] = 1;
                    _index = 5;

                    int value = {|CSWINRT2021:span[i]|};
                }

                public void FillWithReadOnlyFieldIndex(Span<int> span)
                {
                    ref readonly int i = ref _index;

                    span[i] = 1;
                    Reset();

                    int value = {|CSWINRT2021:span[i]|};
                }

                public void FillWithArrayIndex(Span<int> span, int[] indices)
                {
                    ref int i = ref indices[0];

                    span[i] = 1;
                    indices[0] = 5;

                    int value = {|CSWINRT2021:span[i]|};
                }

                private void Reset() => _index = 5;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsWithEscapingIndex_Warn()
    {
        const string source = """
            using System;
            using System.Runtime.InteropServices;

            public ref struct Cursor
            {
                private ref int _index;

                public Cursor(ref int index)
                {
                    _index = ref index;
                }

                public void Advance() => _index++;
            }

            public class Sample
            {
                public void FillWithStoredIndexReference(Span<int> span)
                {
                    int i = 0;
                    Cursor cursor = new(ref i);                     // The reference outlives the call

                    span[i] = 1;
                    cursor.Advance();

                    int value = {|CSWINRT2021:span[i]|};
                }

                public void FillWithSpanOverIndex(Span<int> span)
                {
                    int i = 0;
                    Span<int> window = MemoryMarshal.CreateSpan(ref i, 1);

                    span[i] = 1;
                    window[0] = 5;

                    int value = {|CSWINRT2021:span[i]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterUnimplementedPartialMethodWrite_Warn()
    {
        const string source = """
            using System;

            public partial class Sample
            {
                // Calls to a 'partial void' method with no implementing declaration are
                // removed entirely by the compiler, the evaluation of arguments included
                partial void Log(int x);

                public void Fill(Span<int> span)
                {
                    Log(span[0] = 1);

                    int value = {|CSWINRT2021:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ForEachWithWriteInBody_Warns()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    // The write happens after each element has already been read
                    foreach (int x in {|CSWINRT2021:span|})
                    {
                        span.Clear();
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
