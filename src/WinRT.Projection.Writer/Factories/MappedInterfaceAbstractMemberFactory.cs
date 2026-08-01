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
/// This is the counterpart of <see cref="MappedInterfaceStubFactory"/>: that one emits <em>delegating</em>
/// members for the projected class (which wraps a native object), whereas an implementable abstract base
/// has no implementation to delegate to and instead declares the members <c>abstract</c>, so the C# compiler
/// forces the author to supply all of them.
/// </para>
/// <para>
/// The members are declared in their .NET shape, not their Windows Runtime one, because the .NET shape is
/// what the mapped interface actually requires. The interop generator recognises a type implementing e.g.
/// <c>IDictionary&lt;K, V&gt;</c> and emits the corresponding <c>IMap&lt;K, V&gt;</c> COM interface entry, so
/// the resulting vtable is the Windows Runtime one either way.
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
    /// <param name="writtenMembers">Signature keys of members already written, so overlapping interfaces (e.g. <c>IMap`2</c> and <c>IIterable`1</c>, which both surface <c>GetEnumerator</c>) each contribute their members only once.</param>
    public static void WriteAbstractMembers(
        IndentedTextWriter writer,
        ProjectionEmitContext context,
        GenericInstanceTypeSignature? instance,
        string ifaceName,
        HashSet<string> writtenMembers)
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
                WriteMethod(writer, writtenMembers, "void", "Dispose", "");
                break;
            case "IIterable`1" when args.Count == 1:
                WriteEnumerable(writer, writtenMembers, args[0]);
                break;
            case "IIterator`1" when args.Count == 1:
                WriteEnumerator(writer, writtenMembers, args[0]);
                break;
            case "IMap`2" when args.Count == 2:
                WriteDictionary(writer, writtenMembers, args[0], args[1]);
                break;
            case "IMapView`2" when args.Count == 2:
                WriteReadOnlyDictionary(writer, writtenMembers, args[0], args[1]);
                break;
            case "IVector`1" when args.Count == 1:
                WriteList(writer, writtenMembers, args[0]);
                break;
            case "IVectorView`1" when args.Count == 1:
                WriteReadOnlyList(writer, writtenMembers, args[0]);
                break;
        }
    }

    /// <summary>Emits the <c>IEnumerable&lt;T&gt;</c> members.</summary>
    private static void WriteEnumerable(IndentedTextWriter writer, HashSet<string> writtenMembers, string t)
    {
        WriteMethod(writer, writtenMembers, $"global::System.Collections.Generic.IEnumerator<{t}>", "GetEnumerator", "");

        WriteNonGenericGetEnumerator(writer, writtenMembers);
    }

    /// <summary>Emits the <c>IEnumerator&lt;T&gt;</c> members.</summary>
    private static void WriteEnumerator(IndentedTextWriter writer, HashSet<string> writtenMembers, string t)
    {
        WriteProperty(writer, writtenMembers, t, "Current", hasSetter: false);
        WriteMethod(writer, writtenMembers, "bool", "MoveNext", "");
        WriteMethod(writer, writtenMembers, "void", "Reset", "");
        WriteMethod(writer, writtenMembers, "void", "Dispose", "");

        // An explicit interface implementation cannot be abstract, so the non-generic member forwards
        // to the generic one that the author implements.
        if (writtenMembers.Add("explicit:IEnumerator.Current"))
        {
            writer.WriteLine();
            writer.WriteLine("object? global::System.Collections.IEnumerator.Current => Current;");
        }
    }

    /// <summary>Emits the <c>IDictionary&lt;K, V&gt;</c> members.</summary>
    private static void WriteDictionary(IndentedTextWriter writer, HashSet<string> writtenMembers, string k, string v)
    {
        string pair = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";

        WriteIndexer(writer, writtenMembers, v, k, "key", hasSetter: true);
        WriteProperty(writer, writtenMembers, $"global::System.Collections.Generic.ICollection<{k}>", "Keys", hasSetter: false);
        WriteProperty(writer, writtenMembers, $"global::System.Collections.Generic.ICollection<{v}>", "Values", hasSetter: false);
        WriteProperty(writer, writtenMembers, "int", "Count", hasSetter: false);
        WriteProperty(writer, writtenMembers, "bool", "IsReadOnly", hasSetter: false);
        WriteMethod(writer, writtenMembers, "void", "Add", $"{k} key, {v} value");
        WriteMethod(writer, writtenMembers, "void", "Add", $"{pair} item");
        WriteMethod(writer, writtenMembers, "void", "Clear", "");
        WriteMethod(writer, writtenMembers, "bool", "Contains", $"{pair} item");
        WriteMethod(writer, writtenMembers, "bool", "ContainsKey", $"{k} key");
        WriteMethod(writer, writtenMembers, "void", "CopyTo", $"{pair}[] array, int arrayIndex");
        WriteMethod(writer, writtenMembers, "bool", "Remove", $"{k} key");
        WriteMethod(writer, writtenMembers, "bool", "Remove", $"{pair} item");
        WriteMethod(writer, writtenMembers, "bool", "TryGetValue", $"{k} key, out {v} value");

        WriteEnumerable(writer, writtenMembers, pair);
    }

    /// <summary>Emits the <c>IReadOnlyDictionary&lt;K, V&gt;</c> members.</summary>
    private static void WriteReadOnlyDictionary(IndentedTextWriter writer, HashSet<string> writtenMembers, string k, string v)
    {
        string pair = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";

        WriteIndexer(writer, writtenMembers, v, k, "key", hasSetter: false);
        WriteProperty(writer, writtenMembers, $"global::System.Collections.Generic.IEnumerable<{k}>", "Keys", hasSetter: false);
        WriteProperty(writer, writtenMembers, $"global::System.Collections.Generic.IEnumerable<{v}>", "Values", hasSetter: false);
        WriteProperty(writer, writtenMembers, "int", "Count", hasSetter: false);
        WriteMethod(writer, writtenMembers, "bool", "ContainsKey", $"{k} key");
        WriteMethod(writer, writtenMembers, "bool", "TryGetValue", $"{k} key, out {v} value");

        WriteEnumerable(writer, writtenMembers, pair);
    }

    /// <summary>Emits the <c>IList&lt;T&gt;</c> members.</summary>
    private static void WriteList(IndentedTextWriter writer, HashSet<string> writtenMembers, string t)
    {
        WriteIndexer(writer, writtenMembers, t, "int", "index", hasSetter: true, indexerName: "ListItem");
        WriteProperty(writer, writtenMembers, "int", "Count", hasSetter: false);
        WriteProperty(writer, writtenMembers, "bool", "IsReadOnly", hasSetter: false);
        WriteMethod(writer, writtenMembers, "void", "Add", $"{t} item");
        WriteMethod(writer, writtenMembers, "void", "Clear", "");
        WriteMethod(writer, writtenMembers, "bool", "Contains", $"{t} item");
        WriteMethod(writer, writtenMembers, "void", "CopyTo", $"{t}[] array, int arrayIndex");
        WriteMethod(writer, writtenMembers, "int", "IndexOf", $"{t} item");
        WriteMethod(writer, writtenMembers, "void", "Insert", $"int index, {t} item");
        WriteMethod(writer, writtenMembers, "bool", "Remove", $"{t} item");
        WriteMethod(writer, writtenMembers, "void", "RemoveAt", "int index");

        WriteEnumerable(writer, writtenMembers, t);
    }

    /// <summary>Emits the <c>IReadOnlyList&lt;T&gt;</c> members.</summary>
    private static void WriteReadOnlyList(IndentedTextWriter writer, HashSet<string> writtenMembers, string t)
    {
        WriteIndexer(writer, writtenMembers, t, "int", "index", hasSetter: false, indexerName: "ReadOnlyListItem");
        WriteProperty(writer, writtenMembers, "int", "Count", hasSetter: false);

        WriteEnumerable(writer, writtenMembers, t);
    }

    /// <summary>
    /// Emits the non-generic <c>IEnumerable.GetEnumerator</c> forwarder, which cannot be <c>abstract</c>
    /// because it is an explicit interface implementation.
    /// </summary>
    private static void WriteNonGenericGetEnumerator(IndentedTextWriter writer, HashSet<string> writtenMembers)
    {
        if (!writtenMembers.Add("explicit:IEnumerable.GetEnumerator"))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();");
    }

    /// <summary>Emits a single <c>public abstract</c> method, if not already written.</summary>
    private static void WriteMethod(IndentedTextWriter writer, HashSet<string> writtenMembers, string returnType, string name, string parameters)
    {
        if (!writtenMembers.Add($"M:{name}({parameters})"))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"public abstract {returnType} {name}({parameters});");
    }

    /// <summary>Emits a single <c>public abstract</c> property, if not already written.</summary>
    private static void WriteProperty(IndentedTextWriter writer, HashSet<string> writtenMembers, string type, string name, bool hasSetter)
    {
        if (!writtenMembers.Add($"P:{name}"))
        {
            return;
        }

        writer.WriteLine();
        writer.WriteLine($"public abstract {type} {name} {{ get;{(hasSetter ? " set;" : "")} }}");
    }

    /// <summary>Emits a single <c>public abstract</c> indexer, if not already written.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="writtenMembers">Signature keys of members already written.</param>
    /// <param name="type">The indexer's element type.</param>
    /// <param name="indexType">The index parameter's type.</param>
    /// <param name="indexName">The index parameter's name.</param>
    /// <param name="hasSetter">Whether the indexer has a setter.</param>
    /// <param name="indexerName">The <see cref="System.Runtime.CompilerServices.IndexerNameAttribute"/> name to apply, or <see langword="null"/> to leave the default (<c>Item</c>).</param>
    private static void WriteIndexer(
        IndentedTextWriter writer,
        HashSet<string> writtenMembers,
        string type,
        string indexType,
        string indexName,
        bool hasSetter,
        string? indexerName = null)
    {
        if (!writtenMembers.Add($"P:this[{indexType}]"))
        {
            return;
        }

        // A C# indexer is named 'Item' in metadata unless renamed, so an indexer keeping the default name
        // would collide with the Windows Runtime member of that name (which is how an indexer appears in the
        // metadata this base is otherwise generated from).
        if (indexerName is null)
        {
            _ = writtenMembers.Add("P:Item");
            _ = writtenMembers.Add("M:Item(uint index)");
        }

        writer.WriteLine();

        if (indexerName is not null)
        {
            writer.WriteLine($"[global::System.Runtime.CompilerServices.IndexerName(\"{indexerName}\")]");
        }

        writer.WriteLine($"public abstract {type} this[{indexType} {indexName}] {{ get;{(hasSetter ? " set;" : "")} }}");
    }
}
