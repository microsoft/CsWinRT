// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Emits stub members ('=> throw null!') for well-known C# interfaces that come from mapped
/// WinRT interfaces (IClosable -> IDisposable, IMap`2 -> IDictionary&lt;K,V&gt;, etc.). The
/// runtime adapter actually services these at runtime via IDynamicInterfaceCastable, but the
/// C# compiler still requires the class to declare the members.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Returns true if the WinRT interface (by namespace+name) is a mapped interface that
    /// requires emitting C#-interface stub members on the implementing class.
    /// </summary>
    public static bool IsMappedInterfaceRequiringStubs(string ifaceNs, string ifaceName)
    {
        if (MappedTypes.Get(ifaceNs, ifaceName) is not { HasCustomMembersOutput: true })
        {
            return false;
        }
        return ifaceName switch
        {
            "IClosable" => true,
            "IIterable`1" or "IIterator`1" => true,
            "IMap`2" or "IMapView`2" => true,
            "IVector`1" or "IVectorView`1" => true,
            "IBindableIterable" or "IBindableIterator" or "IBindableVector" => true,
            "INotifyDataErrorInfo" => true,
            _ => false,
        };
    }

    /// <summary>
    /// Emits the C# interface stub members for the given WinRT interface that maps to a known
    /// .NET interface.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="ifaceImpl">The InterfaceImpl carrying the generic type arguments.</param>
    /// <param name="ifaceName">The WinRT interface name (e.g. "IMap`2").</param>
    public static void WriteMappedInterfaceStubs(TypeWriter w, InterfaceImplementation ifaceImpl, string ifaceName)
    {
        // Resolve type arguments from the InterfaceImpl signature, if any.
        List<TypeSemantics> typeArgs = new();
        if (ifaceImpl.Interface is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            foreach (TypeSignature arg in gi.TypeArguments)
            {
                typeArgs.Add(TypeSemanticsFactory.Get(arg));
            }
        }

        switch (ifaceName)
        {
            case "IClosable":
                EmitDisposable(w);
                break;
            case "IIterable`1":
                EmitGenericEnumerable(w, typeArgs);
                break;
            case "IIterator`1":
                EmitGenericEnumerator(w, typeArgs);
                break;
            case "IMap`2":
                EmitDictionary(w, typeArgs);
                break;
            case "IMapView`2":
                EmitReadOnlyDictionary(w, typeArgs);
                break;
            case "IVector`1":
                EmitList(w, typeArgs);
                break;
            case "IVectorView`1":
                EmitReadOnlyList(w, typeArgs);
                break;
            case "IBindableIterable":
                w.Write("\nglobal::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => throw null!;\n");
                break;
            case "IBindableIterator":
                w.Write("\nbool global::System.Collections.IEnumerator.MoveNext() => throw null!;\n");
                w.Write("void global::System.Collections.IEnumerator.Reset() => throw null!;\n");
                w.Write("object global::System.Collections.IEnumerator.Current => throw null!;\n");
                break;
            case "IBindableVector":
                EmitNonGenericList(w);
                break;
            case "INotifyDataErrorInfo":
                w.Write("\npublic global::System.Collections.IEnumerable GetErrors(string propertyName) => throw null!;\n");
                w.Write("public bool HasErrors => throw null!;\n");
                w.Write("public event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> ErrorsChanged { add => throw null!; remove => throw null!; }\n");
                break;
        }
    }

    private static void EmitDisposable(TypeWriter w)
    {
        w.Write("\npublic void Dispose() => throw null!;\n");
    }

    private static void EmitGenericEnumerable(TypeWriter w, List<TypeSemantics> args)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        w.Write($"\npublic global::System.Collections.Generic.IEnumerator<{t}> GetEnumerator() => throw null!;\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => throw null!;\n");
    }

    private static void EmitGenericEnumerator(TypeWriter w, List<TypeSemantics> args)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        w.Write($"\npublic bool MoveNext() => throw null!;\n");
        w.Write("public void Reset() => throw null!;\n");
        w.Write($"public {t} Current => throw null!;\n");
        w.Write("object global::System.Collections.IEnumerator.Current => throw null!;\n");
        w.Write("public void Dispose() => throw null!;\n");
    }

    private static void EmitDictionary(TypeWriter w, List<TypeSemantics> args)
    {
        if (args.Count != 2) { return; }
        string k = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string v = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[1], TypedefNameType.Projected, true)));
        string kv = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";
        w.Write($"\npublic {v} this[{k} key] {{ get => throw null!; set => throw null!; }}\n");
        w.Write($"public global::System.Collections.Generic.ICollection<{k}> Keys => throw null!;\n");
        w.Write($"public global::System.Collections.Generic.ICollection<{v}> Values => throw null!;\n");
        w.Write("public int Count => throw null!;\n");
        w.Write("public bool IsReadOnly => throw null!;\n");
        w.Write($"public void Add({k} key, {v} value) => throw null!;\n");
        w.Write($"public bool ContainsKey({k} key) => throw null!;\n");
        w.Write($"public bool Remove({k} key) => throw null!;\n");
        w.Write($"public bool TryGetValue({k} key, out {v} value) => throw null!;\n");
        w.Write($"public void Add({kv} item) => throw null!;\n");
        w.Write("public void Clear() => throw null!;\n");
        w.Write($"public bool Contains({kv} item) => throw null!;\n");
        w.Write($"public void CopyTo({kv}[] array, int arrayIndex) => throw null!;\n");
        w.Write($"public bool Remove({kv} item) => throw null!;\n");
        w.Write($"public global::System.Collections.Generic.IEnumerator<{kv}> GetEnumerator() => throw null!;\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => throw null!;\n");
    }

    private static void EmitReadOnlyDictionary(TypeWriter w, List<TypeSemantics> args)
    {
        if (args.Count != 2) { return; }
        string k = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string v = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[1], TypedefNameType.Projected, true)));
        string kv = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";
        w.Write($"\npublic {v} this[{k} key] => throw null!;\n");
        w.Write($"public global::System.Collections.Generic.IEnumerable<{k}> Keys => throw null!;\n");
        w.Write($"public global::System.Collections.Generic.IEnumerable<{v}> Values => throw null!;\n");
        w.Write("public int Count => throw null!;\n");
        w.Write($"public bool ContainsKey({k} key) => throw null!;\n");
        w.Write($"public bool TryGetValue({k} key, out {v} value) => throw null!;\n");
        w.Write($"public global::System.Collections.Generic.IEnumerator<{kv}> GetEnumerator() => throw null!;\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => throw null!;\n");
    }

    private static void EmitList(TypeWriter w, List<TypeSemantics> args)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        w.Write($"\npublic {t} this[int index] {{ get => throw null!; set => throw null!; }}\n");
        w.Write("public int Count => throw null!;\n");
        w.Write("public bool IsReadOnly => throw null!;\n");
        w.Write($"public void Add({t} item) => throw null!;\n");
        w.Write("public void Clear() => throw null!;\n");
        w.Write($"public bool Contains({t} item) => throw null!;\n");
        w.Write($"public void CopyTo({t}[] array, int arrayIndex) => throw null!;\n");
        w.Write($"public int IndexOf({t} item) => throw null!;\n");
        w.Write($"public void Insert(int index, {t} item) => throw null!;\n");
        w.Write($"public bool Remove({t} item) => throw null!;\n");
        w.Write("public void RemoveAt(int index) => throw null!;\n");
        w.Write($"public global::System.Collections.Generic.IEnumerator<{t}> GetEnumerator() => throw null!;\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => throw null!;\n");
    }

    private static void EmitReadOnlyList(TypeWriter w, List<TypeSemantics> args)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        w.Write($"\npublic {t} this[int index] => throw null!;\n");
        w.Write("public int Count => throw null!;\n");
        w.Write($"public global::System.Collections.Generic.IEnumerator<{t}> GetEnumerator() => throw null!;\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => throw null!;\n");
    }

    private static void EmitNonGenericList(TypeWriter w)
    {
        w.Write("\npublic object this[int index] { get => throw null!; set => throw null!; }\n");
        w.Write("public int Count => throw null!;\n");
        w.Write("public bool IsReadOnly => throw null!;\n");
        w.Write("public bool IsFixedSize => throw null!;\n");
        w.Write("public bool IsSynchronized => throw null!;\n");
        w.Write("public object SyncRoot => throw null!;\n");
        w.Write("public int Add(object value) => throw null!;\n");
        w.Write("public void Clear() => throw null!;\n");
        w.Write("public bool Contains(object value) => throw null!;\n");
        w.Write("public int IndexOf(object value) => throw null!;\n");
        w.Write("public void Insert(int index, object value) => throw null!;\n");
        w.Write("public void Remove(object value) => throw null!;\n");
        w.Write("public void RemoveAt(int index) => throw null!;\n");
        w.Write("public void CopyTo(global::System.Array array, int index) => throw null!;\n");
        w.Write("public global::System.Collections.IEnumerator GetEnumerator() => throw null!;\n");
    }
}
