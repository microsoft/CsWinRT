// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<PublicTypeRequiresContractVersionAnalyzer>;

/// <summary>
/// Tests for <see cref="PublicTypeRequiresContractVersionAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_PublicTypeRequiresContractVersionAnalyzer
{
    [TestMethod]
    public async Task PublicClass_WithContractVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ApiContractEnum_WithoutContractVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task InternalClass_WithoutContractVersion_DoesNotWarn()
    {
        const string source = """
            internal sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NestedPublicClass_WithoutContractVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class Outer
            {
                public sealed class Nested;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicClass_NotComponent_DoesNotWarn()
    {
        const string source = """
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PublicClass_WithoutContractVersion_Warns()
    {
        const string source = """
            public sealed class {|CSWINRT2015:MyClass|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicInterface_WithoutContractVersion_Warns()
    {
        const string source = """
            public interface {|CSWINRT2015:IMyInterface|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicStruct_WithoutContractVersion_Warns()
    {
        const string source = """
            public struct {|CSWINRT2015:MyStruct|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicEnum_WithoutContractVersion_Warns()
    {
        const string source = """
            public enum {|CSWINRT2015:MyEnum|}
            {
                A,
                B
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicDelegate_WithoutContractVersion_Warns()
    {
        const string source = """
            public delegate void {|CSWINRT2015:MyDelegate|}();
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
