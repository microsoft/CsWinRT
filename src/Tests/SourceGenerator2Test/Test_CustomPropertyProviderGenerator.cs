// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public class Test_CustomPropertyProviderGenerator
{
    [TestMethod]
    public async Task SimpleShader_ComputeShader()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
                public string Name => "";

                public int Age { get; set; }

                public int this[int index]
                {
                    get => 0;
                    set { }
                }
            }
            """;

        const string result = """"
            
            """";

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }
}