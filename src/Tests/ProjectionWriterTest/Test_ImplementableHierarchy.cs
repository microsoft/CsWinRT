// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectionWriterTest.Helpers;
using WindowsRuntime.ProjectionWriter;

namespace ProjectionWriterTest;

/// <summary>
/// Covers the implementable bases of runtime classes that derive from other runtime classes, or whose shape could
/// collide with generated code (see <see cref="ImplementableHierarchyMetadata"/>).
/// </summary>
[TestClass]
public class Test_ImplementableHierarchy
{
    /// <summary>
    /// The base for <c>IClosable</c> already declares the <c>Dispose()</c> that <c>IIterator&lt;int&gt;</c> (as
    /// <c>IEnumerator&lt;int&gt;</c>) also needs, so redeclaring it would hide the inherited abstract member.
    /// </summary>
    [TestMethod]
    public void InheritedMappedMember_IsNotRedeclared()
    {
        ClassDeclarationSyntax derived = GetAbiClass(ImplementableHierarchyMetadata.IteratorDerived);

        Assert.IsFalse(GetMethodNames(derived).Contains("Dispose"));
        Assert.IsTrue(GetMethodNames(derived).Contains("MoveNext"));
        Assert.IsTrue(GetMethodNames(GetAbiClass(ImplementableHierarchyMetadata.ClosableBase)).Contains("Dispose"));
    }

    /// <summary>
    /// A derived base declares its own conversion to each projected ancestor, so converting to one is not ambiguous
    /// with the conversion it inherits from the ancestor's base.
    /// </summary>
    [TestMethod]
    public void DerivedBase_ConvertsToProjectedAncestors()
    {
        string text = GetAbiClass(ImplementableHierarchyMetadata.IteratorDerived).ToString();

        StringAssert.Contains(text, $"public static implicit operator global::Contoso.{ImplementableHierarchyMetadata.ClosableBase}({ImplementableHierarchyMetadata.IteratorDerived} value)");
        StringAssert.Contains(text, $"return (global::Contoso.{ImplementableHierarchyMetadata.IteratorDerived})value;");
    }

    /// <summary>
    /// The same instantiation of a generic interface reached again through a derived class is only declared once.
    /// </summary>
    [TestMethod]
    public void SameGenericInstantiation_IsDeclaredOnce()
    {
        ClassDeclarationSyntax derived = GetAbiClass(ImplementableHierarchyMetadata.SameIterableDerived);

        Assert.IsFalse(GetMethodNames(derived).Contains("GetEnumerator"));
        Assert.IsFalse(derived.BaseList!.Types.Any(static type => type.ToString().Contains("IEnumerable", System.StringComparison.Ordinal)));
    }

    /// <summary>
    /// A different instantiation of a generic interface the base class already implements is not dropped. Its members
    /// differ only in their types from the inherited abstract ones, which C# cannot declare alongside them, so the
    /// class gets no base rather than one that silently leaves the interface out.
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DistinctGenericInstantiation_IsNotDropped(bool referenceProjection)
    {
        string[] names = GetAbiClassNames(referenceProjection);

        Assert.IsFalse(names.Contains(ImplementableHierarchyMetadata.OtherIterableDerived));
        Assert.IsTrue(names.Contains(ImplementableHierarchyMetadata.IntIterableBase));
    }

    /// <summary>
    /// The local holding the constructed instance must not collide with a constructor parameter of the same name.
    /// </summary>
    [TestMethod]
    public void ComposableConstructorParameter_DoesNotCollideWithLocal()
    {
        string text = GetAbiClass($"{ImplementableHierarchyMetadata.InstanceParameter}ActivationFactory").ToString();

        StringAssert.Contains(text, "CreateInstance(int instance, object baseInterface, out object innerInterface)");
        StringAssert.Contains(text, $"{ImplementableHierarchyMetadata.InstanceParameter} __instance = CreateInstance(instance);");
        StringAssert.Contains(text, "return __instance;");
    }

    /// <summary>
    /// A removed class is not projected, so it gets no bases either (they would convert to a class that does not exist).
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RemovedClass_GetsNoBases(bool referenceProjection)
    {
        string[] names = GetAbiClassNames(referenceProjection);

        Assert.IsFalse(names.Any(static name => name.StartsWith(ImplementableHierarchyMetadata.Removed, System.StringComparison.Ordinal)));
    }

    /// <summary>
    /// An ordinary interface method with the signature of a member the base declares for a custom-mapped interface is
    /// satisfied by that member, rather than redeclared.
    /// </summary>
    [TestMethod]
    public void OrdinaryMemberMatchingInheritedMappedMember_IsNotRedeclared()
    {
        Assert.IsFalse(GetMethodNames(GetAbiClass(ImplementableHierarchyMetadata.DisposeDerived)).Contains("Dispose"));
    }

