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
    public static bool IsSupported(string ifaceName)
    {
        return ifaceName is
            "IClosable" or
            "IIterable`1" or
            "IIterator`1" or
            "IMap`2" or
            "IMapView`2" or
            "IVector`1" or
            "IVectorView`1";
    }

    /// <summary>
    /// Emits the <c>abstract</c> members for the .NET interface that <paramref name="ifaceName"/> maps to.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="instance">The (possibly substituted) generic instance signature, or <see langword="null"/> if non-generic.</param>
    /// <param name="ifaceName">The Windows Runtime interface name (e.g. <c>"IMap`2"</c>).</param>
    /// <param name="members">The members declared so far, so overlapping interfaces (e.g. <c>IMap`2</c> and <c>IIterable`1</c>, which both surface <c>GetEnumerator</c>) each contribute their members only once.</param>
    public static void WriteAbstractMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        GenericInstanceTypeSignature? instance,
        string ifaceName,
        ImplementableMemberSet members)
    {
        List<string> args = [];

        if (instance is not null)
        {
            foreach (TypeSignature arg in instance.TypeArguments)
            {
                args.Add(TypedefNameWriter.WriteTypeName(context, TypeSemanticsFactory.Get(arg), TypedefNameType.Projected, true).Format());
            }
        }

        switch (ifaceName)
        {
            case "IClosable":
                WriteMethod(writer, members, "void", "Dispose", "");
                break;
            case "IIterable`1" when args.Count == 1:
                WriteEnumerable(writer, members, args[0]);
                break;
            case "IIterator`1" when args.Count == 1:
                WriteEnumerator(writer, members, args[0]);
                break;
            case "IMap`2" when args.Count == 2:
                WriteDictionary(writer, members, args[0], args[1]);
                break;
            case "IMapView`2" when args.Count == 2:
                WriteReadOnlyDictionary(writer, members, args[0], args[1]);
                break;
            case "IVector`1" when args.Count == 1:
                WriteList(writer, members, args[0]);
                break;
            case "IVectorView`1" when args.Count == 1:
                WriteReadOnlyList(writer, members, args[0]);
                break;
        }
    }

    /// <summary>Emits the <c>IEnumerable&lt;T&gt;</c> members.</summary>
    private static void WriteEnumerable(IndentedTextWriter writer, ImplementableMemberSet members, string t)
    {
        WriteMethod(writer, members, $"global::System.Collections.Generic.IEnumerator<{t}>", "GetEnumerator", "");

        WriteNonGenericGetEnumerator(writer, members);
    }

    /// <summary>Emits the <c>IEnumerator&lt;T&gt;</c> members.</summary>
    private static void WriteEnumerator(IndentedTextWriter writer, ImplementableMemberSet members, string t)
    {
        WriteProperty(writer, members, t, "Current", hasSetter: false);
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
    private static void WriteDictionary(IndentedTextWriter writer, ImplementableMemberSet members, string k, string v)
    {
        string pair = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";

        WriteIndexer(writer, members, v, k, "key", hasSetter: true);
        WriteProperty(writer, members, $"global::System.Collections.Generic.ICollection<{k}>", "Keys", hasSetter: false);
        WriteProperty(writer, members, $"global::System.Collections.Generic.ICollection<{v}>", "Values", hasSetter: false);
        WriteProperty(writer, members, "int", "Count", hasSetter: false);
        WriteProperty(writer, members, "bool", "IsReadOnly", hasSetter: false);
        WriteMethod(writer, members, "void", "Add", $"{k} key, {v} value");
        WriteMethod(writer, members, "void", "Add", $"{pair} item");
        WriteMethod(writer, members, "void", "Clear", "");
        WriteMethod(writer, members, "bool", "Contains", $"{pair} item");
        WriteMethod(writer, members, "bool", "ContainsKey", $"{k} key");
        WriteMethod(writer, members, "void", "CopyTo", $"{pair}[] array, int arrayIndex");
        WriteMethod(writer, members, "bool", "Remove", $"{k} key");
        WriteMethod(writer, members, "bool", "Remove", $"{pair} item");
        WriteMethod(writer, members, "bool", "TryGetValue", $"{k} key, out {v} value");

        WriteEnumerable(writer, members, pair);
    }

    /// <summary>Emits the <c>IReadOnlyDictionary&lt;K, V&gt;</c> members.</summary>
    private static void WriteReadOnlyDictionary(IndentedTextWriter writer, ImplementableMemberSet members, string k, string v)
    {
        string pair = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";

        WriteIndexer(writer, members, v, k, "key", hasSetter: false);
        WriteProperty(writer, members, $"global::System.Collections.Generic.IEnumerable<{k}>", "Keys", hasSetter: false);
        WriteProperty(writer, members, $"global::System.Collections.Generic.IEnumerable<{v}>", "Values", hasSetter: false);
        WriteProperty(writer, members, "int", "Count", hasSetter: false);
        WriteMethod(writer, members, "bool", "ContainsKey", $"{k} key");
        WriteMethod(writer, members, "bool", "TryGetValue", $"{k} key, out {v} value");

        WriteEnumerable(writer, members, pair);
    }

    /// <summary>Emits the <c>IList&lt;T&gt;</c> members.</summary>
    private static void WriteList(IndentedTextWriter writer, ImplementableMemberSet members, string t)
    {
        WriteIndexer(writer, members, t, "int", "index", hasSetter: true, indexerName: "ListItem");
        WriteProperty(writer, members, "int", "Count", hasSetter: false);
        WriteProperty(writer, members, "bool", "IsReadOnly", hasSetter: false);
        WriteMethod(writer, members, "void", "Add", $"{t} item");
        WriteMethod(writer, members, "void", "Clear", "");
        WriteMethod(writer, members, "bool", "Contains", $"{t} item");
        WriteMethod(writer, members, "void", "CopyTo", $"{t}[] array, int arrayIndex");
        WriteMethod(writer, members, "int", "IndexOf", $"{t} item");
        WriteMethod(writer, members, "void", "Insert", $"int index, {t} item");
        WriteMethod(writer, members, "bool", "Remove", $"{t} item");
        WriteMethod(writer, members, "void", "RemoveAt", "int index");

        WriteEnumerable(writer, members, t);
    }

    /// <summary>Emits the <c>IReadOnlyList&lt;T&gt;</c> members.</summary>
    private static void WriteReadOnlyList(IndentedTextWriter writer, ImplementableMemberSet members, string t)
    {
        WriteIndexer(writer, members, t, "int", "index", hasSetter: false, indexerName: "ReadOnlyListItem");
        WriteProperty(writer, members, "int", "Count", hasSetter: false);

        WriteEnumerable(writer, members, t);
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
