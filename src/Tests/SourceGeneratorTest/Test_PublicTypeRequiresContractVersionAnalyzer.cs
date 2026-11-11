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
    public async Task NoPublicTypes_DoesNotWarn()
    {
        const string source = """
            internal sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task SinglePublicType_WithContractVersion_DoesNotWarn()
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
    public async Task SinglePublicType_WithoutContractVersion_DoesNotWarn()
    {
        const string source = """
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task AllPublicTypes_WithoutContractVersion_DoesNotWarn()
    {
        const string source = """
            public sealed class MyClass1;

            public sealed class MyClass2;

            public interface IMyInterface;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task AllPublicTypes_WithContractVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass1;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass2;

            [ContractVersion(typeof(MyContract), 1u)]
            public interface IMyInterface;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task MixedPublicTypes_NotComponent_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass1;

            public sealed class MyClass2;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ApiContractEnum_WithoutContractVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass1;

            [ApiContract]
            public enum MyOtherContract;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NestedPublicType_WithoutContractVersion_DoesNotWarn()
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
    public async Task InternalType_WithoutContractVersion_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass1;

            internal sealed class MyClass2;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task MixedPublicTypes_WithoutContractVersion_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass1;

            public sealed class {|CSWINRT2016:MyClass2|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicTypeWithVersionOnly_OtherTypeWithContractVersion_Warns()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass1;

            [Version(1u)]
            public sealed class {|CSWINRT2016:MyClass2|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task MultiplePublicTypes_WithoutContractVersion_AllWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [ApiContract]
            [ContractVersion(1u)]
            public enum MyContract;

            [ContractVersion(typeof(MyContract), 1u)]
            public sealed class MyClass1;

            public sealed class {|CSWINRT2016:MyClass2|};

            public interface {|CSWINRT2016:IMyInterface|};

            public struct {|CSWINRT2016:MyStruct|};

            public enum {|CSWINRT2016:MyEnum|}
            {
                A,
                B
            }

            public delegate void {|CSWINRT2016:MyDelegate|}();
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
