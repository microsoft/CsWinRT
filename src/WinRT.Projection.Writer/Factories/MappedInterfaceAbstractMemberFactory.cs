// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits <c>abstract</c> member declarations for the .NET interfaces that Windows Runtime interfaces are
/// custom-mapped to (<c>IClosable</c> to <see cref="System.IDisposable"/>, <c>IMap`2</c> to
/// <c>IDictionary&lt;K, V&gt;</c>, and so on).
/// </summary>
/// <remarks>
/// <para>
/// The counterpart of <see cref="MappedInterfaceStubFactory"/>: that emits <em>delegating</em> members for a
/// projected class wrapping a native object, whereas an implementable base has nothing to delegate to and
/// declares them <c>abstract</c> instead.
/// </para>
/// <para>
/// Members are declared in their .NET shape, since that is what the mapped interface requires. The interop
/// generator maps a type implementing e.g. <c>IDictionary&lt;K, V&gt;</c> back to an <c>IMap&lt;K, V&gt;</c>
/// interface entry, so the resulting vtable is still the Windows Runtime one.
/// </para>
/// </remarks>
internal static class MappedInterfaceAbstractMemberFactory
{
    /// <summary>
    /// Returns whether abstract members can be emitted for the given custom-mapped Windows Runtime interface.
    /// </summary>
    /// <remarks>
    /// A mapped interface with no known .NET shape here cannot be declared on an abstract base at all (the
    /// base would not compile), so callers use this to decide whether the enclosing type is supported.
    /// </remarks>
    public static bool IsSupported(string interfaceName)
    {
        return interfaceName is
            "IClosable" or
            "IIterable`1" or
            "IIterator`1" or
            "IMap`2" or
            "IMapView`2" or
            "IVector`1" or
            "IVectorView`1" or
            "IBindableIterable" or
            "IBindableIterator" or
            "IBindableVector" or
            "INotifyDataErrorInfo";
    }

