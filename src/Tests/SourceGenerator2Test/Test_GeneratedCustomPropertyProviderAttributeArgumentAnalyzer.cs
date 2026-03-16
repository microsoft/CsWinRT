// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    WindowsRuntime.SourceGenerator.Diagnostics.GeneratedCustomPropertyProviderAttributeArgumentAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public class Test_GeneratedCustomPropertyProviderAttributeArgumentAnalyzer
{
    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public async Task DefaultConstructor_DoesNotWarn(string modifier)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider]
            public partial {{modifier}} MyType
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task EmptyArrays_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { })]
            public partial class MyType
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ValidPropertyName_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Name" }, new Type[] { })]
            public partial class MyType
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ValidIndexerType_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { typeof(int) })]
            public partial class MyType
            {
                public string this[int index] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ValidPropertyNameAndIndexerType_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Name" }, new Type[] { typeof(int) })]
            public partial class MyType
            {
                public string Name { get; set; }
                public string this[int index] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task InheritedPropertyName_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            public class BaseType
            {
                public string Name { get; set; }
            }

            [GeneratedCustomPropertyProvider(new string[] { "Name" }, new Type[] { })]
            public partial class MyType : BaseType;
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task InheritedIndexerType_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            public class BaseType
            {
                public string this[int index] => "";
            }

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { typeof(int) })]
            public partial class MyType : BaseType;
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task MultipleValidPropertyNames_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Name", "Age" }, new Type[] { })]
            public partial class MyType
            {
                public string Name { get; set; }
                public int Age { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task OverriddenPropertyName_DoesNotWarn()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            public class BaseType
            {
                public virtual string Name { get; set; }
            }

            [GeneratedCustomPropertyProvider(new string[] { "Name" }, new Type[] { })]
            public partial class MyType : BaseType
            {
                public override string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task NullPropertyName_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { null }, new Type[] { })]
            public partial class {|CSWINRT2004:MyType|}
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task NullAmongValidPropertyNames_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Name", null }, new Type[] { })]
            public partial class {|CSWINRT2004:MyType|}
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task NullIndexerType_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { null })]
            public partial class {|CSWINRT2005:MyType|}
            {
                public string this[int index] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task NullAmongValidIndexerTypes_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { typeof(int), null })]
            public partial class {|CSWINRT2005:MyType|}
            {
                public string this[int index] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PropertyNameNotFound_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Missing" }, new Type[] { })]
            public partial class {|CSWINRT2006:MyType|}
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PropertyNameMatchesPrivateProperty_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Secret" }, new Type[] { })]
            public partial class {|CSWINRT2006:MyType|}
            {
                private string Secret { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task MultiplePropertyNamesNotFound_WarnsForEach()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Missing1", "Missing2" }, new Type[] { })]
            public partial class MyType
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source, expectedDiagnostics: [
            VerifyCS.Diagnostic("CSWINRT2006").WithSpan(5, 22, 5, 28).WithArguments("Missing1", "MyType"),
            VerifyCS.Diagnostic("CSWINRT2006").WithSpan(5, 22, 5, 28).WithArguments("Missing2", "MyType")]);
    }

    [TestMethod]
    public async Task IndexerTypeNotFound_WrongType_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { typeof(string) })]
            public partial class {|CSWINRT2007:MyType|}
            {
                public string this[int index] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task IndexerTypeNotFound_NoIndexer_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { typeof(int) })]
            public partial class {|CSWINRT2007:MyType|}
            {
                public string Name { get; set; }
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task IndexerTypeMatchesMultiParameterIndexer_Warns()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { typeof(int) })]
            public partial class {|CSWINRT2007:MyType|}
            {
                public string this[int row, int col] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task MultipleIndexerTypesNotFound_WarnsForEach()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { }, new Type[] { typeof(string), typeof(double) })]
            public partial class MyType
            {
                public string this[int index] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source, expectedDiagnostics: [
            VerifyCS.Diagnostic("CSWINRT2007").WithSpan(5, 22, 5, 28).WithArguments("double", "MyType"),
            VerifyCS.Diagnostic("CSWINRT2007").WithSpan(5, 22, 5, 28).WithArguments("string", "MyType")]);
    }

    [TestMethod]
    public async Task MixedInvalidArguments_WarnsForEach()
    {
        string source = """
            using System;
            using WindowsRuntime.Xaml;

            [GeneratedCustomPropertyProvider(new string[] { "Missing", null }, new Type[] { typeof(string), null })]
            public partial class MyType
            {
                public string Name { get; set; }
                public string this[int index] => "";
            }
            """;

        await CSharpAnalyzerTest<GeneratedCustomPropertyProviderAttributeArgumentAnalyzer>.VerifyAnalyzerAsync(source, expectedDiagnostics: [
            VerifyCS.Diagnostic("CSWINRT2004").WithSpan(5, 22, 5, 28).WithArguments("MyType"),
            VerifyCS.Diagnostic("CSWINRT2005").WithSpan(5, 22, 5, 28).WithArguments("MyType"),
            VerifyCS.Diagnostic("CSWINRT2006").WithSpan(5, 22, 5, 28).WithArguments("Missing", "MyType"),
            VerifyCS.Diagnostic("CSWINRT2007").WithSpan(5, 22, 5, 28).WithArguments("string", "MyType")]);
    }
}