    /// <summary>
    /// A protected member cannot implement an interface member implicitly, so every interface requiring it gets an explicit
    /// implementation forwarding to it, including interfaces sharing it and public interfaces of derived classes.
    /// </summary>
    [TestMethod]
    public void ProtectedMembers_AreForwardedForEveryInterface()
    {
        string baseText = GetAbiClass(ImplementableHierarchyMetadata.ProtectedBase).ToString();

        StringAssert.Contains(baseText, "protected abstract void Hook();");
        StringAssert.Contains(baseText, "void global::Contoso.IProtectedHooks.Hook() => Hook();");
        StringAssert.Contains(baseText, "void global::Contoso.IProtectedHooks2.Hook() => Hook();");
        StringAssert.Contains(baseText, "protected abstract int Level { get; set; }");
        StringAssert.Contains(baseText, "int global::Contoso.IProtectedHooks2.Level");
        StringAssert.Contains(baseText, "event global::System.EventHandler<object> global::Contoso.IProtectedHooks2.Changed");

        string derivedText = GetAbiClass(ImplementableHierarchyMetadata.PublicHookDerived).ToString();

        Assert.IsFalse(GetMethodNames(GetAbiClass(ImplementableHierarchyMetadata.PublicHookDerived)).Contains("Hook"));
        StringAssert.Contains(derivedText, "void global::Contoso.IPublicHooks.Hook() => Hook();");
        StringAssert.Contains(derivedText, "int global::Contoso.IPublicHooks.Level");
        StringAssert.Contains(derivedText, "event global::System.EventHandler<object> global::Contoso.IPublicHooks.Changed");
    }

    /// <summary>
    /// A removed member of a protected interface is not part of the projected interface, so it gets no hook or explicit implementation.
    /// </summary>
    [TestMethod]
    public void RemovedProtectedMember_IsNotImplemented()
    {
        Assert.IsFalse(GetAbiClass(ImplementableHierarchyMetadata.ProtectedBase).ToString().Contains("Gone", System.StringComparison.Ordinal));
    }

    /// <summary>
    /// An inherited property cannot gain the setter a new interface requires, which C# cannot express, so the class gets no base.
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InheritedPropertyMissingAccessor_GetsNoBase(bool referenceProjection)
    {
        string[] names = GetAbiClassNames(referenceProjection);

        Assert.IsFalse(names.Contains(ImplementableHierarchyMetadata.SettableCountDerived));
        Assert.IsTrue(names.Contains(ImplementableHierarchyMetadata.VectorBase));
    }

    /// <summary>
    /// A removed factory interface is not projected, so the factory base neither implements it nor declares its members.
    /// </summary>
    [TestMethod]
    public void RemovedFactoryInterface_IsNotImplemented()
    {
        string text = GetAbiClass($"{ImplementableHierarchyMetadata.RemovedFactory}ActivationFactory").ToString();

        Assert.IsFalse(text.Contains($"I{ImplementableHierarchyMetadata.RemovedFactory}Factory", System.StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("CreateWithValue", System.StringComparison.Ordinal));
        StringAssert.Contains(text, $"public abstract {ImplementableHierarchyMetadata.RemovedFactory} ActivateInstance();");
    }

    /// <summary>
    /// A constructor and a static method with the same name and parameters need hooks returning different types, which C# cannot
    /// declare side by side, so the class gets no bases.
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ConstructorAndStaticWithSameSignature_GetNoBases(bool referenceProjection)
    {
        Assert.IsFalse(GetAbiClassNames(referenceProjection).Any(static name => name.StartsWith(ImplementableHierarchyMetadata.CreateCollision, System.StringComparison.Ordinal)));
    }

    /// <summary>
    /// A class that gets no bases is reported once, with the reason, so it is not dropped silently.
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SkippedClasses_AreLoggedOnce(bool referenceProjection)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;
        System.Collections.Concurrent.ConcurrentQueue<string> messages = new();

        try
        {
            _ = Generate(directory, referenceProjection, messages.Enqueue);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }

        string[] skipped = [.. messages.Where(static message => message.StartsWith("No implementable base generated for ", System.StringComparison.Ordinal))];

        Assert.HasCount(3, skipped, string.Join(System.Environment.NewLine, messages));
        Assert.ContainsSingle(skipped.Where(static message => message.Contains($"'Contoso.{ImplementableHierarchyMetadata.OtherIterableDerived}': it implements multiple instantiations", System.StringComparison.Ordinal)));
        Assert.ContainsSingle(skipped.Where(static message => message.Contains($"'Contoso.{ImplementableHierarchyMetadata.CreateCollision}': some of its members", System.StringComparison.Ordinal)));
        Assert.ContainsSingle(skipped.Where(static message => message.Contains($"'Contoso.{ImplementableHierarchyMetadata.SettableCountDerived}': some of its members", System.StringComparison.Ordinal)));
    }

