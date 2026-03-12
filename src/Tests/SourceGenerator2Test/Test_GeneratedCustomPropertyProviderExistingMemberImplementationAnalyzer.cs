// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

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
    [DataRow("class")]
    [DataRow("struct")]
    public async Task ValidType_NoMembers_DoesNotWarn(string modifier)
    {
        string source = $$"""            
            using Microsoft.UI.Xaml.Data;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial {{modifier}} MyType : ICustomPropertyProvider
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source);
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
            public partial class {|CSWINRT2003:MyType|} : ICustomPropertyProvider
            {
                Type ICustomPropertyProvider.Type => typeof(MyType);
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source);
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
            public partial class {|CSWINRT2003:MyType|} : ICustomPropertyProvider
            {
                public Type Type => typeof(MyType);
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderExistingMemberImplementationAnalyzer>.VerifyAnalyzerAsync(source);
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
