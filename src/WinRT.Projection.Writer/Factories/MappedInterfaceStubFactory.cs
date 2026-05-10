// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits stub members ('=> throw null!') for well-known C# interfaces that come from mapped
/// WinRT interfaces (IClosable -> IDisposable, IMap`2 -> IDictionary&lt;K,V&gt;, etc.). The
/// runtime adapter actually services these at runtime via IDynamicInterfaceCastable, but the
/// C# compiler still requires the class to declare the members.
/// </summary>
internal static class MappedInterfaceStubFactory
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
    /// <param name="writer">The writer.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="instance">The (possibly substituted) generic instance signature for the interface, or null if non-generic.</param>
    /// <param name="ifaceName">The WinRT interface name (e.g. "IMap`2").</param>
    /// <param name="objRefName">The name of the lazy <c>_objRef_*</c> field for the interface on the class.</param>
    public static void WriteMappedInterfaceStubs(IndentedTextWriter writer, ProjectionEmitContext context, GenericInstanceTypeSignature? instance, string ifaceName, string objRefName)
    {
        // Resolve type arguments from the (substituted) generic instance signature, if any.
        List<TypeSemantics> typeArgs = [];
        List<TypeSignature> typeArgSigs = [];
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
                EmitDisposable(writer, objRefName);
                break;
            case "IIterable`1":
                EmitGenericEnumerable(writer, context, typeArgs, typeArgSigs, objRefName);
                break;
            case "IIterator`1":
                EmitGenericEnumerator(writer, context, typeArgs, typeArgSigs, objRefName);
                break;
            case "IMap`2":
                EmitDictionary(writer, context, typeArgs, typeArgSigs, objRefName);
                break;
            case "IMapView`2":
                EmitReadOnlyDictionary(writer, context, typeArgs, typeArgSigs, objRefName);
                break;
            case "IVector`1":
                EmitList(writer, context, typeArgs, typeArgSigs, objRefName);
                break;
            case "IVectorView`1":
                EmitReadOnlyList(writer, context, typeArgs, typeArgSigs, objRefName);
                break;
            case "IBindableIterable":
                writer.Write($"\nIEnumerator global::System.Collections.IEnumerable.GetEnumerator() => global::ABI.System.Collections.IEnumerableMethods.GetEnumerator({objRefName});\n");
                break;
            case "IBindableIterator":
                writer.Write($"\npublic bool MoveNext() => global::ABI.System.Collections.IEnumeratorMethods.MoveNext({objRefName});\n");
                writer.WriteLine("public void Reset() => throw new NotSupportedException();");
                writer.Write($"public object Current => global::ABI.System.Collections.IEnumeratorMethods.Current({objRefName});\n");
                break;
            case "IBindableVector":
                EmitNonGenericList(writer, objRefName);
                break;
            case "INotifyDataErrorInfo":
                writer.Write($"\npublic global::System.Collections.IEnumerable GetErrors(string propertyName) => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.GetErrors({objRefName}, propertyName);\n");
                writer.Write($"public bool HasErrors {{get => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.HasErrors({objRefName}); }}\n");
                writer.Write($"public event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs> ErrorsChanged\n{{\n    add => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.ErrorsChanged(this, {objRefName}).Subscribe(value);\n    remove => global::ABI.System.ComponentModel.INotifyDataErrorInfoMethods.ErrorsChanged(this, {objRefName}).Unsubscribe(value);\n}}\n");
                break;
        }
    }
    private static void EmitDisposable(IndentedTextWriter writer, string objRefName)
    {
        writer.WriteLine($"\npublic void Dispose() => global::ABI.System.IDisposableMethods.Dispose({objRefName});");
    }

    private static void EmitGenericEnumerable(IndentedTextWriter writer, ProjectionEmitContext context, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = WriteTypeNameToString(context, args[0], TypedefNameType.Projected, true);
        string elementId = EncodeArgIdentifier(context, args[0]);
        string interopTypeArgs = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IEnumerable'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IEnumerableMethods_" + elementId + "_";

        writer.WriteLine("");
        EmitUnsafeAccessor(writer, "GetEnumerator", $"IEnumerator<{t}>", $"{prefix}GetEnumerator", interopType, "");

        writer.WriteLine($"{$"\npublic IEnumerator<{t}> GetEnumerator() => {prefix}GetEnumerator(null, {objRefName});\n"}global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();");
    }

    private static void EmitGenericEnumerator(IndentedTextWriter writer, ProjectionEmitContext context, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = WriteTypeNameToString(context, args[0], TypedefNameType.Projected, true);
        string elementId = EncodeArgIdentifier(context, args[0]);
        string interopTypeArgs = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IEnumerator'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IEnumeratorMethods_" + elementId + "_";

        writer.WriteLine("");
        EmitUnsafeAccessor(writer, "Current", t, $"{prefix}Current", interopType, "");
        EmitUnsafeAccessor(writer, "MoveNext", "bool", $"{prefix}MoveNext", interopType, "");

        writer.Write($"\npublic bool MoveNext() => {prefix}MoveNext(null, {objRefName});\n");
        writer.Write($$"""
            public void Reset() => throw new NotSupportedException();
            public void Dispose() {}
            {{$"public {t} Current => {prefix}Current(null, {objRefName});\n"}}object global::System.Collections.IEnumerator.Current => Current!;
            """, isMultiline: true);
    }

    private static void EmitDictionary(IndentedTextWriter writer, ProjectionEmitContext context, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 2) { return; }
        string k = WriteTypeNameToString(context, args[0], TypedefNameType.Projected, true);
        string v = WriteTypeNameToString(context, args[1], TypedefNameType.Projected, true);
        // Truth uses two forms for KeyValuePair:
        // - 'kv' (unqualified) for plain type usages: parameters, field/return types
        // - 'kvNested' (fully qualified) for generic argument usages (inside IEnumerator<>, ICollection<>)
        string kv = $"KeyValuePair<{k}, {v}>";
        string kvNested = $"global::System.Collections.Generic.KeyValuePair<{k}, {v}>";
        // Long form (always fully qualified) used for objref field-name computation
        // (matches the form WriteClassObjRefDefinitions emits transitively).
        string kvLong = kvNested;
        string keyId = EncodeArgIdentifier(context, args[0]);
        string valId = EncodeArgIdentifier(context, args[1]);
        string keyInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string valInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[1], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IDictionary'2<" + keyInteropArg + "|" + valInteropArg + ">Methods, WinRT.Interop";
        string prefix = "IDictionaryMethods_" + keyId + "_" + valId + "_";
        // The IEnumerable<KeyValuePair<K,V>> objref name (matches what WriteClassObjRefDefinitions emits transitively).
        string enumerableObjRefName = "_objRef_System_Collections_Generic_IEnumerable_" + IIDExpressionWriter.EscapeTypeNameForIdentifier(kvLong, stripGlobal: false) + "_";

        writer.WriteLine("");
        EmitUnsafeAccessor(writer, "Keys", $"ICollection<{k}>", $"{prefix}Keys", interopType, "");
        EmitUnsafeAccessor(writer, "Values", $"ICollection<{v}>", $"{prefix}Values", interopType, "");
        EmitUnsafeAccessor(writer, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(writer, "Item", v, $"{prefix}Item", interopType, $", {k} key");
        EmitUnsafeAccessor(writer, "Item", "void", $"{prefix}Item", interopType, $", {k} key, {v} value");
        EmitUnsafeAccessor(writer, "Add", "void", $"{prefix}Add", interopType, $", {k} key, {v} value");
        EmitUnsafeAccessor(writer, "ContainsKey", "bool", $"{prefix}ContainsKey", interopType, $", {k} key");
        EmitUnsafeAccessor(writer, "Remove", "bool", $"{prefix}Remove", interopType, $", {k} key");
        EmitUnsafeAccessor(writer, "TryGetValue", "bool", $"{prefix}TryGetValue", interopType, $", {k} key, out {v} value");
        EmitUnsafeAccessor(writer, "Add", "void", $"{prefix}Add", interopType, $", {kv} item");
        EmitUnsafeAccessor(writer, "Clear", "void", $"{prefix}Clear", interopType, "");
        EmitUnsafeAccessor(writer, "Contains", "bool", $"{prefix}Contains", interopType, $", {kv} item");
        EmitUnsafeAccessor(writer, "CopyTo", "void", $"{prefix}CopyTo", interopType, $", WindowsRuntimeObjectReference enumObjRef, {kv}[] array, int arrayIndex");
        EmitUnsafeAccessor(writer, "Remove", "bool", $"{prefix}Remove", interopType, $", {kv} item");

        // Public member emission order matches the WinRT IMap<K,V> vtable order, NOT alphabetical.
        // GetEnumerator is NOT emitted here -- it's handled separately by IIterable<KVP>'s own
        // EmitGenericEnumerable invocation.
        writer.Write($$"""
            public ICollection<{{k}}> Keys => {{prefix}}Keys(null, {{objRefName}});
            public ICollection<{{v}}> Values => {{prefix}}Values(null, {{objRefName}});
            public int Count => {{prefix}}Count(null, {{objRefName}});
            public bool IsReadOnly => false;
            {{$"public {v} this[{k} key]\n{{\n    get => {prefix}Item(null, {objRefName}, key);\n    set => {prefix}Item(null, {objRefName}, key, value);\n}}\n"}}{{$"public void Add({k} key, {v} value) => {prefix}Add(null, {objRefName}, key, value);\n"}}{{$"public bool ContainsKey({k} key) => {prefix}ContainsKey(null, {objRefName}, key);\n"}}{{$"public bool Remove({k} key) => {prefix}Remove(null, {objRefName}, key);\n"}}{{$"public bool TryGetValue({k} key, out {v} value) => {prefix}TryGetValue(null, {objRefName}, key, out value);\n"}}{{$"public void Add({kv} item) => {prefix}Add(null, {objRefName}, item);\n"}}{{$"public void Clear() => {prefix}Clear(null, {objRefName});\n"}}{{$"public bool Contains({kv} item) => {prefix}Contains(null, {objRefName}, item);\n"}}{{$"public void CopyTo({kv}[] array, int arrayIndex) => {prefix}CopyTo(null, {objRefName}, {enumerableObjRefName}, array, arrayIndex);\n"}}{{$"bool ICollection<{kv}>.Remove({kv} item) => {prefix}Remove(null, {objRefName}, item);\n"}}
            """, isMultiline: true);
    }

    private static void EmitReadOnlyDictionary(IndentedTextWriter writer, ProjectionEmitContext context, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 2) { return; }
        string k = WriteTypeNameToString(context, args[0], TypedefNameType.Projected, true);
        string v = WriteTypeNameToString(context, args[1], TypedefNameType.Projected, true);
        string keyId = EncodeArgIdentifier(context, args[0]);
        string valId = EncodeArgIdentifier(context, args[1]);
        string keyInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string valInteropArg = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[1], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IReadOnlyDictionary'2<" + keyInteropArg + "|" + valInteropArg + ">Methods, WinRT.Interop";
        string prefix = "IReadOnlyDictionaryMethods_" + keyId + "_" + valId + "_";

        writer.WriteLine("");
        EmitUnsafeAccessor(writer, "Keys", $"ICollection<{k}>", $"{prefix}Keys", interopType, "");
        EmitUnsafeAccessor(writer, "Values", $"ICollection<{v}>", $"{prefix}Values", interopType, "");
        EmitUnsafeAccessor(writer, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(writer, "Item", v, $"{prefix}Item", interopType, $", {k} key");
        EmitUnsafeAccessor(writer, "ContainsKey", "bool", $"{prefix}ContainsKey", interopType, $", {k} key");
        EmitUnsafeAccessor(writer, "TryGetValue", "bool", $"{prefix}TryGetValue", interopType, $", {k} key, out {v} value");

        // GetEnumerator is NOT emitted here -- it's handled separately by IIterable<KVP>'s
        // EmitGenericEnumerable invocation.
        writer.Write($"{$"\npublic {v} this[{k} key] => {prefix}Item(null, {objRefName}, key);\n"}{$"public IEnumerable<{k}> Keys => {prefix}Keys(null, {objRefName});\n"}{$"public IEnumerable<{v}> Values => {prefix}Values(null, {objRefName});\n"}{$"public int Count => {prefix}Count(null, {objRefName});\n"}{$"public bool ContainsKey({k} key) => {prefix}ContainsKey(null, {objRefName}, key);\n"}{$"public bool TryGetValue({k} key, out {v} value) => {prefix}TryGetValue(null, {objRefName}, key, out value);\n"}");
    }

    private static void EmitReadOnlyList(IndentedTextWriter writer, ProjectionEmitContext context, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = WriteTypeNameToString(context, args[0], TypedefNameType.Projected, true);
        string elementId = EncodeArgIdentifier(context, args[0]);
        string interopTypeArgs = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IReadOnlyList'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IReadOnlyListMethods_" + elementId + "_";

        writer.WriteLine("");
        EmitUnsafeAccessor(writer, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(writer, "Item", t, $"{prefix}Item", interopType, ", int index");

        // GetEnumerator is NOT emitted here -- it's handled separately by IIterable<T>'s
        // EmitGenericEnumerable invocation.
        writer.Write($"\n[global::System.Runtime.CompilerServices.IndexerName(\"ReadOnlyListItem\")]\n{$"public {t} this[int index] => {prefix}Item(null, {objRefName}, index);\n"}{$"public int Count => {prefix}Count(null, {objRefName});\n"}");
    }

    /// <summary>
    /// Writes a projected type name to a scratch buffer and returns the string.
    /// </summary>
    private static string WriteTypeNameToString(ProjectionEmitContext context, TypeSemantics arg, TypedefNameType nameType, bool forceQualified)
    {
        IndentedTextWriter scratch = new();
        TypedefNameWriter.WriteTypeName(scratch, context, arg, nameType, forceQualified);
        return scratch.ToString();
    }

    /// <summary>
    /// Encodes a type semantics as a C# identifier-safe name. Uses the projected type name
    /// WITHOUT forcing namespace qualification, then strips 'global::' and replaces '.' with '_'.
    /// </summary>
    private static string EncodeArgIdentifier(ProjectionEmitContext context, TypeSemantics arg)
    {
        string projected = WriteTypeNameToString(context, arg, TypedefNameType.Projected, false);
        return IIDExpressionWriter.EscapeTypeNameForIdentifier(projected, stripGlobal: true);
    }

    private static void EmitList(IndentedTextWriter writer, ProjectionEmitContext context, List<TypeSemantics> args, List<TypeSignature> argSigs, string objRefName)
    {
        if (args.Count != 1) { return; }
        string t = WriteTypeNameToString(context, args[0], TypedefNameType.Projected, true);
        string elementId = EncodeArgIdentifier(context, args[0]);
        string interopTypeArgs = InteropTypeNameWriter.EncodeInteropTypeName(argSigs[0], TypedefNameType.Projected);
        string interopType = "ABI.System.Collections.Generic.<#corlib>IList'1<" + interopTypeArgs + ">Methods, WinRT.Interop";
        string prefix = "IListMethods_" + elementId + "_";

        writer.WriteLine("");
        EmitUnsafeAccessor(writer, "Count", "int", $"{prefix}Count", interopType, "");
        EmitUnsafeAccessor(writer, "Item", t, $"{prefix}Item", interopType, ", int index");
        EmitUnsafeAccessor(writer, "Item", "void", $"{prefix}Item", interopType, $", int index, {t} value");
        EmitUnsafeAccessor(writer, "IndexOf", "int", $"{prefix}IndexOf", interopType, $", {t} item");
        EmitUnsafeAccessor(writer, "Insert", "void", $"{prefix}Insert", interopType, $", int index, {t} item");
        EmitUnsafeAccessor(writer, "RemoveAt", "void", $"{prefix}RemoveAt", interopType, ", int index");
        EmitUnsafeAccessor(writer, "Add", "void", $"{prefix}Add", interopType, $", {t} item");
        EmitUnsafeAccessor(writer, "Clear", "void", $"{prefix}Clear", interopType, "");
        EmitUnsafeAccessor(writer, "Contains", "bool", $"{prefix}Contains", interopType, $", {t} item");
        EmitUnsafeAccessor(writer, "CopyTo", "void", $"{prefix}CopyTo", interopType, $", {t}[] array, int arrayIndex");
        EmitUnsafeAccessor(writer, "Remove", "bool", $"{prefix}Remove", interopType, $", {t} item");

        // Public member emission order matches the WinRT IVector<T> vtable order mapped to IList<T>,
        // NOT alphabetical. GetEnumerator is NOT emitted here -- it's handled separately by IIterable<T>'s
        // own EmitGenericEnumerable invocation.
        writer.Write($$"""
            public int Count => {{prefix}}Count(null, {{objRefName}});
            public bool IsReadOnly => false;
            
            [global::System.Runtime.CompilerServices.IndexerName("ListItem")]
            {{$"public {t} this[int index]\n{{\n    get => {prefix}Item(null, {objRefName}, index);\n    set => {prefix}Item(null, {objRefName}, index, value);\n}}\n"}}{{$"public int IndexOf({t} item) => {prefix}IndexOf(null, {objRefName}, item);\n"}}{{$"public void Insert(int index, {t} item) => {prefix}Insert(null, {objRefName}, index, item);\n"}}{{$"public void RemoveAt(int index) => {prefix}RemoveAt(null, {objRefName}, index);\n"}}{{$"public void Add({t} item) => {prefix}Add(null, {objRefName}, item);\n"}}{{$"public void Clear() => {prefix}Clear(null, {objRefName});\n"}}{{$"public bool Contains({t} item) => {prefix}Contains(null, {objRefName}, item);\n"}}{{$"public void CopyTo({t}[] array, int arrayIndex) => {prefix}CopyTo(null, {objRefName}, array, arrayIndex);\n"}}{{$"public bool Remove({t} item) => {prefix}Remove(null, {objRefName}, item);\n"}}
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits a single <c>[UnsafeAccessor]</c> static extern declaration that targets a method on a
    /// WinRT.Interop helper type. The function signature is built from the supplied parts.
    /// </summary>
    private static void EmitUnsafeAccessor(IndentedTextWriter writer, string accessName, string returnType, string functionName, string interopType, string extraParams)
    {
        writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"");
        writer.Write(accessName);
        writer.WriteLine("\")]");
        writer.Write($"static extern {returnType} {functionName}([UnsafeAccessorType(\"{interopType}\")] object _, WindowsRuntimeObjectReference objRef{extraParams});\n\n");
    }

    private static void EmitNonGenericList(IndentedTextWriter writer, string objRefName)
    {
        writer.WriteLine("");
        writer.WriteLine("[global::System.Runtime.CompilerServices.IndexerName(\"NonGenericListItem\")]");
        writer.Write($"public object this[int index]\n{{\n    get => global::ABI.System.Collections.IListMethods.Item({objRefName}, index);\n    set => global::ABI.System.Collections.IListMethods.Item({objRefName}, index, value);\n}}\n");
        writer.Write($$"""
            public int Count => global::ABI.System.Collections.IListMethods.Count({{objRefName}});
            public bool IsReadOnly => false;
            public bool IsFixedSize => false;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            {{$"public int Add(object value) => global::ABI.System.Collections.IListMethods.Add({objRefName}, value);\n"}}{{$"public void Clear() => global::ABI.System.Collections.IListMethods.Clear({objRefName});\n"}}{{$"public bool Contains(object value) => global::ABI.System.Collections.IListMethods.Contains({objRefName}, value);\n"}}{{$"public int IndexOf(object value) => global::ABI.System.Collections.IListMethods.IndexOf({objRefName}, value);\n"}}{{$"public void Insert(int index, object value) => global::ABI.System.Collections.IListMethods.Insert({objRefName}, index, value);\n"}}{{$"public void Remove(object value) => global::ABI.System.Collections.IListMethods.Remove({objRefName}, value);\n"}}{{$"public void RemoveAt(int index) => global::ABI.System.Collections.IListMethods.RemoveAt({objRefName}, index);\n"}}{{$"public void CopyTo(Array array, int index) => global::ABI.System.Collections.IListMethods.CopyTo({objRefName}, array, index);\n"}}
            """, isMultiline: true);
        // GetEnumerator is NOT emitted here -- it's handled separately by IBindableIterable's
        // EmitNonGenericEnumerable invocation.
    }
}