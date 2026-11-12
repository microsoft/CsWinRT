// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<ValidApiContractEnumTypeAnalyzer>;

/// <summary>
/// Tests for <see cref="ValidApiContractEnumTypeAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_ValidApiContractEnumTypeAnalyzer
{
    [TestMethod]
    public async Task EmptyApiContractEnum_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task EnumWithCases_NoApiContract_DoesNotWarn()
    {
        const string source = """
            public enum MyEnum
            {
                A,
                B,
                C
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ApiContractEnumWithCases_NotComponent_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum MyContract
            {
                A
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ApiContractEnumWithCases_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum {|CSWINRT2010:MyContract|}
            {
                A,
                B
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ApiContractEnumWithSingleCase_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum {|CSWINRT2010:MyContract|}
            {
                Version1
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
