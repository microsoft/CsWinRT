// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

[TestClass]
public class Test_CustomPropertyProviderGenerator
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void HiddenProperties_GenericBase_UsesMostDerivedProperty(bool explicitSelection)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public class Behavior
            {
                public object AssociatedObject { get; private set; } = "target";

                public int Inherited { get; set; } = 42;
            }

            public class Behavior<T> : Behavior where T : class
            {
                public new T AssociatedObject => (T)base.AssociatedObject;
            }

            [GeneratedCustomPropertyProvider{{(explicitSelection ? "([nameof(AssociatedObject), nameof(IsEnabled), nameof(Inherited)], [])" : "")}}]
            public sealed partial class MyType : Behavior<string>
            {
                public bool IsEnabled { get; set; }

                public static MyType Create() => new MyType();
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);

        AssertReadOnlyProperty(provider, "AssociatedObject", "target");
        Assert.AreEqual(3, provider.GetType().Assembly.GetTypes().Count(type => typeof(ICustomProperty).IsAssignableFrom(type)));

        ICustomProperty isEnabled = provider.GetCustomProperty("IsEnabled");

        Assert.IsNotNull(isEnabled);
        Assert.AreEqual(typeof(bool), isEnabled.Type);
        Assert.IsTrue(isEnabled.CanRead);
        Assert.IsTrue(isEnabled.CanWrite);
        Assert.AreEqual(false, isEnabled.GetValue(provider));

        isEnabled.SetValue(provider, true);

        Assert.AreEqual(true, isEnabled.GetValue(provider));

        ICustomProperty inherited = provider.GetCustomProperty("Inherited");

        Assert.IsNotNull(inherited);
        Assert.AreEqual(typeof(int), inherited.Type);
        Assert.IsTrue(inherited.CanRead);
        Assert.IsTrue(inherited.CanWrite);
        Assert.AreEqual(42, inherited.GetValue(provider));

        inherited.SetValue(provider, 100);

        Assert.AreEqual(100, inherited.GetValue(provider));
    }

    [TestMethod]
    [DataRow("object", "get;", true, false, false)]
    [DataRow("object", "get;", true, false, true)]
    [DataRow("string", "get;", true, false, false)]
    [DataRow("string", "get;", true, false, true)]
    [DataRow("object", "get; set;", true, true, false)]
    [DataRow("string", "get; set;", true, true, true)]
    [DataRow("object", "get; init;", true, false, false)]
    [DataRow("string", "get; init;", true, false, true)]
    [DataRow("object", "private get; set;", false, true, false)]
    [DataRow("string", "private get; set;", false, true, true)]
    [DataRow("object", "get; private set;", true, false, false)]
    [DataRow("string", "get; private set;", true, false, true)]
    public void HiddenProperties_NonGeneric_UsesMostDerivedAccessors(string baseType, string accessors, bool canRead, bool canWrite, bool explicitSelection)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public class Base
            {
                public {{baseType}} Value { get; set; } = "Base";
            }

            [GeneratedCustomPropertyProvider{{(explicitSelection ? "([nameof(Value), nameof(StoredValue)], [])" : "")}}]
            public partial class MyType : Base
            {
                public new string Value { {{accessors}} } = "Derived";

                public string StoredValue => Value;

                public static MyType Create() => new MyType();
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);
        ICustomProperty property = provider.GetCustomProperty("Value");

        Assert.IsNotNull(property);
        Assert.AreEqual("Value", property.Name);
        Assert.AreEqual(typeof(string), property.Type);
        Assert.AreEqual(canRead, property.CanRead);
        Assert.AreEqual(canWrite, property.CanWrite);
        Assert.AreEqual(2, provider.GetType().Assembly.GetTypes().Count(type => typeof(ICustomProperty).IsAssignableFrom(type)));

        if (canRead)
        {
            Assert.AreEqual("Derived", property.GetValue(provider));
        }
        else
        {
            Assert.ThrowsExactly<NotSupportedException>(() => property.GetValue(provider));
        }

        if (canWrite)
        {
            property.SetValue(provider, "Updated");
        }
        else
        {
            Assert.ThrowsExactly<NotSupportedException>(() => property.SetValue(provider, "Updated"));
        }

        AssertReadOnlyProperty(provider, "StoredValue", canWrite ? "Updated" : "Derived");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void HiddenProperties_IndexerOverloads_ArePreserved(bool explicitSelection)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public class Base
            {
                public object Value => "Base";

                public int this[int index] => index + 1;
            }

            [GeneratedCustomPropertyProvider{{(explicitSelection ? "([nameof(Value)], [typeof(int), typeof(string)])" : "")}}]
            public partial class MyType : Base
            {
                public new string Value => "Derived";

                public string this[string index] => index + "!";

                public static MyType Create() => new MyType();
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);

        AssertReadOnlyProperty(provider, "Value", "Derived");
        Assert.AreEqual(3, provider.GetType().Assembly.GetTypes().Count(type => typeof(ICustomProperty).IsAssignableFrom(type)));

        ICustomProperty intIndexer = provider.GetIndexedProperty("Item", typeof(int));
        ICustomProperty stringIndexer = provider.GetIndexedProperty("Item", typeof(string));

        Assert.IsNotNull(intIndexer);
        Assert.IsNotNull(stringIndexer);
        Assert.AreEqual(typeof(int), intIndexer.Type);
        Assert.AreEqual(typeof(string), stringIndexer.Type);
        Assert.IsTrue(intIndexer.CanRead);
        Assert.IsFalse(intIndexer.CanWrite);
        Assert.IsTrue(stringIndexer.CanRead);
        Assert.IsFalse(stringIndexer.CanWrite);
        Assert.AreEqual(43, intIndexer.GetIndexedValue(provider, 42));
        Assert.AreEqual("key!", stringIndexer.GetIndexedValue(provider, "key"));
    }

    [TestMethod]
    public void HiddenProperties_OverriddenProperty_UsesHiddenDefinition()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public class Base
            {
                public virtual object Value => "Base";
            }

            public class Intermediate : Base
            {
                public new virtual string Value => "Intermediate";
            }

            [GeneratedCustomPropertyProvider]
            public partial class MyType : Intermediate
            {
                public override string Value => "Derived";

                public static MyType Create() => new MyType();
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);

        AssertReadOnlyProperty(provider, "Value", "Derived");
        Assert.AreEqual(1, provider.GetType().Assembly.GetTypes().Count(type => typeof(ICustomProperty).IsAssignableFrom(type)));
    }

    [TestMethod]
    public void HiddenProperties_PartialProperty_UsesDefinition()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public class Base
            {
                public object Value { get; set; } = "Base";
            }

            [GeneratedCustomPropertyProvider]
            public partial class MyType : Base
            {
                private string value = "Derived";

                public new partial string Value { get; set; }

                public new partial string Value
                {
                    get => value;
                    set => this.value = value;
                }

                public static MyType Create() => new MyType();
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);
        ICustomProperty property = provider.GetCustomProperty("Value");

        Assert.IsNotNull(property);
        Assert.AreEqual(typeof(string), property.Type);
        Assert.IsTrue(property.CanRead);
        Assert.IsTrue(property.CanWrite);
        Assert.AreEqual("Derived", property.GetValue(provider));
        Assert.AreEqual(1, provider.GetType().Assembly.GetTypes().Count(type => typeof(ICustomProperty).IsAssignableFrom(type)));

        property.SetValue(provider, "Updated");

        Assert.AreEqual("Updated", property.GetValue(provider));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void HiddenProperties_UnboxableProperty_DoesNotExposeHiddenBase(bool explicitSelection)
    {
        string source = $$"""
            using System;
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public class Base
            {
                public string Value => "Base";

                public string Inherited => "Inherited";
            }

            [GeneratedCustomPropertyProvider{{(explicitSelection ? "([nameof(Value), nameof(Inherited)], [])" : "")}}]
            public partial class MyType : Base
            {
                public new Span<int> Value => default;

                public static MyType Create() => new MyType();
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);

        Assert.IsNull(provider.GetCustomProperty("Value"));
        AssertReadOnlyProperty(provider, "Inherited", "Inherited");
        Assert.AreEqual(1, provider.GetType().Assembly.GetTypes().Count(type => typeof(ICustomProperty).IsAssignableFrom(type)));
    }

    [TestMethod]
    [DataRow("sealed partial class", false)]
    [DataRow("sealed partial class", true)]
    [DataRow("readonly partial struct", false)]
    [DataRow("readonly partial struct", true)]
    public void InitOnlyProperties_AreReadOnly(string typeDeclaration, bool isRequired)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public {{typeDeclaration}} MyType
            {
                public {{(isRequired ? "required " : "")}}double Width { get; init; }

                public {{(isRequired ? "required " : "")}}string Text { get; init; }

                public static MyType Create() => new MyType { Width = 42, Text = "Initialized" };
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);

        AssertReadOnlyProperty(provider, "Width", 42.0);
        AssertReadOnlyProperty(provider, "Text", "Initialized");
    }

    [TestMethod]
    public void InitOnlyProperties_MixedAccessors_PreserveWritability()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyType
            {
                private int indexedValue = 5;

                public required string InitOnly { get; init; }

                public string ReadOnly => "Read only";

                public string Writable { get; set; } = "Before";

                public string PrivateSetter { get; private set; } = "Private setter";

                public string ProtectedSetter { get; protected set; } = "Protected setter";

                public string InternalSetter { get; internal set; } = "Internal setter";

                public string PrivateInit { get; private init; } = "Private init";

                public int this[int index]
                {
                    get => indexedValue + index;
                    set => indexedValue = value - index;
                }

                public static MyType Create() => new MyType { InitOnly = "Initialized" };
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);

        AssertReadOnlyProperty(provider, "InitOnly", "Initialized");
        AssertReadOnlyProperty(provider, "ReadOnly", "Read only");
        AssertReadOnlyProperty(provider, "PrivateSetter", "Private setter");
        AssertReadOnlyProperty(provider, "ProtectedSetter", "Protected setter");
        AssertReadOnlyProperty(provider, "InternalSetter", "Internal setter");
        AssertReadOnlyProperty(provider, "PrivateInit", "Private init");

        ICustomProperty writable = provider.GetCustomProperty("Writable");

        Assert.IsNotNull(writable);
        Assert.IsTrue(writable.CanRead);
        Assert.IsTrue(writable.CanWrite);
        Assert.AreEqual("Before", writable.GetValue(provider));

        writable.SetValue(provider, "After");

        Assert.AreEqual("After", writable.GetValue(provider));

        ICustomProperty indexer = provider.GetIndexedProperty("Item", typeof(int));

        Assert.IsNotNull(indexer);
        Assert.IsTrue(indexer.CanRead);
        Assert.IsTrue(indexer.CanWrite);
        Assert.AreEqual(7, indexer.GetIndexedValue(provider, 2));

        indexer.SetIndexedValue(provider, 10, 2);

        Assert.AreEqual(10, indexer.GetIndexedValue(provider, 2));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InitOnlyProperties_InheritedProperties_RespectSelection(bool explicitSelection)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            public class Base
            {
                public required string Inherited { get; init; }

                public string Excluded { get; init; }
            }

            [GeneratedCustomPropertyProvider{{(explicitSelection ? "([\"Inherited\", \"Declared\"], [])" : "")}}]
            public partial class MyType : Base
            {
                public string Declared { get; init; }

                public static MyType Create() => new MyType
                {
                    Inherited = "Base value",
                    Declared = "Derived value",
                    Excluded = "Excluded value"
                };
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);

        AssertReadOnlyProperty(provider, "Inherited", "Base value");
        AssertReadOnlyProperty(provider, "Declared", "Derived value");

        if (explicitSelection)
        {
            Assert.IsNull(provider.GetCustomProperty("Excluded"));
        }
        else
        {
            AssertReadOnlyProperty(provider, "Excluded", "Excluded value");
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InitOnlyIndexer_IsReadOnly(bool explicitSelection)
    {
        string source = $$"""
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider{{(explicitSelection ? "([], [typeof(int)])" : "")}}]
            public partial class MyType
            {
                private int indexedValue;

                public int this[int index]
                {
                    get => indexedValue + index;
                    init => indexedValue = value - index;
                }

                public static MyType Create() => new MyType { [2] = 42 };
            }
            """;

        ICustomPropertyProvider provider = CreateProvider(source);
        ICustomProperty indexer = provider.GetIndexedProperty("Item", typeof(int));

        Assert.IsNotNull(indexer);
        Assert.IsTrue(indexer.CanRead);
        Assert.IsFalse(indexer.CanWrite);
        Assert.AreEqual(42, indexer.GetIndexedValue(provider, 2));
        Assert.ThrowsExactly<NotSupportedException>(() => indexer.SetIndexedValue(provider, 100, 2));
        Assert.AreEqual(42, indexer.GetIndexedValue(provider, 2));
    }

    private static ICustomPropertyProvider CreateProvider(string source)
    {
        Assembly assembly = CSharpGeneratorTest<CustomPropertyProviderGenerator>.Compile(source);
        MethodInfo factory = assembly.GetType("MyNamespace.MyType", throwOnError: true).GetMethod("Create");

        Assert.IsNotNull(factory);

        return (ICustomPropertyProvider)factory.Invoke(null, null);
    }

    private static void AssertReadOnlyProperty(ICustomPropertyProvider provider, string name, object expectedValue)
    {
        ICustomProperty property = provider.GetCustomProperty(name);

        Assert.IsNotNull(property);
        Assert.IsTrue(property.CanRead);
        Assert.IsFalse(property.CanWrite);
        Assert.AreEqual(name, property.Name);
        Assert.AreEqual(expectedValue.GetType(), property.Type);
        Assert.AreEqual(expectedValue, property.GetValue(provider));
        Assert.ThrowsExactly<NotSupportedException>(() => property.SetValue(provider, expectedValue));
        Assert.AreEqual(expectedValue, property.GetValue(provider));
    }

    [TestMethod]
    public async Task ValidClass_MixedProperties()
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

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return name switch
                        {
                            nameof(Name) => global::WindowsRuntime.Xaml.Generated.MyClass_Name.Instance,
                            nameof(Age) => global::WindowsRuntime.Xaml.Generated.MyClass_Age.Instance,
                            _ => null
                        };
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        if (type == typeof(int))
                        {
                            return global::WindowsRuntime.Xaml.Generated.MyClass_this__int.Instance;
                        }

                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }

            namespace WindowsRuntime.Xaml.Generated
            {
                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::Microsoft.UI.Xaml.Data;

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass.Name"/>.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_Name : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_Name"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_Name Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => false;

                    /// <inheritdoc/>
                    public string Name => "Name";

                    /// <inheritdoc/>
                    public Type Type => typeof(string);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        return ((global::MyNamespace.MyClass)target).Name;
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass.Age"/>.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_Age : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_Age"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_Age Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => true;

                    /// <inheritdoc/>
                    public string Name => "Age";

                    /// <inheritdoc/>
                    public Type Type => typeof(int);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        return ((global::MyNamespace.MyClass)target).Age;
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        ((global::MyNamespace.MyClass)target).Age = (int)value;
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass"/>'s <see cref="int"/> indexer.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_this__int : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_this__int"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_this__int Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => true;

                    /// <inheritdoc/>
                    public string Name => "this";

                    /// <inheritdoc/>
                    public Type Type => typeof(int);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        return ((global::MyNamespace.MyClass)target)[(int)index];
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        ((global::MyNamespace.MyClass)target)[(int)index] = (int)value;
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }

    [TestMethod]
    public async Task ValidClass_NoProperties()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
            }
            """;

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }

    [TestMethod]
    public async Task ValidClass_NormalPropertiesOnly()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
                public string Name => "";

                public int Age { get; set; }
            }
            """;

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return name switch
                        {
                            nameof(Name) => global::WindowsRuntime.Xaml.Generated.MyClass_Name.Instance,
                            nameof(Age) => global::WindowsRuntime.Xaml.Generated.MyClass_Age.Instance,
                            _ => null
                        };
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }

            namespace WindowsRuntime.Xaml.Generated
            {
                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::Microsoft.UI.Xaml.Data;

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass.Name"/>.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_Name : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_Name"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_Name Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => false;

                    /// <inheritdoc/>
                    public string Name => "Name";

                    /// <inheritdoc/>
                    public Type Type => typeof(string);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        return ((global::MyNamespace.MyClass)target).Name;
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass.Age"/>.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_Age : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_Age"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_Age Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => true;

                    /// <inheritdoc/>
                    public string Name => "Age";

                    /// <inheritdoc/>
                    public Type Type => typeof(int);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        return ((global::MyNamespace.MyClass)target).Age;
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        ((global::MyNamespace.MyClass)target).Age = (int)value;
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }

    [TestMethod]
    public async Task ValidClass_IndexerPropertiesOnly()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
                public int this[int index]
                {
                    get => 0;
                    set { }
                }
            }
            """;

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        if (type == typeof(int))
                        {
                            return global::WindowsRuntime.Xaml.Generated.MyClass_this__int.Instance;
                        }

                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }

            namespace WindowsRuntime.Xaml.Generated
            {
                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::Microsoft.UI.Xaml.Data;

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass"/>'s <see cref="int"/> indexer.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_this__int : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_this__int"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_this__int Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => true;

                    /// <inheritdoc/>
                    public string Name => "this";

                    /// <inheritdoc/>
                    public Type Type => typeof(int);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        return ((global::MyNamespace.MyClass)target)[(int)index];
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        ((global::MyNamespace.MyClass)target)[(int)index] = (int)value;
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }

    [TestMethod]
    public async Task ValidClass_ReadOnlyProperty()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
                public string Name => "";
            }
            """;

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return name switch
                        {
                            nameof(Name) => global::WindowsRuntime.Xaml.Generated.MyClass_Name.Instance,
                            _ => null
                        };
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }

            namespace WindowsRuntime.Xaml.Generated
            {
                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::Microsoft.UI.Xaml.Data;

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass.Name"/>.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_Name : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_Name"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_Name Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => false;

                    /// <inheritdoc/>
                    public string Name => "Name";

                    /// <inheritdoc/>
                    public Type Type => typeof(string);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        return ((global::MyNamespace.MyClass)target).Name;
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }

    [TestMethod]
    public async Task ValidClass_WriteOnlyProperty()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
                public string Name { set { } }
            }
            """;

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return name switch
                        {
                            nameof(Name) => global::WindowsRuntime.Xaml.Generated.MyClass_Name.Instance,
                            _ => null
                        };
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }

            namespace WindowsRuntime.Xaml.Generated
            {
                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::Microsoft.UI.Xaml.Data;

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass.Name"/>.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_Name : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_Name"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_Name Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => false;

                    /// <inheritdoc/>
                    public bool CanWrite => true;

                    /// <inheritdoc/>
                    public string Name => "Name";

                    /// <inheritdoc/>
                    public Type Type => typeof(string);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        ((global::MyNamespace.MyClass)target).Name = (string)value;
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }

    [TestMethod]
    public async Task ValidClass_StaticProperty()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
                public static int Count { get; set; }
            }
            """;

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return name switch
                        {
                            nameof(Count) => global::WindowsRuntime.Xaml.Generated.MyClass_Count.Instance,
                            _ => null
                        };
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }

            namespace WindowsRuntime.Xaml.Generated
            {
                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::Microsoft.UI.Xaml.Data;

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass.Count"/>.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_Count : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_Count"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_Count Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => true;

                    /// <inheritdoc/>
                    public string Name => "Count";

                    /// <inheritdoc/>
                    public Type Type => typeof(int);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        return global::MyNamespace.MyClass.Count;
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        global::MyNamespace.MyClass.Count = (int)value;
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }

    [TestMethod]
    public async Task ValidClass_ReadOnlyIndexer()
    {
        const string source = """
            using WindowsRuntime.Xaml;

            namespace MyNamespace;

            [GeneratedCustomPropertyProvider]
            public partial class MyClass
            {
                public string this[int index] => "";
            }
            """;

        const string result = """
            // <auto-generated/>
            #pragma warning disable

            namespace MyNamespace
            {
                /// <inheritdoc cref="MyClass"/>
                partial class MyClass : Microsoft.UI.Xaml.Data.ICustomPropertyProvider
                {
                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    global::System.Type Microsoft.UI.Xaml.Data.ICustomPropertyProvider.Type => typeof(MyClass);

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetCustomProperty(string name)
                    {
                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    Microsoft.UI.Xaml.Data.ICustomProperty Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetIndexedProperty(string name, global::System.Type type)
                    {
                        if (type == typeof(int))
                        {
                            return global::WindowsRuntime.Xaml.Generated.MyClass_this__int.Instance;
                        }

                        return null;
                    }

                    /// <inheritdoc/>
                    [global::System.CodeDom.Compiler.GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                    string Microsoft.UI.Xaml.Data.ICustomPropertyProvider.GetStringRepresentation()
                    {
                        return ToString();
                    }
                }
            }

            namespace WindowsRuntime.Xaml.Generated
            {
                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::Microsoft.UI.Xaml.Data;

                /// <summary>
                /// The <see cref="ICustomProperty"/> implementation for <see cref="MyNamespace.MyClass"/>'s <see cref="int"/> indexer.
                /// </summary>
                [GeneratedCode("CustomPropertyProviderGenerator", <ASSEMBLY_VERSION>)]
                [DebuggerNonUserCode]
                [ExcludeFromCodeCoverage]
                file sealed class MyClass_this__int : ICustomProperty
                {
                    /// <summary>
                    /// Gets the singleton <see cref="MyClass_this__int"/> instance for this custom property.
                    /// </summary>
                    public static readonly MyClass_this__int Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => true;

                    /// <inheritdoc/>
                    public bool CanWrite => false;

                    /// <inheritdoc/>
                    public string Name => "this";

                    /// <inheritdoc/>
                    public Type Type => typeof(string);

                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        return ((global::MyNamespace.MyClass)target)[(int)index];
                    }

                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            """;

        CSharpGeneratorTest<CustomPropertyProviderGenerator>.VerifySources(source, ("MyNamespace.MyClass.g.cs", result));
    }
}