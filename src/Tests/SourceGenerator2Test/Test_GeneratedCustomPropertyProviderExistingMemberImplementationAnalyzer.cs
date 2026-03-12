// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis.Testing;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    WindowsRuntime.SourceGenerator.Diagnostics.GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public class Test_GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer
{
    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public async Task ValidType_NoInterface_DoesNotWarn(string modifier)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial {{modifier}} MyType
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ValidType_NoMembers_DoesNotWarn()
    {
        string source = $$"""            
            using Microsoft.UI.Xaml.Data;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial class MyType : ICustomPropertyProvider
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source, expectedDiagnostics: [
            // /0/Test0.cs(5,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetCustomProperty(string)'
            DiagnosticResult.CompilerError("CS0535").WithSpan(5, 31, 5, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string)"),
            // /0/Test0.cs(5,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetIndexedProperty(string, Type)'
            DiagnosticResult.CompilerError("CS0535").WithSpan(5, 31, 5, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string, System.Type)"),
            // /0/Test0.cs(5,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetStringRepresentation()'
            DiagnosticResult.CompilerError("CS0535").WithSpan(5, 31, 5, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()"),
            // /0/Test0.cs(5,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.Type'
            DiagnosticResult.CompilerError("CS0535").WithSpan(5, 31, 5, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type")]);
    }

    [TestMethod]
    public async Task TypeWithExplicitInterfaceImplementation_Warns()
    {
        string source = """
            using System;
            using Microsoft.UI.Xaml.Data;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial class {|CSWINRT2003:MyType|} : ICustomPropertyProvider
            {
                Type ICustomPropertyProvider.Type => typeof(MyType);
                ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name) => null;
                ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type type) => null;
                string ICustomPropertyProvider.GetStringRepresentation() => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task TypeWithExplicitInterfaceImplementation_Incomplete_Warns()
    {
        string source = """
            using System;
            using Microsoft.UI.Xaml.Data;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial class MyType : ICustomPropertyProvider
            {
                Type ICustomPropertyProvider.Type => typeof(MyType);
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source, expectedDiagnostics: [
            // /0/Test0.cs(6,22): error CSWINRT2003: The type 'MyType' cannot use '[GeneratedCustomPropertyProvider]' because it already has or inherits implementations for one or more 'ICustomPropertyProvider' members
            VerifyCS.Diagnostic().WithSpan(6, 22, 6, 28).WithArguments("MyType"),
            // /0/Test0.cs(6,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetCustomProperty(string)'
            DiagnosticResult.CompilerError("CS0535").WithSpan(6, 31, 6, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string)"),
            // /0/Test0.cs(6,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetIndexedProperty(string, Type)'
            DiagnosticResult.CompilerError("CS0535").WithSpan(6, 31, 6, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string, System.Type)"),
            // /0/Test0.cs(6,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetStringRepresentation()'
            DiagnosticResult.CompilerError("CS0535").WithSpan(6, 31, 6, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()")]);
    }

    [TestMethod]
    public async Task TypeWithImplicitInterfaceImplementation_Warns()
    {
        string source = """
            using System;
            using Microsoft.UI.Xaml.Data;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial class {|CSWINRT2003:MyType|} : ICustomPropertyProvider
            {
                public Type Type => typeof(MyType);
                public ICustomProperty GetCustomProperty(string name) => null;
                public ICustomProperty GetIndexedProperty(string name, Type type) => null;
                public string GetStringRepresentation() => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task TypeWithImplicitInterfaceImplementation_Incomplete_Warns()
    {
        string source = """
            using System;
            using Microsoft.UI.Xaml.Data;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial class MyType : ICustomPropertyProvider
            {
                public Type Type => typeof(MyType);
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source, expectedDiagnostics: [
            // /0/Test0.cs(6,22): error CSWINRT2003: The type 'MyType' cannot use '[GeneratedCustomPropertyProvider]' because it already has or inherits implementations for one or more 'ICustomPropertyProvider' members
            VerifyCS.Diagnostic().WithSpan(6, 22, 6, 28).WithArguments("MyType"),
            // /0/Test0.cs(6,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetCustomProperty(string)'
            DiagnosticResult.CompilerError("CS0535").WithSpan(6, 31, 6, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string)"),
            // /0/Test0.cs(6,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetIndexedProperty(string, Type)'
            DiagnosticResult.CompilerError("CS0535").WithSpan(6, 31, 6, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string, System.Type)"),
            // /0/Test0.cs(6,31): error CS0535: 'MyType' does not implement interface member 'ICustomPropertyProvider.GetStringRepresentation()'
            DiagnosticResult.CompilerError("CS0535").WithSpan(6, 31, 6, 54).WithArguments("MyType", "Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()")]);
    }

    [TestMethod]
    public async Task TypeInheritingFromImplementingBase_Warns()
    {
        string source = """
            using System;
            using Microsoft.UI.Xaml.Data;
            using WindowsRuntime.Xaml;

            public class BaseType : ICustomPropertyProvider
            {
                Type ICustomPropertyProvider.Type => typeof(BaseType);
                ICustomProperty ICustomPropertyProvider.GetCustomProperty(string name) => null;
                ICustomProperty ICustomPropertyProvider.GetIndexedProperty(string name, Type type) => null;
                string ICustomPropertyProvider.GetStringRepresentation() => "";
            }

            [GeneratedCustomPropertyProvider]
            public partial class {|CSWINRT2003:MyType|} : BaseType;
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