    /// <summary>
    /// Emits the <c>abstract</c> members for the .NET interface that <paramref name="interfaceName"/> maps to.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="instance">The (possibly substituted) generic instance signature, or <see langword="null"/> if non-generic.</param>
    /// <param name="interfaceName">The Windows Runtime interface name (e.g. <c>"IMap`2"</c>).</param>
    /// <param name="members">The members declared so far, so overlapping interfaces (e.g. <c>IMap`2</c> and <c>IIterable`1</c>, which both surface <c>GetEnumerator</c>) each contribute their members only once.</param>
    public static void WriteAbstractMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        GenericInstanceTypeSignature? instance,
        string interfaceName,
        ImplementableMemberSet members)
    {
        List<string> typeArgumentNames = [];

        if (instance is not null)
        {
            foreach (TypeSignature typeArgument in instance.TypeArguments)
            {
                typeArgumentNames.Add(TypedefNameWriter.WriteTypeName(context, TypeSemanticsFactory.Get(typeArgument), TypedefNameType.Projected, true).Format());
            }
        }

        switch (interfaceName)
        {
            case "IClosable":
                WriteMethod(writer, members, "void", "Dispose", "");
                break;
            case "IIterable`1" when typeArgumentNames.Count == 1:
                WriteEnumerable(writer, members, typeArgumentNames[0]);
                break;
            case "IIterator`1" when typeArgumentNames.Count == 1:
                WriteEnumerator(writer, members, typeArgumentNames[0]);
                break;
            case "IMap`2" when typeArgumentNames.Count == 2:
                WriteDictionary(writer, members, typeArgumentNames[0], typeArgumentNames[1]);
                break;
            case "IMapView`2" when typeArgumentNames.Count == 2:
                WriteReadOnlyDictionary(writer, members, typeArgumentNames[0], typeArgumentNames[1]);
                break;
            case "IVector`1" when typeArgumentNames.Count == 1:
                WriteList(writer, members, typeArgumentNames[0]);
                break;
            case "IVectorView`1" when typeArgumentNames.Count == 1:
                WriteReadOnlyList(writer, members, typeArgumentNames[0]);
                break;
            case "IBindableIterable":
                WriteMethod(writer, members, "global::System.Collections.IEnumerator", "GetEnumerator", "");
                break;
            case "IBindableIterator":
                WriteNonGenericEnumerator(writer, members);
                break;
            case "IBindableVector":
                WriteNonGenericList(writer, members);
                break;
            case "INotifyDataErrorInfo":
                WriteNotifyDataErrorInfo(writer, members);
                break;
        }
    }

    /// <summary>Emits the non-generic <c>IEnumerator</c> members.</summary>
    private static void WriteNonGenericEnumerator(IndentedTextWriter writer, ImplementableMemberSet members)
    {
        WriteProperty(writer, members, "object", "Current", hasSetter: false);
        WriteMethod(writer, members, "bool", "MoveNext", "");
        WriteMethod(writer, members, "void", "Reset", "");
    }

    /// <summary>Emits the non-generic <c>IList</c> members (<c>GetEnumerator</c> comes from <c>IBindableIterable</c>).</summary>
    private static void WriteNonGenericList(IndentedTextWriter writer, ImplementableMemberSet members)
    {
        WriteIndexer(writer, members, "object", "int", "index", hasSetter: true, indexerName: "NonGenericListItem");
        WriteProperty(writer, members, "int", "Count", hasSetter: false);
        WriteProperty(writer, members, "bool", "IsReadOnly", hasSetter: false);
        WriteMethod(writer, members, "int", "Add", "object value");
        WriteMethod(writer, members, "void", "Clear", "");
        WriteMethod(writer, members, "bool", "Contains", "object value");
        WriteMethod(writer, members, "int", "IndexOf", "object value");
        WriteMethod(writer, members, "void", "Insert", "int index, object value");
        WriteMethod(writer, members, "void", "Remove", "object value");
        WriteMethod(writer, members, "void", "RemoveAt", "int index");
        WriteMethod(writer, members, "void", "CopyTo", "global::System.Array array, int index");

        // These have no Windows Runtime counterpart, so a native caller can never reach them. They get the same
        // fixed values as on the projected class rather than being left for the author to implement.
        WriteFixedProperty(writer, members, "bool", "IsFixedSize", "false");
        WriteFixedProperty(writer, members, "bool", "IsSynchronized", "false");
        WriteFixedProperty(writer, members, "object", "SyncRoot", "this");
    }

    /// <summary>Emits the <c>INotifyDataErrorInfo</c> members.</summary>
    private static void WriteNotifyDataErrorInfo(IndentedTextWriter writer, ImplementableMemberSet members)
    {
        WriteProperty(writer, members, "bool", "HasErrors", hasSetter: false);
        WriteMethod(writer, members, "global::System.Collections.IEnumerable", "GetErrors", "string propertyName");

        if (!ShouldDeclare(members, members.ResolveEvent("ErrorsChanged", "global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>", isProtected: false)))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("public abstract event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> ErrorsChanged;");
    }

    /// <summary>Emits the <c>IEnumerable&lt;T&gt;</c> members.</summary>
    private static void WriteEnumerable(IndentedTextWriter writer, ImplementableMemberSet members, string elementTypeName)
    {
        WriteMethod(writer, members, $"global::System.Collections.Generic.IEnumerator<{elementTypeName}>", "GetEnumerator", "");

        WriteNonGenericGetEnumerator(writer, members);
    }

    /// <summary>Emits the <c>IEnumerator&lt;T&gt;</c> members.</summary>
    private static void WriteEnumerator(IndentedTextWriter writer, ImplementableMemberSet members, string elementTypeName)
    {
        WriteProperty(writer, members, elementTypeName, "Current", hasSetter: false);
        WriteMethod(writer, members, "bool", "MoveNext", "");
        WriteMethod(writer, members, "void", "Reset", "");
        WriteMethod(writer, members, "void", "Dispose", "");

        // An explicit interface implementation cannot be abstract, so the non-generic member forwards
        // to the generic one that the author implements.
        if (members.TryAddExplicitImplementation("IEnumerator.Current"))
        {
            writer.WriteLine();
            writer.WriteLine("object global::System.Collections.IEnumerator.Current => Current;");
        }
    }

    /// <summary>Emits the <c>IDictionary&lt;K, V&gt;</c> members.</summary>
    private static void WriteDictionary(IndentedTextWriter writer, ImplementableMemberSet members, string keyTypeName, string valueTypeName)
    {
        string pair = $"global::System.Collections.Generic.KeyValuePair<{keyTypeName}, {valueTypeName}>";

        WriteIndexer(writer, members, valueTypeName, keyTypeName, "key", hasSetter: true);
        WriteProperty(writer, members, $"global::System.Collections.Generic.ICollection<{keyTypeName}>", "Keys", hasSetter: false);
        WriteProperty(writer, members, $"global::System.Collections.Generic.ICollection<{valueTypeName}>", "Values", hasSetter: false);
        WriteProperty(writer, members, "int", "Count", hasSetter: false);
        WriteProperty(writer, members, "bool", "IsReadOnly", hasSetter: false);
        WriteMethod(writer, members, "void", "Add", $"{keyTypeName} key, {valueTypeName} value");
        WriteMethod(writer, members, "void", "Add", $"{pair} item");
        WriteMethod(writer, members, "void", "Clear", "");
        WriteMethod(writer, members, "bool", "Contains", $"{pair} item");
        WriteMethod(writer, members, "bool", "ContainsKey", $"{keyTypeName} key");
        WriteMethod(writer, members, "void", "CopyTo", $"{pair}[] array, int arrayIndex");
        WriteMethod(writer, members, "bool", "Remove", $"{keyTypeName} key");
        WriteMethod(writer, members, "bool", "Remove", $"{pair} item");
        WriteMethod(writer, members, "bool", "TryGetValue", $"{keyTypeName} key, out {valueTypeName} value");

        WriteEnumerable(writer, members, pair);
    }

    /// <summary>Emits the <c>IReadOnlyDictionary&lt;K, V&gt;</c> members.</summary>
    private static void WriteReadOnlyDictionary(IndentedTextWriter writer, ImplementableMemberSet members, string keyTypeName, string valueTypeName)
    {
        string pair = $"global::System.Collections.Generic.KeyValuePair<{keyTypeName}, {valueTypeName}>";

        WriteIndexer(writer, members, valueTypeName, keyTypeName, "key", hasSetter: false);
        WriteProperty(writer, members, $"global::System.Collections.Generic.IEnumerable<{keyTypeName}>", "Keys", hasSetter: false);
        WriteProperty(writer, members, $"global::System.Collections.Generic.IEnumerable<{valueTypeName}>", "Values", hasSetter: false);
        WriteProperty(writer, members, "int", "Count", hasSetter: false);
        WriteMethod(writer, members, "bool", "ContainsKey", $"{keyTypeName} key");
        WriteMethod(writer, members, "bool", "TryGetValue", $"{keyTypeName} key, out {valueTypeName} value");

        WriteEnumerable(writer, members, pair);
    }

    /// <summary>Emits the <c>IList&lt;T&gt;</c> members.</summary>
    private static void WriteList(IndentedTextWriter writer, ImplementableMemberSet members, string elementTypeName)
    {
        WriteIndexer(writer, members, elementTypeName, "int", "index", hasSetter: true, indexerName: "ListItem");
        WriteProperty(writer, members, "int", "Count", hasSetter: false);
        WriteProperty(writer, members, "bool", "IsReadOnly", hasSetter: false);
        WriteMethod(writer, members, "void", "Add", $"{elementTypeName} item");
        WriteMethod(writer, members, "void", "Clear", "");
        WriteMethod(writer, members, "bool", "Contains", $"{elementTypeName} item");
        WriteMethod(writer, members, "void", "CopyTo", $"{elementTypeName}[] array, int arrayIndex");
        WriteMethod(writer, members, "int", "IndexOf", $"{elementTypeName} item");
        WriteMethod(writer, members, "void", "Insert", $"int index, {elementTypeName} item");
        WriteMethod(writer, members, "bool", "Remove", $"{elementTypeName} item");
        WriteMethod(writer, members, "void", "RemoveAt", "int index");

        WriteEnumerable(writer, members, elementTypeName);
    }

    /// <summary>Emits the <c>IReadOnlyList&lt;T&gt;</c> members.</summary>
    private static void WriteReadOnlyList(IndentedTextWriter writer, ImplementableMemberSet members, string elementTypeName)
    {
        WriteIndexer(writer, members, elementTypeName, "int", "index", hasSetter: false, indexerName: "ReadOnlyListItem");
        WriteProperty(writer, members, "int", "Count", hasSetter: false);

        WriteEnumerable(writer, members, elementTypeName);
    }

    /// <summary>
    /// Emits the non-generic <c>IEnumerable.GetEnumerator</c> forwarder, which cannot be <c>abstract</c>
    /// because it is an explicit interface implementation.
    /// </summary>
    private static void WriteNonGenericGetEnumerator(IndentedTextWriter writer, ImplementableMemberSet members)
    {
        if (!members.TryAddExplicitImplementation("IEnumerable.GetEnumerator"))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();");
    }

    /// <summary>Emits a single <c>public abstract</c> method, if not already declared.</summary>
    private static void WriteMethod(IndentedTextWriter writer, ImplementableMemberSet members, string returnType, string name, string parameters)
    {
        if (!ShouldDeclare(members, members.ResolveMethod(name, parameters, returnType, isProtected: false)))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"public abstract {returnType} {name}({parameters});");
    }

    /// <summary>Emits a single <c>public abstract</c> property, if not already declared.</summary>
    private static void WriteProperty(IndentedTextWriter writer, ImplementableMemberSet members, string type, string name, bool hasSetter)
    {
        if (!ShouldDeclare(members, members.ResolveProperty(name, type, hasGetter: true, hasSetter, isProtected: false)))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"public abstract {type} {name} {{ get;{(hasSetter ? " set;" : "")} }}");
    }

    /// <summary>Emits a single <c>public virtual</c> read-only property returning a fixed value, if not already declared.</summary>
    private static void WriteFixedProperty(IndentedTextWriter writer, ImplementableMemberSet members, string type, string name, string value)
    {
        if (!ShouldDeclare(members, members.ResolveProperty(name, type, hasGetter: true, hasSetter: false, isProtected: false)))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"public virtual {type} {name} => {value};");
    }

    /// <summary>Emits a single <c>public abstract</c> indexer, if not already declared.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="members">The members declared so far.</param>
    /// <param name="type">The indexer's element type.</param>
    /// <param name="indexType">The index parameter's type.</param>
    /// <param name="indexName">The index parameter's name.</param>
    /// <param name="hasSetter">Whether the indexer has a setter.</param>
    /// <param name="indexerName">The <see cref="System.Runtime.CompilerServices.IndexerNameAttribute"/> name to apply, or <see langword="null"/> to leave the default (<c>Item</c>).</param>
    private static void WriteIndexer(
        IndentedTextWriter writer,
        ImplementableMemberSet members,
        string type,
        string indexType,
        string indexName,
        bool hasSetter,
        string? indexerName = null)
    {
        if (!ShouldDeclare(members, members.ResolveProperty($"this[{indexType}]", type, hasGetter: true, hasSetter, isProtected: false)))
        {
            return;
        }

        // A C# indexer is named 'Item' in metadata unless renamed, so an indexer keeping the default name
        // would collide with the Windows Runtime member of that name (which is how an indexer appears in the
        // metadata this base is otherwise generated from).
        if (indexerName is null)
        {
            members.Reserve("Item", "uint index");
        }

        writer.WriteLine();

        if (indexerName is not null)
        {
            writer.WriteLine($"[global::System.Runtime.CompilerServices.IndexerName(\"{indexerName}\")]");
        }

        writer.WriteLine($"public abstract {type} this[{indexType} {indexName}] {{ get;{(hasSetter ? " set;" : "")} }}");
    }

    /// <summary>
    /// Decides whether a member has to be declared, given how it resolves against the members declared so far.
    /// </summary>
    /// <remarks>
    /// A .NET interface member can only be satisfied by a public member, as there is no Windows Runtime interface to forward
    /// to explicitly, so an existing protected member with the same signature is a conflict.
    /// </remarks>
    private static bool ShouldDeclare(ImplementableMemberSet members, ImplementableMemberResolution resolution)
    {
        if (resolution is ImplementableMemberResolution.Forward)
        {
            members.MarkConflict();
        }

        return resolution is ImplementableMemberResolution.Declare;
    }
}
