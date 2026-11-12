// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<WindowsRuntimeNativeExposedTypeAnalyzer>;

/// <summary>
/// Tests for <see cref="WindowsRuntimeNativeExposedTypeAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_WindowsRuntimeNativeExposedTypeAnalyzer
{
    [TestMethod]
    public async Task ProjectedClass_DoesNotWarn()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType(typeof(Microsoft.UI.Xaml.DependencyObjectCollection))]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task Interface_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2018:typeof(IMyInterface)|})]

            public interface IMyInterface;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task AbstractClass_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2018:typeof(MyClass)|})]

            public abstract class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task StaticClass_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2018:typeof(MyClass)|})]

            public static class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GenericTypeDefinition_Warns()
    {
        const string source = """
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2018:typeof(List<>)|})]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task UserDefinedClass_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(MyClass)|})]

            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ValueType_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(MyStruct)|})]

            public struct MyStruct;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task Delegate_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(MyDelegate)|})]

            public delegate void MyDelegate();
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ArrayType_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(int[])|})]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ClosedGenericType_Warns()
    {
        const string source = """
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(List<int>)|})]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ProjectedStruct_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(Windows.Foundation.Point)|})]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task DuplicateProjectedClass_BothWarn()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2020:typeof(Microsoft.UI.Xaml.DependencyObjectCollection)|})]
            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2020:typeof(Microsoft.UI.Xaml.DependencyObjectCollection)|})]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task DuplicateNonProjectedClass_WarnsAsNotProjectedClass()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(MyClass)|})]
            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(MyClass)|})]

            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task DuplicateAcrossUserAndGeneratedCode_OnlyUserWarns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2020:typeof(Microsoft.UI.Xaml.DependencyObjectCollection)|})]
            """;

        const string generatedSource = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType(typeof(Microsoft.UI.Xaml.DependencyObjectCollection))]
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, generatedSource: generatedSource);
    }

    [TestMethod]
    public async Task ProjectedClassFromReferenceProjection_DoesNotWarn()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType(typeof(MyProjectedClass))]
            """;

        const string referenceProjectionSource = """
            public sealed class MyProjectedClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, referenceProjectionSource: referenceProjectionSource);
    }

    [TestMethod]
    public async Task ProjectedStructFromReferenceProjection_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2019:typeof(MyProjectedStruct)|})]
            """;

        const string referenceProjectionSource = """
            public struct MyProjectedStruct;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, referenceProjectionSource: referenceProjectionSource);
    }

    [TestMethod]
    public async Task DuplicateProjectedClassFromReferenceProjection_BothWarn()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2020:typeof(MyProjectedClass)|})]
            [assembly: WindowsRuntimeNativeExposedType({|CSWINRT2020:typeof(MyProjectedClass)|})]
            """;

        const string referenceProjectionSource = """
            public sealed class MyProjectedClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, referenceProjectionSource: referenceProjectionSource);
    }

    [TestMethod]
    public async Task ApplicationInGeneratedCodeOnly_DoesNotWarn()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType(typeof(Microsoft.UI.Xaml.DependencyObjectCollection))]
            """;

        const string generatedSource = """
            using WindowsRuntime.InteropServices;

            [assembly: WindowsRuntimeNativeExposedType(typeof(MyClass))]

            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, generatedSource: generatedSource);
    }
}
