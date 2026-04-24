// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<ValidContractVersionAttributeAnalyzer>;

/// <summary>
/// Tests for <see cref="ValidContractVersionAttributeAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_ValidContractVersionAttributeAnalyzer
{
    [TestMethod]
    public async Task VersionOnlyConstructor_OnApiContractType_DoesNotWarn()
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
    public async Task ContractNameAndVersionConstructor_OnApiContractType_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion("MyContractName", 1u)]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ContractTypeConstructor_WithValidContract_OnNonContractType_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public class MyType;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task InvalidUsage_NotComponent_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum MyContract;

            [ContractVersion(1u)]
            public class NotAContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [ApiContract]
            public enum AlsoAContract;

            [ContractVersion(typeof(NotAContract), 1u)]
            public class TypeWithBadContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task VersionOnlyConstructor_OnNonContractType_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [{|CSWINRT2011:ContractVersion(1u)|}]
            public class MyType;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task VersionOnlyConstructor_OnEnumWithoutApiContract_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [{|CSWINRT2011:ContractVersion(1u)|}]
            public enum MyEnum;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ContractNameAndVersionConstructor_OnNonContractType_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [{|CSWINRT2011:ContractVersion("MyContractName", 1u)|}]
            public class MyType;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ContractTypeConstructor_OnApiContractType_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum OtherContract;

            [ApiContract]
            [{|CSWINRT2012:ContractVersion(typeof(OtherContract), 1u)|}]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ContractTypeConstructor_WithNonContractTypeArgument_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            public class NotAContract;

            [ContractVersion({|CSWINRT2013:typeof(NotAContract)|}, 1u)]
            public class MyType;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ContractTypeConstructor_WithEnumWithoutApiContractArgument_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            public enum NotAContract;

            [ContractVersion({|CSWINRT2013:typeof(NotAContract)|}, 1u)]
            public class MyType;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ContractTypeConstructor_OnApiContractType_WithNonContractTypeArgument_WarnsBoth()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            public class NotAContract;

            [ApiContract]
            [{|CSWINRT2012:ContractVersion({|CSWINRT2013:typeof(NotAContract)|}, 1u)|}]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
