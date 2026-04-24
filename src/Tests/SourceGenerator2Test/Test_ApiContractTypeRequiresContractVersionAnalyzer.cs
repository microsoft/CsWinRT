// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<ApiContractTypeRequiresContractVersionAnalyzer>;

/// <summary>
/// Tests for <see cref="ApiContractTypeRequiresContractVersionAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_ApiContractTypeRequiresContractVersionAnalyzer
{
    [TestMethod]
    public async Task ApiContractEnum_WithVersionOnlyConstructor_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ApiContractEnum_WithNameAndVersionConstructor_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion("OtherContract", 1u)]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NonApiContractEnum_DoesNotWarn()
    {
        const string source = """
            public enum MyEnum
            {
                A,
                B
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ApiContractEnum_NotComponent_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ApiContractEnum_NoContractVersion_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum {|CSWINRT2014:MyContract|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ApiContractEnum_OnlyContractTypeConstructor_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(typeof(MyContract), 1u)]
            public enum {|CSWINRT2014:MyContract|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
