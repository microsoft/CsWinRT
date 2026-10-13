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
                    int value = {|CSWINRT2023:span[0]|};
                    Value({|CSWINRT2023:span[1]|});
                    ref readonly int readOnlyReference = ref {|CSWINRT2023:span[2]|};
                    In(in {|CSWINRT2023:span[3]|});
                    {|CSWINRT2023:span[4]|}++;
                    {|CSWINRT2023:span[5]|} += 1;
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
                    ReadOnlySpan<int> implicitConversion = {|CSWINRT2023:span|};
                    ReadOnlySpan<int> explicitConversion = {|CSWINRT2023:(ReadOnlySpan<int>)span|};

                    Read({|CSWINRT2023:span|});
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
                    foreach (int x in {|CSWINRT2023:span|})
                    {
                    }

                    foreach (ref readonly int y in {|CSWINRT2023:span|})
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
                        int value = {|CSWINRT2023:x|};
                        Value({|CSWINRT2023:x|});
                        In(in {|CSWINRT2023:x|});
                        RefReadOnly(in {|CSWINRT2023:x|});
                        ref readonly int readOnlyReference = ref {|CSWINRT2023:x|};
                        {|CSWINRT2023:x|}++;
                        {|CSWINRT2023:x|} += 1;

                        if (value > 0)
                        {
                            Value({|CSWINRT2023:x|});               // Nested reads are detected as well
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
                    int x = {|CSWINRT2023:span[0]|};
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
                    int x = {|CSWINRT2023:span[0]|};
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
                    int x = {|CSWINRT2023:span[0]|};
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
                    int value = {|CSWINRT2023:span[0]|};

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
                    int value = {|CSWINRT2023:span[1]|};         // A different constant index

                    span[index] = 2;
                    int other = {|CSWINRT2023:span[index + 1]|}; // Not provably the same index

                    // A single element being initialized says nothing about all the other ones
                    ReadOnlySpan<int> readOnlySpan = {|CSWINRT2023:span|};

                    foreach (int x in {|CSWINRT2023:span|})
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

                    int value = {|CSWINRT2023:span[i]|};
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

                    int value = {|CSWINRT2023:span[0]|};
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

                    int value = {|CSWINRT2023:span[0]|};

                    while (condition)
                    {
                        span.Fill(1);                       // A loop body might never run
                    }

                    int other = {|CSWINRT2023:span[1]|};

                    try
                    {
                        span.Clear();                       // An exception might skip the rest of the block
                    }
                    catch (Exception)
                    {
                    }

                    int last = {|CSWINRT2023:span[2]|};
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
                    int value = {|CSWINRT2023:span[0]|};

                    ref int slot = ref span[1];             // The alias might never be written to
                    int other = {|CSWINRT2023:span[1]|};

                    foreach (ref int x in span)
                    {
                        Ref(ref x);

                        int nested = {|CSWINRT2023:x|};
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

                    int value = {|CSWINRT2023:span[0]|};
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
                    int value = {|CSWINRT2023:span[0]|};
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
                        int value = {|CSWINRT2023:x|};      // The write only covers later reads

                        x = 0;
                    }

                    foreach (ref int y in span)
                    {
                        if (condition)
                        {
                            y = 0;                          // The write might not happen
                        }

                        int value = {|CSWINRT2023:y|};
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

                    int value = {|CSWINRT2023:span[0]|};
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

                    int value = {|CSWINRT2023:span[0]|};
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

                    int value = {|CSWINRT2023:span[0]|};
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
                private int _field = {|CSWINRT2023:span[0]|};

                private int Property { get; } = {|CSWINRT2023:span[1]|};

                private int _converted = Read({|CSWINRT2023:span|});

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

                    int value = {|CSWINRT2023:span[i]|};
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

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithAliasedSpan(Span<int> span, Span<int> other)
                {
                    ref Span<int> alias = ref span;         // The alias can redirect the span from anywhere

                    span.Clear();
                    alias = other;

                    int value = {|CSWINRT2023:span[0]|};
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

                    int value = {|CSWINRT2023:span[i]|};
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

                    int value = {|CSWINRT2023:span[0]|};
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

                    int value = {|CSWINRT2023:span[0]|};
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

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithReturnedAlias(Span<int> span)
                {
                    int i = 0;
                    ref int alias = ref Identity(ref i);

                    span[i] = 1;
                    alias = 5;

                    int value = {|CSWINRT2023:span[i]|};
                }

                public unsafe void FillWithPointer(Span<int> span)
                {
                    int i = 0;
                    int* pointer = &i;

                    span[i] = 1;
                    *pointer = 5;

                    int value = {|CSWINRT2023:span[i]|};
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

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithReadOnlyFieldIndex(Span<int> span)
                {
                    ref readonly int i = ref _index;

                    span[i] = 1;
                    Reset();

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithArrayIndex(Span<int> span, int[] indices)
                {
                    ref int i = ref indices[0];

                    span[i] = 1;
                    indices[0] = 5;

                    int value = {|CSWINRT2023:span[i]|};
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

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithSpanOverIndex(Span<int> span)
                {
                    int i = 0;
                    Span<int> window = MemoryMarshal.CreateSpan(ref i, 1);

                    span[i] = 1;
                    window[0] = 5;

                    int value = {|CSWINRT2023:span[i]|};
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

                    int value = {|CSWINRT2023:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterInterpolatedStringHandlerWrite_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<char> destination, Span<int> span)
                {
                    // The handler can report failure, in which case the holes are never evaluated
                    destination.TryWrite($"{span[0] = 1}", out int written);

                    int value = {|CSWINRT2023:span[0]|};
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
                    foreach (int x in {|CSWINRT2023:span|})
                    {
                        span.Clear();
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    // --- Regression tests for false positives (and their read counterparts) ---

    [TestMethod]
    public async Task NestedStructFieldWrites_DoNotWarn()
    {
        const string source = """
            using System;

            public struct Inner
            {
                public int Value;
            }

            public struct Pair
            {
                public int X;
                public int Y;
                public Inner Inner;
            }

            public class Sample
            {
                public void Fill(Span<Pair> span, Span<(int, int)> tuples)
                {
                    // Assigning a field of a struct element only writes to (part of) that element
                    span[0].X = 1;
                    span[0].Y = 2;
                    span[1].Inner.Value = 3;
                    (span[2].X, span[2].Y) = (4, 5);
                    tuples[0].Item1 = 6;

                    foreach (ref Pair pair in span)
                    {
                        pair.X = 7;
                        pair.Inner.Value = 8;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NestedStructFieldReads_Warn()
    {
        const string source = """
            using System;

            public class Node
            {
                public int Value;
            }

            public struct Pair
            {
                public int X;
                public int Y;
                public Node Node;

                public void Touch()
                {
                }
            }

            public class Sample
            {
                public void Fill(Span<Pair> span, Span<Node> nodes)
                {
                    int x = {|CSWINRT2023:span[0]|}.X;
                    {|CSWINRT2023:span[1]|}.X += 1;
                    {|CSWINRT2023:span[2]|}.X++;
                    Value({|CSWINRT2023:span[3]|}.Y);
                    {|CSWINRT2023:span[4]|}.Touch();

                    // Writing a field through a reference stored in the element reads that reference
                    {|CSWINRT2023:span[5]|}.Node.Value = 1;
                    {|CSWINRT2023:nodes[0]|}.Value = 1;

                    // Writing one field says nothing about the other ones
                    span[6].X = 1;
                    int y = {|CSWINRT2023:span[6]|}.Y;

                    foreach (ref Pair pair in span)
                    {
                        int value = {|CSWINRT2023:pair|}.X;
                    }
                }

                private static void Value(int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task DeconstructionAssignments_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    (span[0], span[1]) = (1, 2);
                    ((span[2], span[3]), _) = ((3, 4), 5);
                    (span[4], span[5]) = GetPair();
                    (span[6], int declared) = (6, 7);

                    foreach (ref int x in span)
                    {
                        int other;

                        (x, other) = (1, 2);
                    }
                }

                private static (int, int) GetPair() => (0, 0);
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task DeconstructionReads_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    (int a, int b) = ({|CSWINRT2023:span[0]|}, {|CSWINRT2023:span[1]|});
                    var tuple = ({|CSWINRT2023:span[2]|}, 0);
                    (span[3], int c) = ({|CSWINRT2023:span[4]|}, 1);

                    foreach (ref int x in span)
                    {
                        (int d, int e) = ({|CSWINRT2023:x|}, 0);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterDeconstructionWrite_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    // A deconstruction definitely assigns each of its targets
                    (span[0], span[1]) = (1, 2);

                    int value = span[0];
                    int other = span[1];

                    foreach (ref int x in span)
                    {
                        int unused;

                        (x, unused) = (1, 2);

                        int nested = x;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task RefReassignments_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int n = 0;
                    ref int slot = ref n;

                    // Rebinding a writable 'ref' local to an element only aliases it
                    slot = ref span[0];
                    slot = 1;

                    foreach (ref int x in span)
                    {
                        int m = 0;
                        ref int other = ref m;

                        other = ref x;
                        other = 2;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task RefConditionalWrites_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool flag, bool other)
                {
                    int local = 0;

                    (flag ? ref span[0] : ref span[1]) = 5;

                    ref int slot = ref (flag ? ref span[2] : ref span[3]);
                    slot = 5;

                    Set(out (flag ? ref span[4] : ref span[5]));
                    (flag ? ref span[6] : ref (other ? ref span[7] : ref span[8])) = 6;
                    (flag ? ref span[9] : ref local) = 7;
                    ((flag ? ref span[10] : ref span[11]), local) = (8, 9);

                    foreach (ref int x in span)
                    {
                        (flag ? ref x : ref local) = 1;
                    }
                }

                private static void Set(out int x) => x = 0;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task RefConditionalReads_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, bool flag)
                {
                    int value = flag ? {|CSWINRT2023:span[0]|} : {|CSWINRT2023:span[1]|};
                    int other = (flag ? ref {|CSWINRT2023:span[2]|} : ref {|CSWINRT2023:span[3]|});
                    (flag ? ref {|CSWINRT2023:span[4]|} : ref {|CSWINRT2023:span[5]|}) += 1;
                    In(in (flag ? ref {|CSWINRT2023:span[6]|} : ref {|CSWINRT2023:span[7]|}));

                    foreach (ref int x in span)
                    {
                        int nested = (flag ? ref {|CSWINRT2023:x|} : ref value);
                    }
                }

                private static void In(in int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task AbiNamespace_DoesNotWarn()
    {
        const string source = """
            using System;

            namespace ABI
            {
                public class Sample
                {
                    public void Fill(Span<int> span)
                    {
                        int x = span[0];
                    }
                }
            }

            namespace ABI.Custom
            {
                public class Sample
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
    public async Task NonAbiNamespaces_Warn()
    {
        const string source = """
            using System;

            namespace ABIExtensions
            {
                public class Sample
                {
                    public void Fill(Span<int> span)
                    {
                        int x = {|CSWINRT2023:span[0]|};
                    }
                }
            }

            namespace Custom.ABI
            {
                public class Sample
                {
                    public void Fill(Span<int> span)
                    {
                        int x = {|CSWINRT2023:span[0]|};
                    }
                }
            }

            namespace Abi.Custom
            {
                public class Sample
                {
                    public void Fill(Span<int> span)
                    {
                        int x = {|CSWINRT2023:span[0]|};
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExplicitImplementationOfNonProjectedInterfaces_DoesNotWarn()
    {
        const string source = """
            using System;

            internal class Hidden
            {
                public interface IFillable
                {
                    void Fill(Span<int> span);
                }
            }

            public class Outer
            {
                public interface IFillable
                {
                    void Fill(Span<int> span);
                }
            }

            internal interface IInternalFillable
            {
                void Fill(Span<int> span);
            }

            namespace ABI.Custom
            {
                public interface IFillable
                {
                    void Fill(Span<int> span);
                }
            }

            public class Sample : Hidden.IFillable, Outer.IFillable, IInternalFillable, ABI.Custom.IFillable
            {
                void Hidden.IFillable.Fill(Span<int> span)
                {
                    int x = span[0];
                }

                void Outer.IFillable.Fill(Span<int> span)
                {
                    int x = span[0];
                }

                void IInternalFillable.Fill(Span<int> span)
                {
                    int x = span[0];
                }

                void ABI.Custom.IFillable.Fill(Span<int> span)
                {
                    int x = span[0];
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExplicitImplementationOfNonWindowsRuntimeInterfaces_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Sample : ISpanFormattable, IUtf8SpanFormattable
            {
                public string ToString(string format, IFormatProvider formatProvider) => "";

                bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    destination[0] = char.ToUpperInvariant(destination[0]);
                    charsWritten = 1;

                    return true;
                }

                bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    utf8Destination[0] = (byte)(utf8Destination[0] | 0x20);
                    bytesWritten = 1;

                    return true;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicImplementationOfNonWindowsRuntimeInterface_Warns()
    {
        const string source = """
            using System;

            public class Sample : ISpanFormattable
            {
                public string ToString(string format, IFormatProvider formatProvider) => "";

                // A public method is still part of the class surface, regardless of the interface
                public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    destination[0] = char.ToUpperInvariant({|CSWINRT2023:destination[0]|});
                    charsWritten = 1;

                    return true;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExplicitImplementationOfProjectedInterface_Warns()
    {
        const string referenceProjectionSource = """
            namespace Contoso
            {
                public interface IFillable
                {
                    void Fill(System.Span<int> span);
                }
            }
            """;

        const string source = """
            using System;

            public class Sample : Contoso.IFillable
            {
                void Contoso.IFillable.Fill(Span<int> span)
                {
                    int x = {|CSWINRT2023:span[0]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true, referenceProjectionSource: referenceProjectionSource);
    }

    [TestMethod]
    public async Task ReadsAfterWholeSpanWriteWithUnstableIndex_DoNotWarn()
    {
        const string source = """
            using System;

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
                public void FillWithOutIndex(Span<int> span)
                {
                    int i = 0;
                    Set(out i);
                    span.Clear();

                    // Every element is initialized, so it doesn't matter which one is read
                    _ = span[i];
                }

                public void FillWithModifiedIndex(Span<int> span)
                {
                    int i = 0;
                    span.Fill(1);
                    i++;

                    _ = span[i];
                }

                public void FillWithEscapingIndex(Span<int> span)
                {
                    int i = 0;
                    Cursor cursor = new(ref i);

                    span.Clear();
                    cursor.Advance();

                    _ = span[i];
                }

                private static void Set(out int x) => x = 0;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterElementWriteWithOutIndex_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    // An 'out' argument is implicitly 'scoped', so the reference can't escape the call
                    int i = 0;
                    Set(out i);

                    span[i] = 1;

                    int value = span[i];
                }

                private static void Set(out int x) => x = 0;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsWithEscapingOutIndex_Warn()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            public ref struct Holder
            {
                private ref int _value;

                public Holder([UnscopedRef] out int value)
                {
                    value = 0;
                    _value = ref value;
                }

                public void Set() => _value = 5;
            }

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int i;
                    Holder holder = new(out i);         // An '[UnscopedRef]' 'out' reference can escape

                    span[i] = 1;
                    holder.Set();

                    int value = {|CSWINRT2023:span[i]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsWithOperandsWrittenThroughRefConditional_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void FillWithConditionalIndex(Span<int> span, bool flag)
                {
                    int i = 0;
                    int j = 0;

                    span[i] = 1;
                    (flag ? ref i : ref j) = 5;

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithConditionalOutIndex(Span<int> span, bool flag)
                {
                    int i = 0;
                    int j = 0;

                    span[i] = 1;
                    Set(out (flag ? ref i : ref j));

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithConditionalDeconstructedIndex(Span<int> span, bool flag)
                {
                    int i = 0;
                    int j = 0;

                    span[i] = 1;
                    ((flag ? ref i : ref j), _) = (5, 0);

                    int value = {|CSWINRT2023:span[i]|};
                }

                public void FillWithConditionalSpan(Span<int> span, Span<int> other, bool flag)
                {
                    span.Clear();
                    (flag ? ref span : ref other) = other;

                    int value = {|CSWINRT2023:span[0]|};
                }

                public void FillWithConditionalSpanAlias(Span<int> span, Span<int> other, bool flag)
                {
                    ref Span<int> alias = ref (flag ? ref span : ref other);

                    span.Clear();
                    alias = other;

                    int value = {|CSWINRT2023:span[0]|};
                }

                private static void Set(out int x) => x = 0;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsInErasedConditionalCalls_DoNotWarn()
    {
        const string source = """
            using System;
            using System.Diagnostics;

            public class Logger
            {
                [Conditional("NEVER_DEFINED")]
                public virtual void Log(int x)
                {
                }
            }

            public class Sample : Logger
            {
                public override void Log(int x)
                {
                }

                public void Fill(Span<int> span)
                {
                    // The calls are removed entirely (arguments included), as the symbols are not defined
                    Trace(span[0]);
                    TraceIn(in span[1]);
                    Log(span[2]);
                    Debug.Assert(span[3] == 0);
                    Debug.WriteLine(span[4]);
                }

                [Conditional("NEVER_DEFINED")]
                private static void Trace(int x)
                {
                }

                [Conditional("NEVER_DEFINED")]
                private static void TraceIn(in int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsInDefinedConditionalCalls_Warn()
    {
        const string source = """
            #define TRACE_ENABLED
            #define DEBUG

            using System;
            using System.Diagnostics;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    Trace({|CSWINRT2023:span[0]|});
                    TraceIn(in {|CSWINRT2023:span[1]|});
                    Debug.Assert({|CSWINRT2023:span[2]|} == 0);
                }

                [Conditional("TRACE_ENABLED")]
                private static void Trace(int x)
                {
                }

                [Conditional("TRACE_ENABLED")]
                private static void TraceIn(in int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsInUnimplementedPartialMethodCalls_DoNotWarn()
    {
        const string source = """
            using System;

            public partial class Sample
            {
                static partial void Trace(int x);

                static partial void TraceIn(in int x);

                public void Fill(Span<int> span)
                {
                    // Calls to a 'partial void' method with no implementation are removed entirely
                    Trace(span[0]);
                    TraceIn(in span[1]);
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsInImplementedPartialMethodCalls_Warn()
    {
        const string source = """
            using System;

            public partial class Sample
            {
                static partial void Trace(int x);

                static partial void Trace(int x)
                {
                }

                public void Fill(Span<int> span)
                {
                    Trace({|CSWINRT2023:span[0]|});
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task InlineArrayElementWrites_DoNotWarn()
    {
        const string source = """
            using System;
            using System.Runtime.CompilerServices;

            [InlineArray(4)]
            public struct Buffer4
            {
                private int _element;
            }

            public class Sample
            {
                public void Fill(Span<Buffer4> span)
                {
                    // Assigning an element of an inline array element only writes to (part of) that element
                    span[0][1] = 5;

                    foreach (ref Buffer4 buffer in span)
                    {
                        buffer[2] = 6;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task InlineArrayElementReads_Warn()
    {
        const string source = """
            using System;
            using System.Runtime.CompilerServices;

            [InlineArray(4)]
            public struct Buffer4
            {
                private int _element;
            }

            public class Sample
            {
                public void Fill(Span<Buffer4> span)
                {
                    int value = {|CSWINRT2023:span[0]|}[1];
                    {|CSWINRT2023:span[1]|}[2] += 1;

                    foreach (ref Buffer4 buffer in span)
                    {
                        int nested = {|CSWINRT2023:buffer|}[3];
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NameOf_DoesNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    string parameterName = nameof(span);
                    string propertyName = nameof(span.Length);
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task LoopVariableReadsAfterClearOfReassignedSpan_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, Span<int> other)
                {
                    foreach (ref int x in span)
                    {
                        // The loop keeps iterating over the original buffer, which is not the one being cleared
                        span = other;
                        span.Clear();

                        int value = {|CSWINRT2023:x|};
                    }
                }

                public void FillWithLaterReassignment(Span<int> span, Span<int> other, bool flag)
                {
                    foreach (ref int x in span)
                    {
                        // A previous iteration might have already redirected the span without clearing the original buffer
                        if (flag)
                        {
                            span.Clear();

                            int value = {|CSWINRT2023:x|};
                        }

                        span = other;
                        flag = true;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ConservativeReadOnlyViewsAndAliases_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public unsafe void Fill(Span<int> span, ReadOnlySpan<int> source)
                {
                    // Read-only views, address-only uses and 'ref readonly' aliases are conservatively reported,
                    // even when they are not actually used to read any element of the span.
                    ReadOnlySpan<int> view = {|CSWINRT2023:span|};
                    int length = view.Length;

                    bool overlaps = source.Overlaps({|CSWINRT2023:span|});
                    bool reverseOverlaps = {|CSWINRT2023:span|}.Overlaps(source);
                    bool same = source == {|CSWINRT2023:span|};

                    ref readonly int slot = ref {|CSWINRT2023:span[0]|};

                    int n = 0;
                    ref readonly int rebound = ref n;
                    rebound = ref {|CSWINRT2023:span[1]|};

                    fixed (int* pointer = &{|CSWINRT2023:span[2]|})
                    {
                    }

                    foreach (ref readonly int x in {|CSWINRT2023:span|})
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterWholeSpanWriteWithReadOnlyRefIndex_DoNotWarn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void FillWithReadOnlyRefIndex(Span<int> span)
                {
                    int n = 0;

                    span.Clear();

                    // Every element is initialized, so it doesn't matter what the index aliases
                    ref readonly int index = ref n;
                    _ = span[index];
                }

            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterErasedRefArgument_DoNotWarn()
    {
        const string source = """
            using System;
            using System.Diagnostics;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int i = 0;

                    span[i] = 1;
                    Bump(ref i);                            // The call is removed, so the index is unchanged

                    _ = span[i];
                }

                [Conditional("NEVER_DEFINED")]
                private static void Bump(ref int x) => x++;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ReadsAfterDefinedRefArgument_Warn()
    {
        const string source = """
            #define BUMP_ENABLED

            using System;
            using System.Diagnostics;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int i = 0;

                    span[i] = 1;
                    Bump(ref i);

                    _ = {|CSWINRT2023:span[i]|};
                }

                [Conditional("BUMP_ENABLED")]
                private static void Bump(ref int x) => x++;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("Clear()")]
    [DataRow("Fill(42)")]
    public async Task ReadsAfterEarlierWholeSpanWriteAndUnstableElementWrite_DoNotWarn(string initialization)
    {
        string source = $$"""
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int i = 0;

                    span.{{initialization}};
                    span[i] = 1;
                    i = 1;

                    _ = span[i];
                }

                public void FillWithNestedBlock(Span<int> span)
                {
                    int i = 0;

                    span.{{initialization}};

                    {
                        span[i] = 1;
                        i = 1;

                        _ = span[i];
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("Clear()")]
    [DataRow("Fill(42)")]
    public async Task ReadsAfterUnstableElementWriteWithoutCoveringWholeSpanWrite_Warn(string initialization)
    {
        string source = $$"""
            using System;

            public class Sample
            {
                public void FillWithoutInitialization(Span<int> span)
                {
                    int i = 0;

                    span[i] = 1;
                    i = 1;

                    _ = {|CSWINRT2023:span[i]|};
                }

                public void FillWithReassignedSpan(Span<int> span, Span<int> other)
                {
                    int i = 0;

                    span.{{initialization}};
                    span = other;
                    span[i] = 1;
                    i = 1;

                    _ = {|CSWINRT2023:span[i]|};
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("Clear()")]
    [DataRow("Fill(42)")]
    public async Task LoopVariableReadsAfterGuaranteedInitializationBeforeReassignment_DoNotWarn(string initialization)
    {
        string source = $$"""
            using System;

            public class Sample
            {
                public void Fill(Span<int> span, Span<int> other)
                {
                    foreach (ref int element in span)
                    {
                        span.{{initialization}};

                        int value = element;

                        span = other;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task RefReassignmentResultReads_Warn()
    {
        const string source = """
            using System;

            public class Sample
            {
                public void Fill(Span<int> span)
                {
                    int n = 0;
                    ref int slot = ref n;

                    // The result of a ref assignment is the new referent, so consuming it by value reads the element
                    _ = (slot = ref {|CSWINRT2023:span[0]|});
                    int value = (slot = ref {|CSWINRT2023:span[1]|});
                    Value(slot = ref {|CSWINRT2023:span[2]|});
                }

                private static void Value(int x)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task FieldReadsAfterSameFieldWrite_DoNotWarn()
    {
        const string source = """
            using System;

            public struct Inner
            {
                public int Value;
            }

            public struct Pair
            {
                public int X;
                public int Y;
                public Inner Inner;
            }

            public class Sample
            {
                public void Fill(Span<Pair> span)
                {
                    span[0].X = 1;
                    _ = span[0].X;

                    span[1].Inner.Value = 2;
                    _ = span[1].Inner.Value;

                    (span[2].X, span[2].Y) = (3, 4);
                    _ = span[2].X;
                    _ = span[2].Y;

                    foreach (ref Pair pair in span)
                    {
                        pair.X = 5;
                        _ = pair.X;

                        pair.Inner.Value = 6;
                        _ = pair.Inner.Value;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task FieldReadsAfterOtherFieldWrite_Warn()
    {
        const string source = """
            using System;

            public struct Inner
            {
                public int Value;
            }

            public struct Pair
            {
                public int X;
                public int Y;
                public Inner Inner;
            }

            public class Sample
            {
                public void Fill(Span<Pair> span)
                {
                    span[0].X = 1;
                    _ = {|CSWINRT2023:span[0]|}.Y;
                    _ = {|CSWINRT2023:span[0]|};

                    span[1].Inner.Value = 2;
                    _ = {|CSWINRT2023:span[1]|}.X;

                    int i = 0;
                    span[i].X = 3;
                    i++;
                    _ = {|CSWINRT2023:span[i]|}.X;

                    foreach (ref Pair pair in span)
                    {
                        pair.X = 4;
                        _ = {|CSWINRT2023:pair|}.Y;
                        _ = {|CSWINRT2023:pair|};
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task FieldReadsAfterContainingFieldOrElementWrite_DoNotWarn()
    {
        const string source = """
            using System;

            public struct Inner
            {
                public int Value;
                public int Other;
            }

            public class Node
            {
                public int Value;
            }

            public struct Pair
            {
                public int X;
                public Inner Inner;
                public Node Node;
            }

            public class Sample
            {
                public void Fill(Span<Pair> span)
                {
                    span[0] = new Pair();
                    _ = span[0].X;
                    _ = span[0].Inner.Value;

                    span[1].Inner = new Inner { Value = 42 };
                    _ = span[1].Inner.Value;
                    _ = span[1].Inner.Other;

                    int i = 2;
                    span[i].X = 42;
                    _ = span[i].X;

                    Set(out span[3].X);
                    _ = span[3].X;

                    span[4].Node = new Node();
                    span[4].Node.Value = 42;
                    _ = span[4].Node.Value;

                    foreach (ref Pair pair in span)
                    {
                        pair.Inner = new Inner { Value = 42 };
                        _ = pair.Inner.Value;

                        Set(out pair.X);
                        _ = pair.X;
                    }
                }

                private static void Set(out int value) => value = 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task FieldReadsAfterNonCoveringWrites_Warn()
    {
        const string source = """
            using System;

            public struct Inner
            {
                public int Value;
                public int Other;

                public int Touch() => Value + Other;
            }

            public class Node
            {
                public int Value;
            }

            public struct Pair
            {
                public int X;
                public Inner Inner;
                public Node Node;

                public int Touch() => X + Inner.Other;
            }

            public class Sample
            {
                public void Fill(Span<Pair> span)
                {
                    span[0].Inner.Value = 42;
                    _ = {|CSWINRT2023:span[0]|}.Inner;
                    _ = {|CSWINRT2023:span[0]|}.Inner.Other;

                    span[1].X = 42;
                    _ = {|CSWINRT2023:span[1]|}.Touch();

                    span[2].Inner.Value = 42;
                    _ = {|CSWINRT2023:span[2]|}.Inner.Touch();

                    span[3].X = 42;
                    {|CSWINRT2023:span[3]|}.Node.Value = 42;
                }

                public void FillWithReassignment(Span<Pair> span, Span<Pair> other)
                {
                    span[0].X = 42;
                    span = other;

                    _ = {|CSWINRT2023:span[0]|}.X;
                }

                public void FillWithIndirectReassignment(Span<Pair> span, Span<Pair> other)
                {
                    span[0].X = Redirect(ref span, other);

                    _ = {|CSWINRT2023:span[0]|}.X;
                }

                private static int Redirect(ref Span<Pair> span, Span<Pair> other)
                {
                    span = other;

                    return 42;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("X", "Item1")]
    [DataRow("Item1", "X")]
    public async Task TupleFieldReadsAfterEquivalentFieldWrite_DoNotWarn(string writeField, string readField)
    {
        string source = $$"""
            using System;

            public class Sample
            {
                public void Fill(Span<(int X, int Y)> span)
                {
                    span[0].{{writeField}} = 42;

                    _ = span[0].{{readField}};
                    _ = {|CSWINRT2023:span[0]|}.Y;

                    foreach (ref (int X, int Y) pair in span)
                    {
                        pair.{{writeField}} = 42;

                        _ = pair.{{readField}};
                        _ = {|CSWINRT2023:pair|}.Y;
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}