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
    [DataRow("abstract class")]
    [DataRow("static class")]
    [DataRow("static struct")]
    [DataRow("ref struct")]
    public async Task InvalidTargetType_Warns(string modifiers)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            [{|CSWINRT2000:GeneratedCustomPropertyProvider|}]
            public {{modifiers}} MyType;
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

            [{|CSWINRT2001:GeneratedCustomPropertyProvider|}]
            public {{modifier}} MyType;
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
                [{|CSWINRT2001:GeneratedCustomPropertyProvider|}]
                public partial {{modifier}} MyType;
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderTargetTypeAnalyzer>.VerifyAnalyzerAsync(source);
    }
}