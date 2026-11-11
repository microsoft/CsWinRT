// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<ExperimentalAttributeTargetAnalyzer>;

/// <summary>
/// Tests for <see cref="ExperimentalAttributeTargetAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_ExperimentalAttributeTargetAnalyzer
{
    [TestMethod]
    [DataRow("public sealed class MyClass;")]
    [DataRow("public interface IMyInterface;")]
    [DataRow("public struct MyStruct;")]
    [DataRow("public enum MyEnum { A }")]
    [DataRow("public delegate void MyDelegate();")]
    public async Task ExperimentalOnType_DoesNotWarn(string declaration)
    {
        string source = $$"""
            using System.Diagnostics.CodeAnalysis;

            [Experimental("TEST0001")]
            {{declaration}}
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExperimentalOnMembers_DoesNotWarn()
    {
        const string source = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            public sealed class MyClass
            {
                [Experimental("TEST0001")]
                public int MyMethod() => 42;

                [Experimental("TEST0002")]
                public int MyProperty { get; set; }

                [Experimental("TEST0003")]
                public event EventHandler<int> MyEvent;
            }

            public struct MyStruct
            {
                [Experimental("TEST0004")]
                public int MyField;
            }

            public enum MyEnum
            {
                [Experimental("TEST0005")]
                A
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("private")]
    [DataRow("internal")]
    [DataRow("protected")]
    public async Task ExperimentalOnNonPublicConstructor_DoesNotWarn(string accessibility)
    {
        string source = $$"""
            using System.Diagnostics.CodeAnalysis;

            public class MyClass
            {
                [Experimental("TEST0001")]
                {{accessibility}} MyClass()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExperimentalOnStaticConstructor_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            public sealed class MyClass
            {
                [Experimental("TEST0001")]
                static MyClass()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExperimentalOnConstructorOfNonVisibleType_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            internal sealed class MyInternalClass
            {
                [Experimental("TEST0001")]
                public MyInternalClass()
                {
                }
            }

            internal class MyOuterClass
            {
                public sealed class MyNestedClass
                {
                    [Experimental("TEST0002")]
                    public MyNestedClass()
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExperimentalOnConstructor_NotComponent_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            public sealed class MyClass
            {
                [Experimental("TEST0001")]
                public MyClass()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task AssemblyLevelExperimental_NotComponent_DoesNotWarn()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            [assembly: Experimental("TEST0001")]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    /// <summary>
    /// A same-named attribute type that is not the .NET <c>[Experimental]</c> one is never translated
    /// by the WinMD generator to begin with, so it is copied like any other user attribute.
    /// </summary>
    [TestMethod]
    public async Task UnrelatedExperimentalAttribute_DoesNotWarn()
    {
        const string source = """
            using System;

            public sealed class ExperimentalAttribute : Attribute
            {
                public ExperimentalAttribute(string diagnosticId)
                {
                }
            }

            public sealed class MyClass
            {
                [Experimental("TEST0001")]
                public MyClass()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExperimentalOnPublicConstructor_Warns()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            public sealed class MyClass
            {
                [{|CSWINRT2021:Experimental("TEST0001")|}]
                public MyClass()
                {
                }

                [{|CSWINRT2021:Experimental("TEST0002", UrlFormat = "https://example.com/{0}", Message = "Not final")|}]
                public MyClass(int value)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ExperimentalOnPublicConstructorOfNestedPublicType_Warns()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            public class MyOuterClass
            {
                public sealed class MyNestedClass
                {
                    [{|CSWINRT2021:Experimental("TEST0001")|}]
                    public MyNestedClass()
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("""Experimental("TEST0001")""")]
    [DataRow("""ExperimentalAttribute("TEST0001")""")]
    [DataRow("""System.Diagnostics.CodeAnalysis.Experimental("TEST0001")""")]
    [DataRow("""global::System.Diagnostics.CodeAnalysis.ExperimentalAttribute("TEST0001")""")]
    public async Task ExperimentalOnPublicConstructor_AnySyntacticForm_Warns(string attribute)
    {
        string source = $$"""
            using System.Diagnostics.CodeAnalysis;

            public sealed class MyClass
            {
                [{|CSWINRT2021:{{attribute}}|}]
                public MyClass()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task AssemblyLevelExperimental_Warns()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            [assembly: {|CSWINRT2021:Experimental("TEST0001")|}]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ModuleLevelExperimental_Warns()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            [module: {|CSWINRT2021:Experimental("TEST0001")|}]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
