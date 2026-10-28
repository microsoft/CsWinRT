// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

/// <summary>
/// Tests exclusions from an authored component's Windows Runtime surface.
/// </summary>
[TestClass]
public sealed class Test_ExcludeFromWindowsMetadataAttributeAnalyzer
{
    [TestMethod]
    public async Task IgnoredManagedSurface_DoesNotReportAuthoringRestrictions()
    {
        await CSharpAnalyzerTest<ComponentAuthoringAnalyzer>.VerifyAnalyzerAsync("""
            using System;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            namespace Component;

            [ExcludeFromWindowsMetadata]
            public class Managed<T>
            {
                public List<T> Values { private get; set; }
                public void Invalid(ref int[] array) { }
            }

            [ExcludeFromWindowsMetadata]
            public interface IManaged
            {
                List<int> Work();
            }

            public sealed class Component : IManaged
            {
                public void Work(int value) { }
                [ExcludeFromWindowsMetadata]
                public void Work(string value) { }
                [ExcludeFromWindowsMetadata]
                public Component(List<int> values) { }
                public Component() { }
                [ExcludeFromWindowsMetadata]
                public List<int> ManagedValues { private get; set; }
                List<int> IManaged.Work() => null;
            }
            """, isCsWinRTComponent: true, assemblyName: "Component");
    }

    [TestMethod]
    [DataRow("Hidden")]
    [DataRow("Hidden[]")]
    [DataRow("System.Collections.Generic.IReadOnlyList<Hidden>")]
    [DataRow("System.ReadOnlySpan<Hidden>")]
    public async Task ExportedSignatureReferencingIgnoredType_ReportsDiagnostic(string type)
    {
        await VerifyAsync($$"""
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            public interface IContract
            {
                {{type}} {|CSWINRT2050:Method|}({{type}} {|CSWINRT2050:value|});
            }
            """);
    }

