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
    /// <param name="instance">The (possibly substituted) generic instance signature for the interface, or null if non-generic.</param>
    /// <param name="ifaceName">The WinRT interface name (e.g. "IMap`2").</param>
    /// <param name="objRefName">The name of the lazy <c>_objRef_*</c> field for the interface on the class.</param>
    public static void WriteMappedInterfaceStubs(TypeWriter w, GenericInstanceTypeSignature? instance, string ifaceName, string objRefName)
    {
        // Resolve type arguments from the (substituted) generic instance signature, if any.
        List<TypeSemantics> typeArgs = new();
        List<TypeSignature> typeArgSigs = new();
        if (instance is not null)
        {
            foreach (TypeSignature arg in instance.TypeArguments)
            {
                typeArgs.Add(TypeSemanticsFactory.Get(arg));
                typeArgSigs.Add(arg);
            }
        }

        switch (ifaceName)
        {
            case "IClosable":
                EmitDisposable(w, objRefName);
                break;
            case "IIterable`1":
                EmitGenericEnumerable(w, typeArgs, typeArgSigs, objRefName);
                break;
            case "IIterator`1":
                EmitGenericEnumerator(w, typeArgs, typeArgSigs, objRefName);
                break;
            case "IMap`2":
                EmitDictionary(w, typeArgs, typeArgSigs, objRefName);
                break;
            case "IMapView`2":
                EmitReadOnlyDictionary(w, typeArgs, typeArgSigs, objRefName);
                break;
            case "IVector`1":
                EmitList(w, typeArgs, typeArgSigs, objRefName);
                break;
            case "IVectorView`1":
                EmitReadOnlyList(w, typeArgs, typeArgSigs, objRefName);
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

    private static void EmitDisposable(TypeWriter w, string objRefName)
    {
        w.Write("\npublic void Dispose() => global::ABI.System.IDisposableMethods.Dispose(");
        w.Write(objRefName);
        w.Write(");\n");
    }

    private static void EmitGenericEnumerable(TypeWriter w, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string elementId = EncodeArgIdentifier(w, args[0]);
        string interopTypeArgs = EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IEnumerable'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IEnumerableMethods_" + elementId + "_";

        w.Write("\n");
        EmitUnsafeAccessor(w, "GetEnumerator", $"IEnumerator<{t}>", $"{prefix}GetEnumerator", interopType, "");

        w.Write($"\npublic IEnumerator<{t}> GetEnumerator() => {prefix}GetEnumerator(null, {objRefName});\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();\n");
    }

    private static void EmitGenericEnumerator(TypeWriter w, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string elementId = EncodeArgIdentifier(w, args[0]);
        string interopTypeArgs = EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IEnumerator'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IEnumeratorMethods_" + elementId + "_";

        w.Write("\n");
        EmitUnsafeAccessor(w, "Current", t, $"{prefix}Current", interopType, "");
        EmitUnsafeAccessor(w, "MoveNext", "bool", $"{prefix}MoveNext", interopType, "");
        EmitUnsafeAccessor(w, "Reset", "void", $"{prefix}Reset", interopType, "");
        EmitUnsafeAccessor(w, "Dispose", "void", $"{prefix}Dispose", interopType, "");

        w.Write($"\npublic bool MoveNext() => {prefix}MoveNext(null, {objRefName});\n");
        w.Write($"public void Reset() => {prefix}Reset(null, {objRefName});\n");
        w.Write($"public {t} Current => {prefix}Current(null, {objRefName});\n");
        w.Write("object global::System.Collections.IEnumerator.Current => Current!;\n");
        w.Write($"public void Dispose() => {prefix}Dispose(null, {objRefName});\n");
    }

    private static void EmitDictionary(TypeWriter w, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 2) { return; }
        string k = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string v = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[1], TypedefNameType.Projected, true)));
        // Truth uses the fully-qualified 'global::System.Collections.Generic.KeyValuePair<K,V>' form for KeyValuePair
        // generic args (the C++ generator emits via the IKeyValuePair mapped type which forces the global:: prefix).
        string kv = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";
        // Long form (same as kv now) used for objref field-name computation.
        string kvLong = kv;
        string keyId = EncodeArgIdentifier(w, args[0]);
        string valId = EncodeArgIdentifier(w, args[1]);
        string keyInteropArg = EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string valInteropArg = EncodeInteropTypeName(argSigs[1], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IDictionary'2<" + keyInteropArg + "|" + valInteropArg + ">Methods, WinRT.Interop";
        string prefix = "IDictionaryMethods_" + keyId + "_" + valId + "_";
        // The IEnumerable<KeyValuePair<K,V>> objref name (matches what WriteClassObjRefDefinitions emits transitively).
        string enumerableObjRefName = "_objRef_System_Collections_Generic_IEnumerable_" + EscapeTypeNameForIdentifier(kvLong, stripGlobal: false) + "_";
        string kvInteropArgs = "<#corlib>System-Collections-Generic-KeyValuePair'2<" + keyInteropArg + "|" + valInteropArg + ">";
        string enumerableInteropType = "ABI.System.Collections.Generic.<#corlib>IEnumerable'1<" + kvInteropArgs + ">Methods, WinRT.Interop";
        string enumerablePrefix = "IEnumerableMethods_System_Collections_Generic_KeyValuePair_" + keyId + "__" + valId + "__";

        w.Write("\n");
        EmitUnsafeAccessor(w, "Keys", $"ICollection<{k}>", $"{prefix}Keys", interopType, "");
        EmitUnsafeAccessor(w, "Values", $"ICollection<{v}>", $"{prefix}Values", interopType, "");
        EmitUnsafeAccessor(w, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(w, "Item", v, $"{prefix}Item", interopType, $", {k} key");
        EmitUnsafeAccessor(w, "Item", "void", $"{prefix}Item", interopType, $", {k} key, {v} value");
        EmitUnsafeAccessor(w, "Add", "void", $"{prefix}Add", interopType, $", {k} key, {v} value");
        EmitUnsafeAccessor(w, "ContainsKey", "bool", $"{prefix}ContainsKey", interopType, $", {k} key");
        EmitUnsafeAccessor(w, "Remove", "bool", $"{prefix}Remove", interopType, $", {k} key");
        EmitUnsafeAccessor(w, "TryGetValue", "bool", $"{prefix}TryGetValue", interopType, $", {k} key, out {v} value");
        EmitUnsafeAccessor(w, "Add", "void", $"{prefix}AddPair", interopType, $", {kv} item");
        EmitUnsafeAccessor(w, "Clear", "void", $"{prefix}Clear", interopType, "");
        EmitUnsafeAccessor(w, "Contains", "bool", $"{prefix}Contains", interopType, $", {kv} item");
        EmitUnsafeAccessor(w, "CopyTo", "void", $"{prefix}CopyTo", interopType, $", WindowsRuntimeObjectReference enumObjRef, {kv}[] array, int arrayIndex");
        EmitUnsafeAccessor(w, "Remove", "bool", $"{prefix}Remove", interopType, $", {kv} item");
        EmitUnsafeAccessor(w, "GetEnumerator", $"IEnumerator<{kv}>", $"{enumerablePrefix}GetEnumerator", enumerableInteropType, "");

        w.Write($"\npublic {v} this[{k} key] {{ get => {prefix}Item(null, {objRefName}, key); set => {prefix}Item(null, {objRefName}, key, value); }}\n");
        w.Write($"public ICollection<{k}> Keys => {prefix}Keys(null, {objRefName});\n");
        w.Write($"public ICollection<{v}> Values => {prefix}Values(null, {objRefName});\n");
        w.Write($"public int Count => {prefix}Count(null, {objRefName});\n");
        w.Write("public bool IsReadOnly => false;\n");
        w.Write($"public void Add({k} key, {v} value) => {prefix}Add(null, {objRefName}, key, value);\n");
        w.Write($"public bool ContainsKey({k} key) => {prefix}ContainsKey(null, {objRefName}, key);\n");
        w.Write($"public bool Remove({k} key) => {prefix}Remove(null, {objRefName}, key);\n");
        w.Write($"public bool TryGetValue({k} key, out {v} value) => {prefix}TryGetValue(null, {objRefName}, key, out value);\n");
        w.Write($"public void Add({kv} item) => {prefix}AddPair(null, {objRefName}, item);\n");
        w.Write($"public void Clear() => {prefix}Clear(null, {objRefName});\n");
        w.Write($"public bool Contains({kv} item) => {prefix}Contains(null, {objRefName}, item);\n");
        w.Write($"public void CopyTo({kv}[] array, int arrayIndex) => {prefix}CopyTo(null, {objRefName}, {enumerableObjRefName}, array, arrayIndex);\n");
        // ICollection<KVP>.Remove must be explicit to avoid clashing with IDictionary<K,V>.Remove(K key).
        w.Write($"bool ICollection<{kv}>.Remove({kv} item) => {prefix}Remove(null, {objRefName}, item);\n");
        w.Write($"public IEnumerator<{kv}> GetEnumerator() => {enumerablePrefix}GetEnumerator(null, {enumerableObjRefName});\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();\n");
    }

    private static void EmitReadOnlyDictionary(TypeWriter w, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 2) { return; }
        string k = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string v = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[1], TypedefNameType.Projected, true)));
        // Truth uses fully-qualified KeyValuePair (mirrors C++ IKeyValuePair mapped projection).
        string kv = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";
        string kvLong = kv;
        string keyId = EncodeArgIdentifier(w, args[0]);
        string valId = EncodeArgIdentifier(w, args[1]);
        string keyInteropArg = EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string valInteropArg = EncodeInteropTypeName(argSigs[1], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IReadOnlyDictionary'2<" + keyInteropArg + "|" + valInteropArg + ">Methods, WinRT.Interop";
        string prefix = "IReadOnlyDictionaryMethods_" + keyId + "_" + valId + "_";
        // IEnumerable<KeyValuePair<K,V>> objref for the typed GetEnumerator.
        string enumerableObjRefName = "_objRef_System_Collections_Generic_IEnumerable_" + EscapeTypeNameForIdentifier(kvLong, stripGlobal: false) + "_";
        string kvInteropArgs = "<#corlib>System-Collections-Generic-KeyValuePair'2<" + keyInteropArg + "|" + valInteropArg + ">";
        string enumerableInteropType = "ABI.System.Collections.Generic.<#corlib>IEnumerable'1<" + kvInteropArgs + ">Methods, WinRT.Interop";
        string enumerablePrefix = "IEnumerableMethods_System_Collections_Generic_KeyValuePair_" + keyId + "__" + valId + "__";

        w.Write("\n");
        EmitUnsafeAccessor(w, "Keys", $"IEnumerable<{k}>", $"{prefix}Keys", interopType, "");
        EmitUnsafeAccessor(w, "Values", $"IEnumerable<{v}>", $"{prefix}Values", interopType, "");
        EmitUnsafeAccessor(w, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(w, "Item", v, $"{prefix}Item", interopType, $", {k} key");
        EmitUnsafeAccessor(w, "ContainsKey", "bool", $"{prefix}ContainsKey", interopType, $", {k} key");
        EmitUnsafeAccessor(w, "TryGetValue", "bool", $"{prefix}TryGetValue", interopType, $", {k} key, out {v} value");
        EmitUnsafeAccessor(w, "GetEnumerator", $"IEnumerator<{kv}>", $"{enumerablePrefix}GetEnumerator", enumerableInteropType, "");

        w.Write($"\npublic {v} this[{k} key] => {prefix}Item(null, {objRefName}, key);\n");
        w.Write($"public IEnumerable<{k}> Keys => {prefix}Keys(null, {objRefName});\n");
        w.Write($"public IEnumerable<{v}> Values => {prefix}Values(null, {objRefName});\n");
        w.Write($"public int Count => {prefix}Count(null, {objRefName});\n");
        w.Write($"public bool ContainsKey({k} key) => {prefix}ContainsKey(null, {objRefName}, key);\n");
        w.Write($"public bool TryGetValue({k} key, out {v} value) => {prefix}TryGetValue(null, {objRefName}, key, out value);\n");
        w.Write($"public IEnumerator<{kv}> GetEnumerator() => {enumerablePrefix}GetEnumerator(null, {enumerableObjRefName});\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();\n");
    }

    private static void EmitReadOnlyList(TypeWriter w, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string elementId = EncodeArgIdentifier(w, args[0]);
        string interopTypeArgs = EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IReadOnlyList'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IReadOnlyListMethods_" + elementId + "_";
        string enumerableObjRefName = "_objRef_System_Collections_Generic_IEnumerable_" + EscapeTypeNameForIdentifier(t, stripGlobal: false) + "_";
        string enumerableInteropType = "ABI.System.Collections.Generic.<#corlib>IEnumerable'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string enumerablePrefix = "IEnumerableMethods_" + elementId + "_";

        w.Write("\n");
        EmitUnsafeAccessor(w, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(w, "Item", t, $"{prefix}Item", interopType, ", int index");
        EmitUnsafeAccessor(w, "GetEnumerator", $"IEnumerator<{t}>", $"{enumerablePrefix}GetEnumerator", enumerableInteropType, "");

        w.Write("\n[global::System.Runtime.CompilerServices.IndexerName(\"ReadOnlyListItem\")]\n");
        w.Write($"public {t} this[int index] => {prefix}Item(null, {objRefName}, index);\n");
        w.Write($"public int Count => {prefix}Count(null, {objRefName});\n");
        w.Write($"public IEnumerator<{t}> GetEnumerator() => {enumerablePrefix}GetEnumerator(null, {enumerableObjRefName});\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();\n");
    }

    /// <summary>
    /// Helper to encode the WinRT.Interop dictionary key for a type-arg encoded identifier
    /// (used in UnsafeAccessor function names and method-name prefixes). Mirrors C++
    /// <c>escape_type_name_for_identifier(write_type_name(arg), true)</c>.
    /// </summary>
    /// <summary>
    /// Encodes a type semantics as a C# identifier-safe name. Mirrors C++
    /// <c>escape_type_name_for_identifier(write_projection_type(arg), true)</c>:
    /// uses the projected type name WITHOUT forcing namespace qualification, then strips
    /// 'global::' and replaces '.' with '_'.
    /// </summary>
    private static string EncodeArgIdentifier(TypeWriter w, TypeSemantics arg)
    {
        string projected = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, arg, TypedefNameType.Projected, false)));
        return EscapeTypeNameForIdentifier(projected, stripGlobal: true);
    }

    private static void EmitList(TypeWriter w, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteTypeName(w, args[0], TypedefNameType.Projected, true)));
        string elementId = EncodeArgIdentifier(w, args[0]);
        string interopTypeArgs = EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IList'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IListMethods_" + elementId + "_";
        // The IEnumerable<T> objref name (matches what WriteClassObjRefDefinitions emits transitively).
        string enumerableObjRefName = "_objRef_System_Collections_Generic_IEnumerable_" + EscapeTypeNameForIdentifier(t, stripGlobal: false) + "_";
        string enumerableInteropType = "ABI.System.Collections.Generic.<#corlib>IEnumerable'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string enumerablePrefix = "IEnumerableMethods_" + elementId + "_";

        w.Write("\n");
        EmitUnsafeAccessor(w, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(w, "Item", t, $"{prefix}Item", interopType, ", int index");
        EmitUnsafeAccessor(w, "Item", "void", $"{prefix}Item", interopType, $", int index, {t} value");
        EmitUnsafeAccessor(w, "IndexOf", "int", $"{prefix}IndexOf", interopType, $", {t} item");
        EmitUnsafeAccessor(w, "Insert", "void", $"{prefix}Insert", interopType, $", int index, {t} item");
        EmitUnsafeAccessor(w, "RemoveAt", "void", $"{prefix}RemoveAt", interopType, ", int index");
        EmitUnsafeAccessor(w, "Add", "void", $"{prefix}Add", interopType, $", {t} item");
        EmitUnsafeAccessor(w, "Clear", "void", $"{prefix}Clear", interopType, "");
        EmitUnsafeAccessor(w, "Contains", "bool", $"{prefix}Contains", interopType, $", {t} item");
        EmitUnsafeAccessor(w, "CopyTo", "void", $"{prefix}CopyTo", interopType, $", {t}[] array, int arrayIndex");
        EmitUnsafeAccessor(w, "Remove", "bool", $"{prefix}Remove", interopType, $", {t} item");
        EmitUnsafeAccessor(w, "GetEnumerator", $"IEnumerator<{t}>", $"{enumerablePrefix}GetEnumerator", enumerableInteropType, "");

        w.Write("\n[global::System.Runtime.CompilerServices.IndexerName(\"ListItem\")]\n");
        w.Write($"public {t} this[int index]\n{{\n    get => {prefix}Item(null, {objRefName}, index);\n    set => {prefix}Item(null, {objRefName}, index, value);\n}}\n");
        w.Write($"public int Count => {prefix}Count(null, {objRefName});\n");
        w.Write("public bool IsReadOnly => false;\n");
        w.Write($"public void Add({t} item) => {prefix}Add(null, {objRefName}, item);\n");
        w.Write($"public void Clear() => {prefix}Clear(null, {objRefName});\n");
        w.Write($"public bool Contains({t} item) => {prefix}Contains(null, {objRefName}, item);\n");
        w.Write($"public void CopyTo({t}[] array, int arrayIndex) => {prefix}CopyTo(null, {objRefName}, array, arrayIndex);\n");
        w.Write($"public int IndexOf({t} item) => {prefix}IndexOf(null, {objRefName}, item);\n");
        w.Write($"public void Insert(int index, {t} item) => {prefix}Insert(null, {objRefName}, index, item);\n");
        w.Write($"public bool Remove({t} item) => {prefix}Remove(null, {objRefName}, item);\n");
        w.Write($"public void RemoveAt(int index) => {prefix}RemoveAt(null, {objRefName}, index);\n");
        w.Write($"public IEnumerator<{t}> GetEnumerator() => {enumerablePrefix}GetEnumerator(null, {enumerableObjRefName});\n");
        w.Write("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();\n");
    }

    /// <summary>
    /// Emits a single <c>[UnsafeAccessor]</c> static extern declaration that targets a method on a
    /// WinRT.Interop helper type. The function signature is built from the supplied parts.
    /// </summary>
    private static void EmitUnsafeAccessor(TypeWriter w, string accessName, string returnType, string functionName, string interopType, string extraParams)
    {
        w.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"");
        w.Write(accessName);
        w.Write("\")]\n");
        w.Write("static extern ");
        w.Write(returnType);
        w.Write(" ");
        w.Write(functionName);
        w.Write("([UnsafeAccessorType(\"");
        w.Write(interopType);
        w.Write("\")] object _, WindowsRuntimeObjectReference objRef");
        w.Write(extraParams);
        w.Write(");\n");
    }

    private static void EmitNonGenericList(TypeWriter w)
    {
        w.Write("\n[global::System.Runtime.CompilerServices.IndexerName(\"BindableListItem\")]\n");
        w.Write("public object this[int index] { get => throw null!; set => throw null!; }\n");
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