    /// <summary>
    /// The non-generic mapped interfaces are declared in their .NET form, like the generic ones, with the members
    /// that have no Windows Runtime counterpart given the same fixed values as on the projected class.
    /// </summary>
    [TestMethod]
    public void NonGenericMappedInterfaces_AreDeclaredInTheirNetForm()
    {
        string list = GetAbiClass(ImplementableHierarchyMetadata.BindableList).ToString();

        StringAssert.Contains(list, "global::System.Collections.IList");
        StringAssert.Contains(list, "public abstract object this[int index] { get; set; }");
        StringAssert.Contains(list, "public abstract int Add(object value);");
        StringAssert.Contains(list, "public abstract global::System.Collections.IEnumerator GetEnumerator();");
        StringAssert.Contains(list, "public virtual object SyncRoot => this;");

        StringAssert.Contains(GetAbiClass(ImplementableHierarchyMetadata.BindableIterator).ToString(), "public abstract object Current { get; }");

        string errors = GetAbiClass(ImplementableHierarchyMetadata.DataErrors).ToString();

        StringAssert.Contains(errors, "global::System.ComponentModel.INotifyDataErrorInfo");
        StringAssert.Contains(errors, "public abstract event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> ErrorsChanged;");
    }

    /// <summary>
    /// Members are compared by how their types are spelled, so the result must not depend on whether the namespace's first base
    /// is checked before the <c>ABI</c> namespace is opened (where a type from the same namespace is not fully qualified).
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void InheritedMemberOfSameNamespaceType_SatisfiesMappedMember(bool referenceProjection)
    {
        Assert.Contains(ImplementableHierarchyMetadata.CurrentDerived, GetAbiClassNames(referenceProjection));
    }