    [TestMethod]
    public async Task IgnoredTypeInStructFieldAndInterfaceContracts_ReportsDiagnostic()
    {
        await VerifyAsync("""
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public struct HiddenValue { public int Value; }
            [ExcludeFromWindowsMetadata]
            public interface IHidden;
            public interface {|CSWINRT2050:IContract|} : IHidden;
            public struct Value { public HiddenValue {|CSWINRT2050:Field|}; }
            """);
    }

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task IgnoredManagedBase_DerivedTypeCanBeExported(bool abstractBase, bool genericBase)
    {
        string modifier = abstractBase ? "abstract " : "";
        string declaration = genericBase ? "Hidden<T>" : "Hidden";
        string baseType = genericBase ? "Hidden<int>" : "Hidden";

        await VerifyAsync($$"""
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public {{modifier}}class {{declaration}};
            public sealed class Visible : {{baseType}};
            """);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task IgnoredBase_InExportedSignatureStillReportsDiagnostic(bool abstractBase)
    {
        string modifier = abstractBase ? "abstract " : "";

        await VerifyAsync($$"""
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public {{modifier}}class Hidden;
            public sealed class Visible : Hidden
            {
                public Hidden {|CSWINRT2050:GetBase|}() => this;
            }
            """);
    }

    [TestMethod]
    public async Task ExportedAttributeArguments_CannotReferenceIgnoredTypes()
    {
        await VerifyAsync("""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            [System.Runtime.Serialization.KnownType(typeof(Hidden))]
            public sealed class {|CSWINRT2050:Visible|};
            """);
    }

    [TestMethod]
    public async Task NonExportedManagedMembers_CanReferenceIgnoredTypes()
    {
        await VerifyAsync("""
            namespace Component;
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            public struct Value
            {
                public int Field;
                public Hidden ManagedHelper(Hidden value) => value;
            }
            public sealed class Visible
            {
                public Hidden ManagedField;
            }
            """);
    }

    [TestMethod]
    public async Task RequiredInterfaceMembers_CannotBeIgnored()
    {
        await VerifyAsync("""
            using System;
            using WindowsRuntime.InteropServices;
            namespace Component;
            public interface IContract
            {
                void Method();
                int Value { get; }
                event EventHandler Changed;
            }
            public sealed class Component : IContract, IDisposable
            {
                [ExcludeFromWindowsMetadata]
                public void {|CSWINRT2049:Method|}() { }
                [ExcludeFromWindowsMetadata]
                public int {|CSWINRT2049:Value|} => 0;
                [ExcludeFromWindowsMetadata]
                public event EventHandler {|CSWINRT2049:Changed|} { add { } remove { } }
                [ExcludeFromWindowsMetadata]
                public void {|CSWINRT2049:Dispose|}() { }
            }
            """);
    }

    [TestMethod]
    public async Task AccessorsAndInstanceStructFields_CannotBeIgnored()
    {
        await VerifyAsync("""
            using WindowsRuntime.InteropServices;
            namespace Component;
            public sealed class Component
            {
                public int Value { [ExcludeFromWindowsMetadata] {|CSWINRT2049:get|} => 0; }
            }
            public struct Value
            {
                [ExcludeFromWindowsMetadata]
                public int {|CSWINRT2049:Field|};
            }
            """);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AliasedExclusionOnEventAccessor_ReportsDiagnostic(bool generated)
    {
        const string source = """
            using Exclude = WindowsRuntime.InteropServices.ExcludeFromWindowsMetadataAttribute;
            namespace Component;
            public sealed class Component
            {
                public event System.EventHandler Changed
                {
                    [Exclude]
                    {|CSWINRT2049:add|} { }
                    remove { }
                }
            }
            """;

        await CSharpAnalyzerTest<ExcludeFromWindowsMetadataAttributeAnalyzer>.VerifyAnalyzerAsync(
            generated ? "" : source,
            isCsWinRTComponent: true,
            generatedSource: generated ? source : null,
            assemblyName: "Component");
    }

    [TestMethod]
    public async Task ExcludedStructBackingField_ReportsDiagnostic()
    {
        await VerifyAsync("""
            using WindowsRuntime.InteropServices;
            namespace Component;
            public struct Value
            {
                [field: ExcludeFromWindowsMetadata]
                public int {|CSWINRT2049:Property|} { get; set; }
            }
            """);
    }

    [TestMethod]
    [DataRow("IList<int>", "ICollection<int>", false, "Count")]
    [DataRow("IList<int>", "ICollection<int>", true, "Count")]
    [DataRow("IReadOnlyList<int>", "IReadOnlyCollection<int>", false, "Count")]
    [DataRow("IReadOnlyList<int>", "IReadOnlyCollection<int>", true, "Count")]
    [DataRow("IList<int>", "ICollection<int>", false, "Add")]
    [DataRow("IList<int>", "ICollection<int>", true, "Add")]
    public async Task MappedCollectionAncestorMembers_CannotBeIgnored(
        string contract, string ancestor, bool explicitImplementation, string member)
    {
        string countName = member == "Count" ? "{|CSWINRT2049:Count|}" : "Count";
        string addName = member == "Add" ? "{|CSWINRT2049:Add|}" : "Add";
        string count = explicitImplementation && member == "Count" ? $"int {ancestor}.{countName}" : $"public int {countName}";
        string add = explicitImplementation && member == "Add" ? $"void {ancestor}.{addName}" : $"public void {addName}";
        string countAttribute = member == "Count" ? "[WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]" : "";
        string addAttribute = member == "Add" ? "[WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]" : "";

        await VerifyAsync($$"""
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
            """);
    }

    [TestMethod]
    public async Task IgnoredInterfacesAndTheirMembers_DoNotRequireImplementationsInMetadata()
    {
        await VerifyAsync("""
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public interface IManaged { void Method(); }
            public interface IContract { [ExcludeFromWindowsMetadata] void Hidden(); }
            public sealed class Component : IManaged, IContract
            {
                [ExcludeFromWindowsMetadata]
                public void Method() { }
                [ExcludeFromWindowsMetadata]
                public void Hidden() { }
            }
            """);
    }

    [TestMethod]
    public async Task OmittedCollectionMember_WithoutAMappedChildCanBeIgnored()
    {
        await VerifyAsync("""
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
    }

    [TestMethod]
    public async Task OrdinaryLibrary_DoesNotReportComponentExclusions()
    {
        await CSharpAnalyzerTest<ExcludeFromWindowsMetadataAttributeAnalyzer>.VerifyAnalyzerAsync("""
            using WindowsRuntime.InteropServices;
            [ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            public interface IContract { Hidden Method(); }
            public sealed class Visible
            {
                public int Value { [ExcludeFromWindowsMetadata] get => 0; }
            }
            public struct Value
            {
                [ExcludeFromWindowsMetadata]
                public int Field;
            }
            """);
    }

    [TestMethod]
    public async Task GeneratedSurface_UsesTheSameExclusionRules()
    {
        await CSharpAnalyzerTest<ExcludeFromWindowsMetadataAttributeAnalyzer>.VerifyAnalyzerAsync("",
            isCsWinRTComponent: true, assemblyName: "Component", generatedSource: """
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public sealed class Hidden;
            public interface IContract { Hidden {|CSWINRT2050:Method|}(); }
            """);
    }

    [TestMethod]
    public async Task IgnoredTypes_DoNotRequireVersioningOrDeprecatedMetadata()
    {
        const string source = """
            using WindowsRuntime.InteropServices;
            [ExcludeFromWindowsMetadata]
            [System.Obsolete]
            public sealed class Managed
            {
                [System.Obsolete]
                public void Helper() { }
            }
            """;

        await CSharpAnalyzerTest<PublicTypeRequiresVersioningAnalyzer>.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
        await CSharpAnalyzerTest<ObsoleteWithoutDeprecatedAnalyzer>.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
        await CSharpAnalyzerTest<ValidContractVersionAttributeAnalyzer>.VerifyAnalyzerAsync("""
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            [Windows.Foundation.Metadata.ContractVersion(1)]
            public sealed class Managed;
            """, isCsWinRTComponent: true);
        await CSharpAnalyzerTest<ApiContractTypeRequiresContractVersionAnalyzer>.VerifyAnalyzerAsync("""
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            [Windows.Foundation.Metadata.ApiContract]
            public enum ManagedContract { }
            """, isCsWinRTComponent: true);
        await CSharpAnalyzerTest<ValidApiContractEnumTypeAnalyzer>.VerifyAnalyzerAsync("""
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            [Windows.Foundation.Metadata.ApiContract]
            public enum ManagedContract : long { Value = 1 }
            """, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task IgnoredConstructor_DoesNotRequireExperimentalMetadata()
    {
        await CSharpAnalyzerTest<ExperimentalAttributeTargetAnalyzer>.VerifyAnalyzerAsync("""
            public sealed class Component
            {
                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                [System.Diagnostics.CodeAnalysis.Experimental("TEST0001")]
                public Component() { }
            }
            """, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task IgnoredTypes_DoNotParticipateInContractVersioning()
    {
        const string source = """
            using Windows.Foundation.Metadata;
            [ApiContract, ContractVersion(1)]
            public enum Contract { }
            [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
            [ContractVersion(typeof(Contract), 1), Version(1)]
            public sealed class Managed;
            [Version(1)]
            public sealed class Visible;
            """;

        await CSharpAnalyzerTest<PublicTypeRequiresContractVersionAnalyzer>.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
        await CSharpAnalyzerTest<PublicTypeMixedVersioningAttributesAnalyzer>.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task IgnoredBase_GenericInterfaceArgumentsAreValidatedOnlyWhenRedeclared(bool redeclare)
    {
        string interfaces = redeclare ? ", IEnumerable<HiddenValue>" : "";
        string name = redeclare ? "{|CSWINRT2050:Visible|}" : "Visible";

        await VerifyAsync($$"""
            using System.Collections;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public sealed class HiddenValue;
            [ExcludeFromWindowsMetadata]
            public class Hidden<T> : IEnumerable<T>
            {
                public IEnumerator<T> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public sealed class {{name}} : Hidden<HiddenValue>{{interfaces}};
            """);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task IgnoredBase_RequiredMappedAncestorMembersDependOnRedeclaration(bool redeclare)
    {
        string interfaces = redeclare ? ", IReadOnlyList<int>" : "";
        string name = redeclare ? "{|CSWINRT2049:Count|}" : "Count";

        await VerifyAsync($$"""
            using System.Collections;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public class Hidden : IReadOnlyList<int>
            {
                public virtual int Count => 0;
                public int this[int index] => 0;
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public sealed class Visible : Hidden{{interfaces}}
            {
                [ExcludeFromWindowsMetadata]
                public override int {{name}} => 0;
            }
            """);
    }

    [TestMethod]
    public async Task IgnoredBase_ReexposedMappedMemberCannotReferenceAnIgnoredType()
    {
        await VerifyAsync("""
            using System.Collections;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public sealed class HiddenValue;
            [ExcludeFromWindowsMetadata]
            public class Hidden : IEnumerable<HiddenValue>
            {
                public virtual IEnumerator<HiddenValue> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public sealed class Visible : Hidden
            {
                public override IEnumerator<HiddenValue> {|CSWINRT2050:GetEnumerator|}() => null;
            }
            """);
    }

    [TestMethod]
    public async Task IgnoredBase_ReexposedMappedMemberStillValidatesItsSignature()
    {
        await CSharpAnalyzerTest<ComponentAuthoringAnalyzer>.VerifyAnalyzerAsync("""
            using System.Collections;
            using System.Collections.Generic;
            using WindowsRuntime.InteropServices;
            namespace Component;
            [ExcludeFromWindowsMetadata]
            public class Hidden : IEnumerable<List<int>>
            {
                public virtual IEnumerator<List<int>> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
            public sealed class Visible : Hidden
            {
                public override IEnumerator<List<int>> {|CSWINRT2033:GetEnumerator|}() => null;
            }
            """, isCsWinRTComponent: true, assemblyName: "Component");
    }

    private static Task VerifyAsync(string source)
    {
        return CSharpAnalyzerTest<ExcludeFromWindowsMetadataAttributeAnalyzer>.VerifyAnalyzerAsync(
            source, isCsWinRTComponent: true, assemblyName: "Component");
    }
}