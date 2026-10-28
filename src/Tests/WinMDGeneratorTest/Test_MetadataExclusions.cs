// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// Tests exclusions against the metadata emitted by the actual WinMD generator.
/// </summary>
[TestClass]
public sealed class Test_MetadataExclusions
{
    [TestMethod]
    public void IgnoredTypesAndMembers_AreAbsentFromAllGeneratedInterfaces()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public class Managed<T> { public List<T> Values { get; set; } }
            [ExcludeFromWindowsMetadata]
            public struct ManagedValue { public List<int> Values; }
            [ExcludeFromWindowsMetadata]
            public enum ManagedEnum { Value }
            [ExcludeFromWindowsMetadata]
            public delegate List<int> ManagedDelegate();
            [ExcludeFromWindowsMetadata]
            public interface IManaged { List<int> Work(); }
            public sealed class Visible : IManaged
            {
                public Visible() { }
                [ExcludeFromWindowsMetadata]
                public Visible(List<int> values) { }
                public int Work(int value) => value;
                [ExcludeFromWindowsMetadata]
                public List<int> Work(string value) => null;
                List<int> IManaged.Work() => null;
                [ExcludeFromWindowsMetadata]
                public List<int> Values { get; set; }
                [ExcludeFromWindowsMetadata]
                public static List<int> StaticValues { get; set; }
                [ExcludeFromWindowsMetadata]
                public event Action HiddenEvent { add { } remove { } }
                [ExcludeFromWindowsMetadata]
                public static void HiddenStatic() { }
                public int Value { get; set; }
            }
            """;

        (Dictionary<string, string> methods, Dictionary<string, string[]> interfaces) =
            WinMDGeneratorRunner.GetGeneratedApiSurface(source);

        CollectionAssert.AreEquivalent(new[] { "Component.Visible", "Component.IVisibleClass" },
            interfaces.Keys.Where(static name => name != "<Module>").ToArray());
        CollectionAssert.AreEquivalent(new[]
        {
            "Component.Visible..ctor", "Component.Visible.Work", "Component.Visible.get_Value", "Component.Visible.put_Value",
            "Component.IVisibleClass.Work", "Component.IVisibleClass.get_Value", "Component.IVisibleClass.put_Value"
        }, methods.Keys.ToArray());
        Assert.AreEqual("Int32(Int32)", methods["Component.IVisibleClass.Work"]);
        CollectionAssert.AreEqual(new[] { "TestInput::Component.IVisibleClass" }, interfaces["Component.Visible"]);
    }

    [TestMethod]
    public void ExcludedInterface_PreservesUnmarkedPublicMethods()
    {
        (Dictionary<string, string> methods, Dictionary<string, string[]> interfaces) =
            WinMDGeneratorRunner.GetGeneratedApiSurface("""
                using WindowsRuntime.InteropServices;
                namespace Component;
                [ExcludeFromWindowsMetadata]
                public interface IManaged { int Work(); }
                public sealed class Visible : IManaged
                {
                    public int Work() => 42;
                }
                """);

        Assert.IsFalse(interfaces.ContainsKey("Component.IManaged"));
        Assert.AreEqual("Int32()", methods["Component.IVisibleClass.Work"]);
        CollectionAssert.AreEqual(new[] { "TestInput::Component.IVisibleClass" }, interfaces["Component.Visible"]);
    }

    [TestMethod]
    public void ExcludedAutoPropertyAndFieldLikeEvent_DoNotLeaveAccessors()
    {
        string[] members = WinMDGeneratorRunner.GetGeneratedMemberNames("""
            using WindowsRuntime.InteropServices;
            namespace Component;
            public sealed class Visible
            {
                [ExcludeFromWindowsMetadata]
                public int HiddenProperty { get; set; }
                [ExcludeFromWindowsMetadata]
                public event System.EventHandler HiddenEvent;
                public int Value { get; set; }
            }
            """);

        Assert.IsFalse(members.Any(static name => name.Contains("Hidden", System.StringComparison.Ordinal)));
        CollectionAssert.Contains(members, "Component.IVisibleClass.get_Value");
        CollectionAssert.Contains(members, "Component.IVisibleClass.put_Value");
    }

    [TestMethod]
    public void IgnoredConstructors_DoNotSynthesizeActivation()
    {
        const string source = """
            namespace Component;
            public sealed class Visible
            {
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                public Visible() { }
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                public Visible(int value) { }
                public void Work() { }
            }
            """;

        (Dictionary<string, string> methods, Dictionary<string, string[]> interfaces) =
            WinMDGeneratorRunner.GetGeneratedApiSurface(source);
        ILookup<string, string> attributes = WinMDGeneratorRunner.GetGeneratedAttributes(source);

        Assert.IsFalse(methods.ContainsKey("Component.Visible..ctor"));
        Assert.IsFalse(interfaces.ContainsKey("Component.IVisibleFactory"));
        Assert.IsFalse(attributes["Component.Visible"].Contains("Windows.Foundation.Metadata.ActivatableAttribute"));
    }

    [TestMethod]
    public void IgnoredOnlyInterface_StillGetsAnEmptyDefaultInterface()
    {
        const string source = """
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public interface IManaged { void Work(); }
            public sealed class Visible : IManaged
            {
                void IManaged.Work() { }
            }
            """;

        (Dictionary<string, string> methods, Dictionary<string, string[]> interfaces) =
            WinMDGeneratorRunner.GetGeneratedApiSurface(source);
        ILookup<string, string> attributes = WinMDGeneratorRunner.GetGeneratedAttributes(source);

        CollectionAssert.AreEqual(new[] { "TestInput::Component.IVisibleClass" }, interfaces["Component.Visible"]);
        Assert.IsTrue(interfaces.ContainsKey("Component.IVisibleClass"));
        Assert.IsFalse(methods.Keys.Any(static name => name.EndsWith(".Work", System.StringComparison.Ordinal)));
        CollectionAssert.Contains(attributes["Component.Visible:TestInput::Component.IVisibleClass"].ToArray(),
            "Windows.Foundation.Metadata.DefaultAttribute");
    }

    [TestMethod]
    public void IgnoredInterface_PreservesItsRetainedBaseContract()
    {
        (_, Dictionary<string, string[]> interfaces) = WinMDGeneratorRunner.GetGeneratedApiSurface("""
            namespace Component;
            public interface IContract { void Work(); }
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public interface IManaged : IContract;
            public sealed class Visible : IManaged
            {
                public void Work() { }
            }
            """);

        CollectionAssert.AreEqual(new[] { "TestInput::Component.IContract" }, interfaces["Component.Visible"]);
        Assert.IsFalse(interfaces.ContainsKey("Component.IManaged"));
    }

    [TestMethod]
    public void IgnoredAttributeTypes_AreNotCopiedToExportedDeclarations()
    {
        ILookup<string, string> attributes = WinMDGeneratorRunner.GetGeneratedAttributes("""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class ManagedAttribute : System.Attribute;
            public sealed class VisibleAttribute : System.Attribute;
            [Managed]
            [VisibleAttribute]
            public sealed class Visible;
            """);

        Assert.IsFalse(attributes.SelectMany(static group => group).Contains("Component.ManagedAttribute"));
        CollectionAssert.Contains(attributes["Component.Visible"].ToArray(), "Component.VisibleAttribute");
    }

    [TestMethod]
    public void IgnoredInterfaceMembers_AreNotReintroducedThroughImplementations()
    {
        (Dictionary<string, string> methods, _) = WinMDGeneratorRunner.GetGeneratedApiSurface("""
            using WindowsRuntime.InteropServices;
            namespace Component;
            public interface IContract
            {
                [ExcludeFromWindowsMetadata] void Hidden();
                void Work();
            }
            public sealed class Visible : IContract
            {
                [ExcludeFromWindowsMetadata] public void Hidden() { }
                public void Work() { }
            }
            """);

        Assert.IsFalse(methods.Keys.Any(static name => name.EndsWith(".Hidden", System.StringComparison.Ordinal)));
        Assert.IsTrue(methods.ContainsKey("Component.IContract.Work"));
    }

    [TestMethod]
    public void IgnoredInterfaceMembers_DoNotProduceExplicitImplementationRows()
    {
        string[] members = WinMDGeneratorRunner.GetGeneratedMemberNames("""
            using System;
            using WindowsRuntime.InteropServices;
            namespace Component;
            public interface IContract
            {
                [ExcludeFromWindowsMetadata] void Hidden();
                [ExcludeFromWindowsMetadata] int HiddenProperty { get; }
                [ExcludeFromWindowsMetadata] event EventHandler HiddenEvent;
                void Work();
            }
            public sealed class Visible : IContract
            {
                void IContract.Hidden() { }
                int IContract.HiddenProperty => 0;
                event EventHandler IContract.HiddenEvent { add { } remove { } }
                public void Work() { }
            }
            """);

        Assert.IsFalse(members.Any(static name => name.Contains("Hidden", System.StringComparison.Ordinal)));
        CollectionAssert.Contains(members, "Component.IContract.Work");
    }

    [TestMethod]
    public void IgnoredInterface_DoesNotPullInBaseClassInterfaces()
    {
        (_, Dictionary<string, string[]> interfaces) = WinMDGeneratorRunner.GetGeneratedApiSurface("""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public interface IManaged;
            public sealed class Visible : System.ComponentModel.Component, IManaged;
            """);

        CollectionAssert.AreEquivalent(
            new[] { "Windows.Foundation.FoundationContract::Windows.Foundation.IClosable" },
            interfaces["Component.Visible"]);
    }

    [TestMethod]
    [DataRow("IList<int>", "ICollection<int>", false, "Count")]
    [DataRow("IList<int>", "ICollection<int>", true, "Count")]
    [DataRow("IReadOnlyList<int>", "IReadOnlyCollection<int>", false, "Count")]
    [DataRow("IReadOnlyList<int>", "IReadOnlyCollection<int>", true, "Count")]
    [DataRow("IList<int>", "ICollection<int>", false, "Add")]
    [DataRow("IList<int>", "ICollection<int>", true, "Add")]
    public void MappedCollectionAncestorMembers_CannotBeIgnored(string contract, string ancestor, bool explicitImplementation, string member)
    {
        string count = explicitImplementation && member == "Count" ? $"int {ancestor}.Count" : "public int Count";
        string add = explicitImplementation && member == "Add" ? $"void {ancestor}.Add" : "public void Add";
        string countAttribute = member == "Count" ? "[WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]" : "";
        string addAttribute = member == "Add" ? "[WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]" : "";

        WinMDGeneratorRunner.AssertFailure($$"""
            using System.Collections;
            using System.Collections.Generic;
            namespace Component;
            public sealed class Items : {{contract}}
            {
                {{countAttribute}}
                {{count}} => 0;
                public int this[int index] { get => 0; set { } }
                public bool IsReadOnly => true;
                {{addAttribute}}
                {{add}}(int item) { }
                public void Clear() { }
                public bool Contains(int item) => false;
                public void CopyTo(int[] array, int index) { }
                public bool Remove(int item) => false;
                public int IndexOf(int item) => -1;
                public void Insert(int index, int item) { }
                public void RemoveAt(int index) { }
                public IEnumerator<int> GetEnumerator() => throw new System.NotSupportedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            """, error: "CSWINRTWINMDGEN0015");
    }

    [TestMethod]
    public void IgnoredEnumValues_AreOmitted()
    {
        string[] members = WinMDGeneratorRunner.GetGeneratedMemberNames("""
            namespace Component;
            public enum Value
            {
                Visible,
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                [System.Obsolete]
                Hidden
            }
            """);

        CollectionAssert.AreEquivalent(new[] { "Component.Value.value__", "Component.Value.Visible" }, members);
    }

    [TestMethod]
    public void OmittedCollectionMember_WithoutAMappedChildCanBeIgnored()
    {
        string[] members = WinMDGeneratorRunner.GetGeneratedMemberNames("""
            using System.Collections;
            using System.Collections.Generic;
            namespace Component;
            public sealed class Items : IReadOnlyCollection<int>
            {
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                public int Count => 0;
                public IEnumerator<int> GetEnumerator() => throw new System.NotSupportedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            """);

        Assert.IsFalse(members.Any(static name => name.EndsWith(".get_Count", System.StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow("Hidden")]
    [DataRow("Hidden[]")]
    [DataRow("System.Collections.Generic.IReadOnlyList<Hidden>")]
    public void ExportedReturnTypes_CannotReferenceIgnoredTypes(string type)
    {
        WinMDGeneratorRunner.AssertFailure($$"""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            public interface IContract { {{type}} Method(); }
            """, error: "CSWINRTWINMDGEN0016");
    }

    [TestMethod]
    [DataRow("Hidden value")]
    [DataRow("out Hidden[] value")]
    [DataRow("System.Collections.Generic.IReadOnlyList<Hidden> value")]
    [DataRow("System.ReadOnlySpan<Hidden> value")]
    [DataRow("System.Span<Hidden> value")]
    public void ExportedParameterTypes_CannotReferenceIgnoredTypes(string parameter)
    {
        WinMDGeneratorRunner.AssertFailure($$"""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            public interface IContract { void Method({{parameter}}); }
            """, error: "CSWINRTWINMDGEN0016");
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void IgnoredManagedBase_DerivedTypeUsesSystemObject(bool abstractBase, bool genericBase)
    {
        string modifier = abstractBase ? "abstract " : "";
        string declaration = genericBase ? "Hidden<T>" : "Hidden";
        string baseType = genericBase ? "Hidden<int>" : "Hidden";

        Dictionary<string, string?> bases = WinMDGeneratorRunner.GetGeneratedBaseTypes($$"""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public {{modifier}}class {{declaration}};
            public sealed class Visible : {{baseType}};
            """);

        Assert.AreEqual("mscorlib::System.Object", bases["Component.Visible"]);
        Assert.IsFalse(bases.Keys.Any(static name => name.StartsWith("Component.Hidden", System.StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IgnoredBase_InExportedSignatureIsRejected(bool abstractBase)
    {
        string modifier = abstractBase ? "abstract " : "";

        WinMDGeneratorRunner.AssertFailure($$"""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public {{modifier}}class Hidden;
            public sealed class Visible : Hidden
            {
                public Hidden GetBase() => this;
            }
            """, error: "CSWINRTWINMDGEN0016");
    }

    [TestMethod]
    public void IgnoredManagedBase_OnlyExplicitlyExposedMembersAreProjected()
    {
        const string source = """
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            namespace Component;
            public interface IContract { int FromInterface(int value); }
            [ExcludeFromWindowsMetadata]
            public class Hidden<T> : IContract
            {
                public List<T> ManagedOnly() => [];
                public int Forwarded(int value) => value + 1;
                public virtual int Overridden(int value) => value + 2;
                public int FromInterface(int value) => value + 3;
            }
            public sealed class Visible : Hidden<int>, IContract
            {
                public new int Forwarded(int value) => base.Forwarded(value);
                public override int Overridden(int value) => base.Overridden(value) + 4;
            }
            """;

        (Dictionary<string, string> methods, Dictionary<string, string[]> interfaces) =
            WinMDGeneratorRunner.GetGeneratedApiSurface(source);

        Assert.IsFalse(interfaces.Keys.Any(static name => name.StartsWith("Component.Hidden", System.StringComparison.Ordinal)));
        Assert.IsFalse(methods.Keys.Any(static name => name.EndsWith(".ManagedOnly", System.StringComparison.Ordinal)));
        Assert.AreEqual("Int32(Int32)", methods["Component.IVisibleClass.Forwarded"]);
        Assert.AreEqual("Int32(Int32)", methods["Component.IVisibleClass.Overridden"]);
        Assert.AreEqual("Int32(Int32)", methods["Component.IContract.FromInterface"]);
        CollectionAssert.Contains(interfaces["Component.Visible"], "TestInput::Component.IContract");
    }

    [TestMethod]
    public void UnexcludedBase_PreservesWinMDInheritance()
    {
        Dictionary<string, string?> bases = WinMDGeneratorRunner.GetGeneratedBaseTypes("""
            namespace Component;
            public class Base;
            public sealed class Visible : Base;
            """);

        Assert.AreEqual("TestInput::Component.Base", bases["Component.Visible"]);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void IgnoredBase_InterfacesRequireRedeclaration(bool abstractBase, bool redeclare)
    {
        string modifier = abstractBase ? "abstract " : "";
        string interfaces = redeclare ? ", System.IDisposable, IContract" : "";

        (Dictionary<string, string> methods, Dictionary<string, string[]> contracts) =
            WinMDGeneratorRunner.GetGeneratedApiSurface($$"""
                namespace Component;
                public interface IContract { void Work(); }
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                public {{modifier}}class Hidden<T> : System.IDisposable, IContract
                {
                    public void Dispose() { }
                    public void Work() { }
                }
                public sealed class Visible : Hidden<int>{{interfaces}};
                """);

        Assert.AreEqual(redeclare, contracts["Component.Visible"].Any(static name => name.EndsWith("::Windows.Foundation.IClosable", System.StringComparison.Ordinal)));
        Assert.AreEqual(redeclare, contracts["Component.Visible"].Contains("TestInput::Component.IContract"));
        Assert.IsFalse(methods.Keys.Any(static name => name.StartsWith("Component.IVisibleClass.", System.StringComparison.Ordinal)));
        Assert.AreEqual(!redeclare, contracts["Component.Visible"].Contains("TestInput::Component.IVisibleClass"));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IgnoredBase_InterfaceOverrideRemainsAnOrdinaryExportedMember(bool mapped)
    {
        string contract = mapped ? "System.IDisposable" : "IContract";
        string method = mapped ? "Dispose" : "Work";

        (Dictionary<string, string> methods, Dictionary<string, string[]> contracts) =
            WinMDGeneratorRunner.GetGeneratedApiSurface($$"""
                namespace Component;
                public interface IContract { void Work(); }
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                public class Hidden : {{contract}}
                {
                    public virtual void {{method}}() { }
                }
                public sealed class Visible : Hidden
                {
                    public override void {{method}}() { }
                }
                """);

        Assert.AreEqual("Void()", methods[$"Component.IVisibleClass.{method}"]);
        CollectionAssert.AreEqual(new[] { "TestInput::Component.IVisibleClass" }, contracts["Component.Visible"]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IgnoredBase_GenericInterfaceArgumentsAreValidatedOnlyWhenRedeclared(bool redeclare)
    {
        string interfaces = redeclare ? ", System.Collections.Generic.IEnumerable<HiddenValue>" : "";
        string source = $$"""
            using System.Collections;
            using System.Collections.Generic;
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class HiddenValue;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public class Hidden<T> : IEnumerable<T>
            {
                public IEnumerator<T> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public sealed class Visible : Hidden<HiddenValue>{{interfaces}};
            """;

        if (redeclare)
        {
            WinMDGeneratorRunner.AssertFailure(source, error: "CSWINRTWINMDGEN0016");
        }
        else
        {
            (_, Dictionary<string, string[]> contracts) = WinMDGeneratorRunner.GetGeneratedApiSurface(source);
            CollectionAssert.AreEqual(new[] { "TestInput::Component.IVisibleClass" }, contracts["Component.Visible"]);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void IgnoredBase_RequiredMappedAncestorMembersDependOnRedeclaration(bool redeclare)
    {
        string interfaces = redeclare ? ", IReadOnlyList<int>" : "";
        string source = $$"""
            using System.Collections;
            using System.Collections.Generic;
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public class Hidden : IReadOnlyList<int>
            {
                public virtual int Count => 0;
                public int this[int index] => 0;
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public sealed class Visible : Hidden{{interfaces}}
            {
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                public override int Count => 0;
            }
            """;

        if (redeclare)
        {
            WinMDGeneratorRunner.AssertFailure(source, error: "CSWINRTWINMDGEN0015");
        }
        else
        {
            WinMDGeneratorRunner.AssertSuccess(source);
        }
    }

    [TestMethod]
    public void UnexcludedAbstractBase_PreservesConstructedMappedContracts()
    {
        Dictionary<string, string[]> contracts = WinMDGeneratorRunner.GetGeneratedInterfaces("""
            namespace Component;
            public sealed class Visible : System.Collections.ObjectModel.KeyedCollection<string, int>
            {
                protected override string GetKeyForItem(int item) => item.ToString();
            }
            """);

        Assert.IsTrue(contracts["Component.Visible"].Any(static name => name.EndsWith("::Windows.Foundation.Collections.IVector`1<Int32>", System.StringComparison.Ordinal)));
        Assert.IsFalse(contracts["Component.Visible"].Any(static name => name.Contains('!')));
    }

    [TestMethod]
    public void ExportedAttributeArguments_CannotReferenceIgnoredTypes()
    {
        WinMDGeneratorRunner.AssertFailure("""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            [System.Runtime.Serialization.KnownType(typeof(Hidden))]
            public sealed class Visible;
            """, error: "CSWINRTWINMDGEN0016");
    }

    [TestMethod]
    public void NonExportedStructMethods_CanReferenceIgnoredTypes()
    {
        WinMDGeneratorRunner.AssertSuccess("""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            public struct Value
            {
                public int Field;
                public Hidden ManagedHelper(Hidden value) => value;
            }
            """);
    }

    [TestMethod]
    [DataRow("public interface IContract : IHidden;")]
    [DataRow("public struct Value { public HiddenValue Field; }")]
    public void ExportedContractsAndLayouts_CannotReferenceIgnoredTypes(string declaration)
    {
        WinMDGeneratorRunner.AssertFailure($$"""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] public interface IHidden;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] public struct HiddenValue { public int Value; }
            {{declaration}}
            """, error: "CSWINRTWINMDGEN0016");
    }

    [TestMethod]
    [DataRow("public struct Value { [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] public int Field; }")]
    [DataRow("public sealed class Value { public int Property { [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] get => 0; } }")]
    [DataRow("""
        public interface IContract { void Work(); }
        public sealed class Value : IContract
        {
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] public void Work() { }
        }
        """)]
    [DataRow("""
        public interface IContract { int Property { get; } }
        public sealed class Value : IContract
        {
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] public int Property => 0;
        }
        """)]
    [DataRow("""
        public interface IContract { void Work(); }
        public sealed class Value : IContract
        {
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] void IContract.Work() { }
        }
        """)]
    [DataRow("""
        public sealed class Value : System.IDisposable
        {
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata] public void Dispose() { }
        }
        """)]
    public void InvalidExclusions_AreRejectedByTheTool(string declaration)
    {
        WinMDGeneratorRunner.AssertFailure($"namespace Component; {declaration}", error: "CSWINRTWINMDGEN0015");
    }
}