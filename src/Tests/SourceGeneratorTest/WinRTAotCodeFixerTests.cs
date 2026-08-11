// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpCodeFixTest = SourceGeneratorTest.Helpers.CSharpCodeFixTest<
    WinRT.SourceGenerator.WinRTAotDiagnosticAnalyzer,
    WinRT.SourceGenerator.WinRTAotCodeFixer>;

namespace SourceGeneratorTest;

/// <summary>
/// Tests for the "Make type partial" code fix for CsWinRT1028. Any type kind that can be boxed and passed
/// across the WinRT ABI is supported, so the fix has to work for classes, structs, records and record structs.
/// </summary>
[TestClass]
public class WinRTAotCodeFixerTests
{
    private const string CollectionMembers = """
            public int this[int index] => 0;
                public int Count => 0;
                public IEnumerator<int> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
        """;

    [TestMethod]
    public async Task MakePartial_Class()
    {
        await VerifyMakePartialAsync("class", "class");
    }

    [TestMethod]
    public async Task MakePartial_Struct()
    {
        await VerifyMakePartialAsync("struct", "struct");
    }

    [TestMethod]
    public async Task MakePartial_Record()
    {
        await VerifyMakePartialAsync("record", "record");
    }

    [TestMethod]
    public async Task MakePartial_RecordStruct()
    {
        await VerifyMakePartialAsync("record struct", "record struct");
    }

    [TestMethod]
    public async Task MakePartial_NestedStruct()
    {
        const string original = """
            using System.Collections;
            using System.Collections.Generic;

            class Outer
            {
                struct {|CsWinRT1028:MyCollection|} : IReadOnlyList<int>
                {
                    public int this[int index] => 0;
                    public int Count => 0;
                    public IEnumerator<int> GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
            }
            """;

        // Both the nested type and all of its containing types have to be made partial
        const string @fixed = """
            using System.Collections;
            using System.Collections.Generic;

            partial class Outer
            {
                partial struct MyCollection : IReadOnlyList<int>
                {
                    public int this[int index] => 0;
                    public int Count => 0;
                    public IEnumerator<int> GetEnumerator() => throw null;
                    IEnumerator IEnumerable.GetEnumerator() => throw null;
                }
            }
            """;

        await RunAsync(original, @fixed);
    }

    private static Task VerifyMakePartialAsync(string originalKeyword, string fixedKeyword)
    {
        string original = $$"""
            using System.Collections;
            using System.Collections.Generic;

            {{originalKeyword}} {|CsWinRT1028:MyCollection|} : IReadOnlyList<int>
            {
                {{CollectionMembers}}
            }
            """;

        string @fixed = $$"""
            using System.Collections;
            using System.Collections.Generic;

            partial {{fixedKeyword}} MyCollection : IReadOnlyList<int>
            {
                {{CollectionMembers}}
            }
            """;

        return RunAsync(original, @fixed);
    }

    private static Task RunAsync(string original, string @fixed)
    {
        CSharpCodeFixTest test = new(
            LanguageVersion.CSharp13,
            editorconfig: [("CsWinRTAotOptimizerEnabled", "auto"), ("CsWinRTAotWarningLevel", "2")])
        {
            TestCode = original,
            FixedCode = @fixed,

            // CsWinRT1028 is declared with multiple descriptors (a warning and an info variant), so
            // resolve that ambiguity in markup by using the first matching descriptor.
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        return test.RunAsync();
    }
}
