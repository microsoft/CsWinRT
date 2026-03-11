// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public class Test_GeneratedCustomPropertyProviderTargetTypeAnalyzer
{
    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public async Task ValidTargetType_DoesNotWarn(string modifier)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial {{modifier}} MyType;
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public async Task ValidTargetType_InValidHierarchy_DoesNotWarn(string modifier)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            public partial struct A
            {
                public partial class B
                {
                    [GeneratedCustomPropertyProvider]
                    public partial {{modifier}} MyType;
                }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    [DataRow("abstract partial class")]
    [DataRow("static partial class")]
    [DataRow("static partial struct")]
    [DataRow("ref partial struct")]
    public async Task InvalidTargetType_Warns(string modifiers)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public {{modifiers}} {|CSWINRT2000:MyType|};
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public async Task TypeNotPartial_Warns(string modifier)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public {{modifier}} {|CSWINRT2001:MyType|};
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public async Task TypeNotInPartialTypeHierarchy_Warns(string modifier)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            public class ParentType
            {
                [GeneratedCustomPropertyProvider]
                public partial {{modifier}} {|CSWINRT2001:MyType|};
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }
}