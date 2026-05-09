// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<PublicTypeMixedVersioningAttributesAnalyzer>;

/// <summary>
/// Tests for <see cref="PublicTypeMixedVersioningAttributesAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_PublicTypeMixedVersioningAttributesAnalyzer
{
    [TestMethod]
    public async Task PublicClass_NoVersioning_DoesNotWarn()
    {
        const string source = """
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicClass_OnlyContractVersion_DoesNotWarn()
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
    public async Task PublicClass_OnlyVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [Version(1u)]
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ApiContractEnum_WithContractVersionAndVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            [Version(1u)]
            public enum MyContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task InternalClass_WithBothAttributes_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [Version(1u)]
            internal sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NestedPublicClass_WithBothAttributes_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class Outer
            {
                [ContractVersion(typeof(MyContract), 1u)]
                [Version(1u)]
                public sealed class Nested;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicClass_WithBothAttributes_NotComponent_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [Version(1u)]
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PublicClass_WithBothAttributes_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [Version(1u)]
            public sealed class {|CSWINRT2017:MyClass|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicInterface_WithBothAttributes_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [Version(1u)]
            public interface {|CSWINRT2017:IMyInterface|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicStruct_WithBothAttributes_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [Version(1u)]
            public struct {|CSWINRT2017:MyStruct|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicEnum_WithBothAttributes_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [Version(1u)]
            public enum {|CSWINRT2017:MyEnum|}
            {
                A,
                B
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicDelegate_WithBothAttributes_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            [Version(1u)]
            public delegate void {|CSWINRT2017:MyDelegate|}();
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