    [TestMethod]
    public void ReferenceProjection_Compiles()
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection: true);

            _ = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "Projection.dll"),
                referenceProjection: true);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// What an author writes against the generated bases, including converting to a projected ancestor.
    /// </summary>
    [TestMethod]
    public void Implementations_CompileAgainstTheGeneratedBases()
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            string output = Generate(directory, referenceProjection: false);
            string projection = ProjectionWriterRunner.CompileSources(
                Directory.GetFiles(output, "*.cs").Select(File.ReadAllText),
                Path.Combine(directory, "Projection.dll"));

            const string consumer = """
                namespace MyApp
                {
                    public sealed class MyIterator : ABI.Contoso.IteratorDerived
                    {
                        public override void ClosableBaseMethod() { }

                        public override void Dispose() { }

                        public override void IteratorDerivedMethod() { }

                        public override int Current => 0;

                        public override bool MoveNext() => false;

                        public override void Reset() { }
                    }

                    public sealed class MyIterable : ABI.Contoso.SameIterableDerived
                    {
                        public override void IntIterableBaseMethod() { }

                        public override System.Collections.Generic.IEnumerator<int> GetEnumerator() => null!;

                        public override void SameIterableDerivedMethod() { }
                    }

                    public sealed class MyPublicHook : ABI.Contoso.PublicHookDerived
                    {
                        protected override void Hook() { }

                        protected override int Level { get; set; }

                        protected override event System.EventHandler<object> Changed { add { } remove { } }

                        public override void ProtectedBaseMethod() { }

                        public override void PublicHookDerivedMethod() { }
                    }

                    public sealed class MyDisposer : ABI.Contoso.DisposeDerived
                    {
                        public override void ClosableBaseMethod() { }

                        public override void Dispose() { }

                        public override void DisposeDerivedMethod() { }
                    }

                    public sealed class MyBindableList : ABI.Contoso.BindableListWidget
                    {
                        public override void BindableListWidgetMethod() { }

                        public override object this[int index] { get => null!; set { } }

                        public override int Count => 0;

                        public override bool IsReadOnly => false;

                        public override int Add(object value) => 0;

                        public override void Clear() { }

                        public override bool Contains(object value) => false;

                        public override int IndexOf(object value) => -1;

                        public override void Insert(int index, object value) { }

                        public override void Remove(object value) { }

                        public override void RemoveAt(int index) { }

                        public override void CopyTo(System.Array array, int index) { }

                        public override System.Collections.IEnumerator GetEnumerator() => null!;
                    }

                    public sealed class MyBindableIterator : ABI.Contoso.BindableIteratorWidget
                    {
                        public override void BindableIteratorWidgetMethod() { }

                        public override object Current => null!;

                        public override bool MoveNext() => false;

                        public override void Reset() { }
                    }

                    public sealed class MyDataErrors : ABI.Contoso.DataErrorsWidget
                    {
                        public override void DataErrorsWidgetMethod() { }

                        public override bool HasErrors => false;

                        public override System.Collections.IEnumerable GetErrors(string propertyName) => null!;

                        public override event System.EventHandler<System.ComponentModel.DataErrorsChangedEventArgs> ErrorsChanged { add { } remove { } }
                    }

                    public sealed class MyCurrentDerived : ABI.Contoso.AaCurrentDerived
                    {
                        public override void ZzCurrentBaseMethod() { }

                        public override Contoso.AaPoint Current => default;

                        public override void AaCurrentDerivedMethod() { }

                        public override bool MoveNext() => false;

                        public override void Reset() { }

                        public override void Dispose() { }
                    }

                    public static class Usage
                    {
                        public static void Use(MyIterator iterator, MyIterable iterable)
                        {
                            Contoso.IteratorDerived derived = iterator;
                            Contoso.ClosableBase closable = iterator;
                            Contoso.IntIterableBase intIterable = iterable;

                            TakeClosable(iterator);
                            TakeIntIterable(iterable);

                            // Back from a projected ancestor, to the implementation and to each base
                            MyIterator fromAncestor = (MyIterator)closable;
                            ABI.Contoso.IteratorDerived derivedBase = (ABI.Contoso.IteratorDerived)closable;
                            ABI.Contoso.ClosableBase closableBase = (ABI.Contoso.ClosableBase)closable;
                        }

                        private static void TakeClosable(Contoso.ClosableBase value) { }

                        private static void TakeIntIterable(Contoso.IntIterableBase value) { }

                        public static void UseMapped(MyBindableList list, MyBindableIterator iterator, MyDataErrors errors)
                        {
                            System.Collections.IList nonGenericList = list;
                            System.Collections.IEnumerator nonGenericIterator = iterator;
                            System.ComponentModel.INotifyDataErrorInfo dataErrorInfo = errors;
                            Contoso.BindableListWidget projected = list;
                        }
                    }
                }
                """;

            _ = ProjectionWriterRunner.CompileSources(
                [consumer],
                Path.Combine(directory, "Consumer.dll"),
                additionalReferences: [projection]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string[] GetMethodNames(ClassDeclarationSyntax type)
    {
        return [.. type.Members.OfType<MethodDeclarationSyntax>().Where(static method => method.ExplicitInterfaceSpecifier is null).Select(static method => method.Identifier.ValueText)];
    }

    private static ClassDeclarationSyntax GetAbiClass(string name)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            ClassDeclarationSyntax[] classes = [.. GetAbiClasses(Generate(directory, referenceProjection: false)).Where(type => type.Identifier.ValueText == name)];

            Assert.HasCount(1, classes, $"Expected exactly one 'ABI.Contoso.{name}'.");

            return classes[0];
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string[] GetAbiClassNames(bool referenceProjection)
    {
        string directory = Directory.CreateTempSubdirectory("ProjectionHierarchyTest_").FullName;

        try
        {
            return [.. GetAbiClasses(Generate(directory, referenceProjection)).Select(static type => type.Identifier.ValueText)];
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// Gets the abstract classes in the <c>ABI.Contoso</c> namespace, i.e. the implementable bases.
    /// </summary>
    private static ClassDeclarationSyntax[] GetAbiClasses(string output)
    {
        return [.. Directory.GetFiles(output, "*.cs")
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot())
            .SelectMany(static root => root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Where(static type =>
                type.Parent is BaseNamespaceDeclarationSyntax { Name: var ns } &&
                ns.ToString() == "ABI.Contoso" &&
                type.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                !type.Modifiers.Any(SyntaxKind.StaticKeyword))];
    }

    private static string Generate(string directory, bool referenceProjection, System.Action<string>? logger = null)
    {
        string output = Path.Combine(directory, referenceProjection ? "reference" : "generated");

        ProjectionWriter.Run(new ProjectionWriterOptions
        {
            InputPaths = [ImplementableHierarchyMetadata.Create(directory), ImplementableHierarchyMetadata.FindWindowsFoundationWinmd()],
            OutputFolder = output,
            Include = ["Contoso"],
            ImplementWinMDTypes = true,
            ReferenceProjection = referenceProjection,
            Logger = logger ?? (static _ => { })
        });

        return output;
    }
}
