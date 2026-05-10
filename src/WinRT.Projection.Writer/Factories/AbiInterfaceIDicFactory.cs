// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the IDynamicInterfaceCastable shim implementations for projected interface types.
/// Handles required (inherited) interfaces and the special collection forwarders for
/// IObservableMap, IObservableVector, and BCL-mapped types like IBindableVector.
/// </summary>
internal static class AbiInterfaceIDicFactory
{
    public static void WriteInterfaceIdicImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (TypeCategorization.IsExclusiveTo(type) && !context.Settings.IdicExclusiveTo) { return; }
        if (type.GenericParameters.Count > 0) { return; }
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);

        writer.WriteLine("");
        writer.WriteLine("[DynamicInterfaceCastableImplementation]");
        InterfaceFactory.WriteGuidAttribute(writer, type);
        writer.WriteLine("");
        writer.Write($"file interface {nameStripped} : ");
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        TypedefNameWriter.WriteTypeParams(writer, type);
        writer.WriteLine("");
        using (writer.WriteBlock())
        {
            // Emit DIM bodies that dispatch through the static ABI Methods class.
            WriteInterfaceIdicImplMembers(writer, context, type);
            writer.WriteLine("");
        }
    }

    /// <summary>
    /// Emits explicit-interface DIM (default interface method) implementations for the IDIC
    /// file interface.
    /// </summary>
    internal static void WriteInterfaceIdicImplMembers(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        HashSet<TypeDefinition> visited = [];
        WriteInterfaceIdicImplMembersForInterface(writer, context, type);

        // Also walk required (inherited) interfaces and emit members for each one.
        WriteInterfaceIdicImplMembersForRequiredInterfaces(writer, context, type, visited);
    }

    internal static void WriteInterfaceIdicImplMembersForRequiredInterfaces(
        IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, HashSet<TypeDefinition> visited)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            TypeDefinition? required = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl.Interface);
            if (required is null) { continue; }
            if (!visited.Add(required)) { continue; }
            (string rNs, string rName) = required.Names();
            MappedType? mapped = MappedTypes.Get(rNs, rName);
            if (mapped is not null && mapped.HasCustomMembersOutput)
            {
                // Mapped to a BCL interface (IBindableVector -> IList, IBindableIterable -> IEnumerable, etc.).
                // Emit explicit-interface DIM forwarders for the BCL members so the DIC shim
                // satisfies them when queried via casts like '((IList)(WindowsRuntimeObject)this)'.
                EmitDicShimMappedBclForwarders(writer, context, rName);
                // IBindableVector's IList forwarders already include the IEnumerable.GetEnumerator
                // forwarder (since IList : IEnumerable). Pre-add IBindableIterable to the visited
                // set so we don't emit a second GetEnumerator forwarder for it. We also walk the
                // required interfaces so any other (deeper) inherited mapped interface is covered.
                if (rName == "IBindableVector")
                {
                    foreach (InterfaceImplementation impl2 in required.Interfaces)
                    {
                        if (impl2.Interface is null) { continue; }
                        TypeDefinition? r2 = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl2.Interface);
                        if (r2 is not null) { _ = visited.Add(r2); }
                    }
                }
                continue;
            }
            // Special case: IObservableMap`2 and IObservableVector`1 are NOT mapped to BCL
            // interfaces (they retain WinRT names) but they DO need to forward their inherited
            // IDictionary/IList members for cast-based dispatch.
            if (rNs == "Windows.Foundation.Collections" && rName == "IObservableMap`2")
            {
                if (impl.Interface is TypeSpecification tsMap && tsMap.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature giMap && giMap.TypeArguments.Count == 2)
                {
                    IndentedTextWriter __scratchKeyText = new();
                    TypedefNameWriter.WriteTypeName(__scratchKeyText, context, TypeSemanticsFactory.Get(giMap.TypeArguments[0]), TypedefNameType.Projected, true);
                    string keyText = __scratchKeyText.ToString();
                    IndentedTextWriter __scratchValueText = new();
                    TypedefNameWriter.WriteTypeName(__scratchValueText, context, TypeSemanticsFactory.Get(giMap.TypeArguments[1]), TypedefNameType.Projected, true);
                    string valueText = __scratchValueText.ToString();
                    EmitDicShimIObservableMapForwarders(writer, context, keyText, valueText);
                    // Mark the inherited IMap`2 / IIterable`1 as visited so they aren't re-emitted.
                    foreach (InterfaceImplementation impl2 in required.Interfaces)
                    {
                        if (impl2.Interface is null) { continue; }
                        TypeDefinition? r2 = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl2.Interface);
                        if (r2 is not null) { _ = visited.Add(r2); }
                    }
                }
                continue;
            }
            if (rNs == "Windows.Foundation.Collections" && rName == "IObservableVector`1")
            {
                if (impl.Interface is TypeSpecification tsVec && tsVec.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature giVec && giVec.TypeArguments.Count == 1)
                {
                    IndentedTextWriter __scratchElementText = new();
                    TypedefNameWriter.WriteTypeName(__scratchElementText, context, TypeSemanticsFactory.Get(giVec.TypeArguments[0]), TypedefNameType.Projected, true);
                    string elementText = __scratchElementText.ToString();
                    EmitDicShimIObservableVectorForwarders(writer, context, elementText);
                    foreach (InterfaceImplementation impl2 in required.Interfaces)
                    {
                        if (impl2.Interface is null) { continue; }
                        TypeDefinition? r2 = AbiTypeHelpers.ResolveInterfaceTypeDef(context.Cache, impl2.Interface);
                        if (r2 is not null) { _ = visited.Add(r2); }
                    }
                }
                continue;
            }
            // Skip generic interfaces with unbound params (we can't substitute T at this layer).
            if (required.GenericParameters.Count > 0) { continue; }
            // Recurse first so deepest-base is emitted before nearer-base (matches deduplication).
            WriteInterfaceIdicImplMembersForRequiredInterfaces(writer, context, required, visited);
            WriteInterfaceIdicImplMembersForInheritedInterface(writer, context, required);
        }
    }

    /// <summary>
    /// Emits IDictionary&lt;K,V&gt; / ICollection&lt;KVP&gt; / IEnumerable&lt;KVP&gt; +
    /// IObservableMap&lt;K,V&gt;.MapChanged forwarders for a DIC file interface that inherits
    /// from <c>Windows.Foundation.Collections.IObservableMap&lt;K,V&gt;</c>.
    /// <c>write_dictionary_members_using_idic(true)</c> + the IObservableMap event forwarder.
    /// </summary>
    internal static void EmitDicShimIObservableMapForwarders(IndentedTextWriter writer, ProjectionEmitContext context, string keyText, string valueText)
    {
        string target = $"((global::System.Collections.Generic.IDictionary<{keyText}, {valueText}>)(WindowsRuntimeObject)this)";
        string self = $"global::System.Collections.Generic.IDictionary<{keyText}, {valueText}>.";
        string icoll = $"global::System.Collections.Generic.ICollection<global::System.Collections.Generic.KeyValuePair<{keyText}, {valueText}>>.";
        writer.WriteLine("");
        writer.Write($$"""
            ICollection<{{keyText}}> {{self}}Keys => {{target}}.Keys;
            ICollection<{{valueText}}> {{self}}Values => {{target}}.Values;
            int {{icoll}}Count => {{target}}.Count;
            bool {{icoll}}IsReadOnly => {{target}}.IsReadOnly;
            {{valueText}} {{self}}this[{{keyText}} key] 
            {
            get => {{target}}[key];
            set => {{target}}[key] = value;
            }
            void {{self}}Add({{keyText}} key, {{valueText}} value) => {{target}}.Add(key, value);
            bool {{self}}ContainsKey({{keyText}} key) => {{target}}.ContainsKey(key);
            bool {{self}}Remove({{keyText}} key) => {{target}}.Remove(key);
            bool {{self}}TryGetValue({{keyText}} key, out {{valueText}} value) => {{target}}.TryGetValue(key, out value);
            void {{icoll}}Add(KeyValuePair<{{keyText}}, {{valueText}}> item) => {{target}}.Add(item);
            void {{icoll}}Clear() => {{target}}.Clear();
            bool {{icoll}}Contains(KeyValuePair<{{keyText}}, {{valueText}}> item) => {{target}}.Contains(item);
            void {{icoll}}CopyTo(KeyValuePair<{{keyText}}, {{valueText}}>[] array, int arrayIndex) => {{target}}.CopyTo(array, arrayIndex);
            bool ICollection<KeyValuePair<{{keyText}}, {{valueText}}>>.Remove(KeyValuePair<{{keyText}}, {{valueText}}> item) => {{target}}.Remove(item);
            IEnumerator<KeyValuePair<{{keyText}}, {{valueText}}>> IEnumerable<KeyValuePair<{{keyText}}, {{valueText}}>>.GetEnumerator() => {{target}}.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            """, isMultiline: true);
        // IObservableMap.MapChanged event forwarder.
        string obsTarget = $"((global::Windows.Foundation.Collections.IObservableMap<{keyText}, {valueText}>)(WindowsRuntimeObject)this)";
        string obsSelf = $"global::Windows.Foundation.Collections.IObservableMap<{keyText}, {valueText}>.";
        writer.WriteLine("");
        writer.Write($$"""
            event global::Windows.Foundation.Collections.MapChangedEventHandler<{{keyText}}, {{valueText}}> {{obsSelf}}MapChanged
            {
            add => {{obsTarget}}.MapChanged += value;
            remove => {{obsTarget}}.MapChanged -= value;
            }
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits IList&lt;T&gt; / ICollection&lt;T&gt; / IEnumerable&lt;T&gt; +
    /// IObservableVector&lt;T&gt;.VectorChanged forwarders for a DIC file interface that inherits
    /// from <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c>.
    /// <c>write_list_members_using_idic(true)</c> + the IObservableVector event forwarder.
    /// </summary>
    internal static void EmitDicShimIObservableVectorForwarders(IndentedTextWriter writer, ProjectionEmitContext context, string elementText)
    {
        string target = $"((global::System.Collections.Generic.IList<{elementText}>)(WindowsRuntimeObject)this)";
        string self = $"global::System.Collections.Generic.IList<{elementText}>.";
        string icoll = $"global::System.Collections.Generic.ICollection<{elementText}>.";
        writer.WriteLine("");
        writer.Write($$"""
            int {{icoll}}Count => {{target}}.Count;
            bool {{icoll}}IsReadOnly => {{target}}.IsReadOnly;
            {{elementText}} {{self}}this[int index]
            {
            get => {{target}}[index];
            set => {{target}}[index] = value;
            }
            int {{self}}IndexOf({{elementText}} item) => {{target}}.IndexOf(item);
            void {{self}}Insert(int index, {{elementText}} item) => {{target}}.Insert(index, item);
            void {{self}}RemoveAt(int index) => {{target}}.RemoveAt(index);
            void {{icoll}}Add({{elementText}} item) => {{target}}.Add(item);
            void {{icoll}}Clear() => {{target}}.Clear();
            bool {{icoll}}Contains({{elementText}} item) => {{target}}.Contains(item);
            void {{icoll}}CopyTo({{elementText}}[] array, int arrayIndex) => {{target}}.CopyTo(array, arrayIndex);
            bool {{icoll}}Remove({{elementText}} item) => {{target}}.Remove(item);
            IEnumerator<{{elementText}}> IEnumerable<{{elementText}}>.GetEnumerator() => {{target}}.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            """, isMultiline: true);
        // IObservableVector.VectorChanged event forwarder.
        string obsTarget = $"((global::Windows.Foundation.Collections.IObservableVector<{elementText}>)(WindowsRuntimeObject)this)";
        string obsSelf = $"global::Windows.Foundation.Collections.IObservableVector<{elementText}>.";
        writer.WriteLine("");
        writer.Write($$"""
            event global::Windows.Foundation.Collections.VectorChangedEventHandler<{{elementText}}> {{obsSelf}}VectorChanged
            {
            add => {{obsTarget}}.VectorChanged += value;
            remove => {{obsTarget}}.VectorChanged -= value;
            }
            """, isMultiline: true);
    }

    /// <summary>
    /// Emits explicit-interface DIM thunks for an *inherited* (required) interface on a DIC
    /// <c>file interface</c> shim. Each member becomes a thin
    /// <c>=&gt; ((IParent)(WindowsRuntimeObject)this).Member</c> delegating thunk so that DIC
    /// re-dispatches through the parent's own DIC shim.
    /// </summary>
    internal static void WriteInterfaceIdicImplMembersForInheritedInterface(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // The CCW interface name (the projected interface name with global:: prefix). For the
        // delegating thunks we cast through this same projected interface type.
        IndentedTextWriter __scratchCcwIfaceName = new();
        TypedefNameWriter.WriteTypedefName(__scratchCcwIfaceName, context, type, TypedefNameType.Projected, true);
        string ccwIfaceName = __scratchCcwIfaceName.ToString();
        if (!ccwIfaceName.StartsWith("global::", System.StringComparison.Ordinal)) { ccwIfaceName = "global::" + ccwIfaceName; }

        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsSpecial()) { continue; }
            MethodSignatureInfo sig = new(method);
            string mname = method.Name?.Value ?? string.Empty;

            writer.WriteLine("");
            MethodFactory.WriteProjectionReturnType(writer, context, sig);
            writer.Write($" {ccwIfaceName}.{mname}(");
            MethodFactory.WriteParameterList(writer, context, sig);
            writer.Write($") => (({ccwIfaceName})(WindowsRuntimeObject)this).{mname}(");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (i > 0) { writer.Write(", "); }
                ClassMembersFactory.WriteParameterNameWithModifier(writer, context, sig.Params[i]);
            }
            writer.WriteLine(");");
        }

        foreach (PropertyDefinition prop in type.Properties)
        {
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
            string pname = prop.Name?.Value ?? string.Empty;
            string propType = InterfaceFactory.WritePropType(context, prop);

            writer.WriteLine("");
            writer.Write($"{propType} {ccwIfaceName}.{pname}");
            if (getter is not null && setter is null)
            {
                // Read-only: single-line expression body.
                writer.WriteLine($" => (({ccwIfaceName})(WindowsRuntimeObject)this).{pname};");
            }
            else
            {
                writer.WriteLine("");
                using (writer.WriteBlock())
                {
                    if (getter is not null)
                    {
                        writer.WriteLine($"get => (({ccwIfaceName})(WindowsRuntimeObject)this).{pname};");
                    }
                    if (setter is not null)
                    {
                        writer.WriteLine($"set => (({ccwIfaceName})(WindowsRuntimeObject)this).{pname} = value;");
                    }
                }
            }
        }

        foreach (EventDefinition evt in type.Events)
        {
            string evtName = evt.Name?.Value ?? string.Empty;
            writer.WriteLine("");
            writer.Write("event ");
            TypedefNameWriter.WriteEventType(writer, context, evt);
            writer.Write($$"""
                 {{ccwIfaceName}}.{{evtName}}
                {
                    add => (({{ccwIfaceName}})(WindowsRuntimeObject)this).{{evtName}} += value;
                    remove => (({{ccwIfaceName}})(WindowsRuntimeObject)this).{{evtName}} -= value;
                }
                """, isMultiline: true);
        }
    }

    /// <summary>
    /// Emits explicit-interface DIM forwarders on a DIC <c>file interface</c> shim for the BCL
    /// members that come from a system-collection-mapped required WinRT interface
    /// (e.g. <c>IBindableVector</c> maps to <c>IList</c>, so we must satisfy <c>IList</c>,
    /// <c>ICollection</c>, and <c>IEnumerable</c> members on the shim). The forwarders all
    /// re-cast through <c>(WindowsRuntimeObject)this</c> so the DIC machinery can re-dispatch
    /// to the real BCL adapter shim.
    /// </summary>
    internal static void EmitDicShimMappedBclForwarders(IndentedTextWriter writer, ProjectionEmitContext context, string mappedWinRTInterfaceName)
    {
        switch (mappedWinRTInterfaceName)
        {
            case "IClosable":
                // IClosable maps to IDisposable. Forward Dispose() to the
                // WindowsRuntimeObject base which has the actual implementation.
                writer.WriteLine("");
                writer.WriteLine("void global::System.IDisposable.Dispose() => ((global::System.IDisposable)(WindowsRuntimeObject)this).Dispose();");
                break;
            case "IBindableVector":
                // IList covers IList, ICollection, and IEnumerable members.
                writer.WriteLine("");
                writer.Write("""
                    int global::System.Collections.ICollection.Count => ((global::System.Collections.IList)(WindowsRuntimeObject)this).Count;
                    bool global::System.Collections.ICollection.IsSynchronized => ((global::System.Collections.IList)(WindowsRuntimeObject)this).IsSynchronized;
                    object global::System.Collections.ICollection.SyncRoot => ((global::System.Collections.IList)(WindowsRuntimeObject)this).SyncRoot;
                    void global::System.Collections.ICollection.CopyTo(Array array, int index) => ((global::System.Collections.IList)(WindowsRuntimeObject)this).CopyTo(array, index);
                    
                    object global::System.Collections.IList.this[int index]
                    {
                    get => ((global::System.Collections.IList)(WindowsRuntimeObject)this)[index];
                    set => ((global::System.Collections.IList)(WindowsRuntimeObject)this)[index] = value;
                    }
                    bool global::System.Collections.IList.IsFixedSize => ((global::System.Collections.IList)(WindowsRuntimeObject)this).IsFixedSize;
                    bool global::System.Collections.IList.IsReadOnly => ((global::System.Collections.IList)(WindowsRuntimeObject)this).IsReadOnly;
                    int global::System.Collections.IList.Add(object value) => ((global::System.Collections.IList)(WindowsRuntimeObject)this).Add(value);
                    void global::System.Collections.IList.Clear() => ((global::System.Collections.IList)(WindowsRuntimeObject)this).Clear();
                    bool global::System.Collections.IList.Contains(object value) => ((global::System.Collections.IList)(WindowsRuntimeObject)this).Contains(value);
                    int global::System.Collections.IList.IndexOf(object value) => ((global::System.Collections.IList)(WindowsRuntimeObject)this).IndexOf(value);
                    void global::System.Collections.IList.Insert(int index, object value) => ((global::System.Collections.IList)(WindowsRuntimeObject)this).Insert(index, value);
                    void global::System.Collections.IList.Remove(object value) => ((global::System.Collections.IList)(WindowsRuntimeObject)this).Remove(value);
                    void global::System.Collections.IList.RemoveAt(int index) => ((global::System.Collections.IList)(WindowsRuntimeObject)this).RemoveAt(index);
                    
                    IEnumerator IEnumerable.GetEnumerator() => ((global::System.Collections.IList)(WindowsRuntimeObject)this).GetEnumerator();
                    """, isMultiline: true);
                break;
            case "IBindableIterable":
                writer.WriteLine("");
                writer.WriteLine("IEnumerator IEnumerable.GetEnumerator() => ((global::System.Collections.IEnumerable)(WindowsRuntimeObject)this).GetEnumerator();");
                break;
        }
    }

    internal static void WriteInterfaceIdicImplMembersForInterface(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // The CCW interface name (the projected interface name with global:: prefix).
        IndentedTextWriter __scratchCcwIfaceName = new();
        TypedefNameWriter.WriteTypedefName(__scratchCcwIfaceName, context, type, TypedefNameType.Projected, true);
        string ccwIfaceName = __scratchCcwIfaceName.ToString();
        if (!ccwIfaceName.StartsWith("global::", System.StringComparison.Ordinal)) { ccwIfaceName = "global::" + ccwIfaceName; }
        // The static ABI Methods class name.
        IndentedTextWriter __scratchAbiClass = new();
        TypedefNameWriter.WriteTypedefName(__scratchAbiClass, context, type, TypedefNameType.StaticAbiClass, true);
        string abiClass = __scratchAbiClass.ToString();
        if (!abiClass.StartsWith("global::", System.StringComparison.Ordinal)) { abiClass = "global::" + abiClass; }

        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsSpecial()) { continue; }
            MethodSignatureInfo sig = new(method);
            string mname = method.Name?.Value ?? string.Empty;

            writer.WriteLine("");
            writer.Write("unsafe ");
            MethodFactory.WriteProjectionReturnType(writer, context, sig);
            writer.Write($" {ccwIfaceName}.{mname}(");
            MethodFactory.WriteParameterList(writer, context, sig);
            writer.Write($$"""
                )
                {
                    var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof({{ccwIfaceName}}).TypeHandle);
                    
                """, isMultiline: true);
            if (sig.ReturnType is not null) { writer.Write("return "); }
            writer.Write($"{abiClass}.{mname}(_obj");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                writer.Write(", ");
                ClassMembersFactory.WriteParameterNameWithModifier(writer, context, sig.Params[i]);
            }
            writer.Write("""
                );
                }
                """, isMultiline: true);
        }

        foreach (PropertyDefinition prop in type.Properties)
        {
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
            string pname = prop.Name?.Value ?? string.Empty;
            string propType = InterfaceFactory.WritePropType(context, prop);

            writer.WriteLine("");
            writer.Write($$"""
                unsafe {{propType}} {{ccwIfaceName}}.{{pname}}
                {
                """, isMultiline: true);
            if (getter is not null)
            {
                writer.Write($$"""
                        get
                        {
                            var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof({{ccwIfaceName}}).TypeHandle);
                            return {{abiClass}}.{{pname}}(_obj);
                        }
                    """, isMultiline: true);
            }
            if (setter is not null)
            {
                // If the property has only a setter on this interface BUT a base interface declares
                // the getter (so the C# interface decl emits 'get; set;'), C# requires an explicit
                // interface impl to provide both accessors. Emit a synthetic getter that delegates
                // to the base interface where the getter actually lives
                if (getter is null)
                {
                    TypeDefinition? baseIfaceWithGetter = InterfaceFactory.FindPropertyInterfaceInBases(context.Cache, type, pname);
                    if (baseIfaceWithGetter is not null)
                    {
                        writer.Write("    get { return ((");
                        ClassMembersFactory.WriteInterfaceTypeNameForCcw(writer, context, baseIfaceWithGetter);
                        writer.WriteLine($")(WindowsRuntimeObject)this).{pname}; }}");
                    }
                }
                writer.Write($$"""
                        set
                        {
                            var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof({{ccwIfaceName}}).TypeHandle);
                            {{abiClass}}.{{pname}}(_obj, value);
                        }
                    """, isMultiline: true);
            }
            writer.WriteLine("}");
        }

        // Events: emit explicit interface event implementations on the IDIC interface that
        // dispatch through the static ABI Methods class's event accessor (returns an EventSource).
        foreach (EventDefinition evt in type.Events)
        {
            string evtName = evt.Name?.Value ?? string.Empty;
            writer.WriteLine("");
            writer.Write("event ");
            TypedefNameWriter.WriteEventType(writer, context, evt);
            writer.Write($$"""
                 {{ccwIfaceName}}.{{evtName}}
                {
                    add
                    {
                        var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof({{ccwIfaceName}}).TypeHandle);
                        {{abiClass}}.{{evtName}}((WindowsRuntimeObject)this, _obj).Subscribe(value);
                    }
                    remove
                    {
                        var _obj = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof({{ccwIfaceName}}).TypeHandle);
                        {{abiClass}}.{{evtName}}((WindowsRuntimeObject)this, _obj).Unsubscribe(value);
                    }
                }
                """, isMultiline: true);
        }
    }

}