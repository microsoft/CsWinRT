// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

[TestClass]
public class Test_AttributeAccessibility
{
    [TestMethod]
    [DataRow("typeof(HiddenType)", false)]
    [DataRow("typeof(HiddenType[])", false)]
    [DataRow("typeof(System.Collections.Generic.List<HiddenType>)", false)]
    [DataRow("typeof(HiddenContainer.NestedType)", false)]
    [DataRow("typeof(string)", true)]
    [DataRow("typeof(System.Collections.Generic.List<string>)", true)]
    public void TypeArgument_IsCopiedOnlyWhenPubliclyAccessible(string argument, bool expected)
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes($$"""
            using System;
            using System.Diagnostics;

            internal class HiddenType { }
            internal class HiddenContainer
            {
                public class NestedType { }
            }

            [DebuggerTypeProxy({{argument}})]
            public sealed class Component { }
            """);

        Assert.AreEqual(expected, attributes["Component"].Contains("System.Diagnostics.DebuggerTypeProxyAttribute"));
    }

    [TestMethod]
    public void NonPublicAttributeAndConstructor_AreNotCopied()
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            using System;

            internal sealed class HiddenAttribute : Attribute { }

            public sealed class InternalConstructorAttribute : Attribute
            {
                internal InternalConstructorAttribute() { }
            }

            [Hidden]
            [InternalConstructor]
            public sealed class Component { }
            """);

        Assert.IsFalse(attributes["Component"].Contains("HiddenAttribute"));
        Assert.IsFalse(attributes["Component"].Contains("InternalConstructorAttribute"));
    }

    [TestMethod]
    public void NamedTypeArgument_IsNotCopiedWhenNonPublic()
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            using System.Diagnostics;

            internal class HiddenType { }

            [DebuggerDisplay("value", Target = typeof(HiddenType))]
            public sealed class Component { }
            """);

        Assert.IsFalse(attributes["Component"].Contains("System.Diagnostics.DebuggerDisplayAttribute"));
    }

    [TestMethod]
    [DataRow("typeof(HiddenType)", false)]
    [DataRow("typeof(string)", true)]
    public void BoxedTypeInArray_IsCopiedOnlyWhenPubliclyAccessible(string value, bool expected)
    {
        var attributes = WinMDGeneratorRunner.GetGeneratedAttributes($$"""
            using System;

            internal class HiddenType { }

            public sealed class TypeValuesAttribute : Attribute
            {
                public TypeValuesAttribute(params object[] values) { }
            }

            [TypeValues(new object[] { {{value}} })]
            public sealed class Component { }
            """);

        Assert.AreEqual(expected, attributes["Component"].Contains("TypeValuesAttribute"));
    }
}
