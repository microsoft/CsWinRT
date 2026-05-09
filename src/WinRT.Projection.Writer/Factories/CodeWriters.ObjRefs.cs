// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// ObjRef field emission for runtime classes (mirrors C++ <c>write_class_objrefs_definition</c>
/// and helpers around <c>write_objref_type_name</c> / <c>write_iid_guid</c>).
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Returns the field name for the given interface impl (e.g. <c>_objRef_System_IDisposable</c>).
    /// Mirrors C++ <c>write_objref_type_name</c>: takes the projected interface name (with the
    /// namespace forcibly included), strips the <c>global::</c> prefix and replaces
    /// non-identifier characters with <c>_</c>.
    /// </summary>
    public static string GetObjRefName(TypeWriter w, ITypeDefOrRef ifaceType)
    {
        // Build the projected, fully-qualified name with global::.
        string projected;
        if (ifaceType is TypeDefinition td)
        {
            (string ns, string name) = td.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            projected = "global::" + ns + "." + Helpers.StripBackticks(name);
        }
        else if (ifaceType is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            projected = "global::" + ns + "." + Helpers.StripBackticks(name);
        }
        else if (ifaceType is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            // Generic instantiation: always use fully qualified name (with global::) for the objref
            // name computation, so the resulting field name is unique across namespaces. This
            // matches truth output: e.g. _objRef_System_Collections_Generic_IReadOnlyList_global__Windows_Web_Http_HttpCookie_
            // (with 'global__' coming from escaping the global:: prefix).
            projected = w.WriteTemp("%", new Action<TextWriter>(_ =>
            {
                WriteFullyQualifiedInterfaceName(w, ifaceType);
            }));
        }
        else
        {
            projected = w.WriteTemp("%", new Action<TextWriter>(_ =>
            {
                WriteFullyQualifiedInterfaceName(w, ifaceType);
            }));
        }
        return "_objRef_" + EscapeTypeNameForIdentifier(projected, stripGlobal: true);
    }

    /// <summary>
    /// Like <see cref="WriteInterfaceTypeName"/> but always emits a fully qualified name with
    /// <c>global::</c> prefix on every type (even same-namespace ones). Used for objref name
    /// computation where uniqueness across namespaces matters.
    /// </summary>
    private static void WriteFullyQualifiedInterfaceName(TypeWriter w, ITypeDefOrRef ifaceType)
    {
        if (ifaceType is TypeDefinition td)
        {
            (string ns, string name) = td.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            w.Write("global::");
            if (!string.IsNullOrEmpty(ns)) { w.Write(ns); w.Write("."); }
            w.WriteCode(Helpers.StripBackticks(name));
        }
        else if (ifaceType is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            w.Write("global::");
            if (!string.IsNullOrEmpty(ns)) { w.Write(ns); w.Write("."); }
            w.WriteCode(Helpers.StripBackticks(name));
        }
        else if (ifaceType is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef gt = gi.GenericType;
            (string ns, string name) = gt.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            w.Write("global::");
            if (!string.IsNullOrEmpty(ns)) { w.Write(ns); w.Write("."); }
            w.WriteCode(Helpers.StripBackticks(name));
            w.Write("<");
            for (int i = 0; i < gi.TypeArguments.Count; i++)
            {
                if (i > 0) { w.Write(", "); }
                // forceWriteNamespace=true so generic args also get global:: prefix.
                WriteTypeName(w, TypeSemanticsFactory.Get(gi.TypeArguments[i]), TypedefNameType.Projected, true);
            }
            w.Write(">");
        }
    }

    /// <summary>
    /// Writes the IID expression for the given interface impl (used as the second arg to
    /// <c>NativeObjectReference.As(...)</c>). Mirrors C++ <c>write_iid_guid</c>.
    /// </summary>
    public static void WriteIidExpression(TypeWriter w, ITypeDefOrRef ifaceType)
    {
        // Generic instantiation: use the UnsafeAccessor extern method declared above the field
        // (e.g. IID_Windows_Foundation_Collections_IObservableMap_string__object_(null)).
        if (ifaceType is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            string propName = BuildIidPropertyNameForGenericInterface(w, gi);
            w.Write(propName);
            w.Write("(null)");
            return;
        }

        string ns;
        string name;
        bool isMapped;
        if (ifaceType is TypeDefinition td)
        {
            ns = td.Namespace?.Value ?? string.Empty;
            name = td.Name?.Value ?? string.Empty;
            isMapped = MappedTypes.Get(ns, name) is not null;
        }
        else if (ifaceType is TypeReference tr)
        {
            ns = tr.Namespace?.Value ?? string.Empty;
            name = tr.Name?.Value ?? string.Empty;
            isMapped = MappedTypes.Get(ns, name) is not null;
        }
        else
        {
            w.Write("default(global::System.Guid)");
            return;
        }

        if (isMapped)
        {
            // IStringable maps to a simpler IID name in WellKnownInterfaceIIDs.
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is { MappedName: "IStringable" })
            {
                w.Write("global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable");
                return;
            }
            // Mapped interface: use WellKnownInterfaceIIDs.IID_<EscapedNonProjectedName>.
            // The non-projected name is the original WinRT interface (e.g. "Windows.Foundation.IClosable").
            string id = EscapeIdentifier(ns + "." + Helpers.StripBackticks(name));
            w.Write("global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_");
            w.Write(id);
        }
        else
        {
            // Non-mapped, non-generic: ABI.InterfaceIIDs.IID_<EscapedABIName>.
            // Uses the "ABI." prefix on the namespace, escaped with stripGlobalABI.
            string abiQualified = "global::ABI." + ns + "." + Helpers.StripBackticks(name);
            string id = EscapeTypeNameForIdentifier(abiQualified, stripGlobal: false, stripGlobalABI: true);
            w.Write("global::ABI.InterfaceIIDs.IID_");
            w.Write(id);
        }
    }

    /// <summary>
    /// Builds the IID property name for a generic interface instantiation. Mirrors C++
    /// <c>write_iid_guid_property_name</c>: <c>write_type_name(type, ABI, true)</c> + escape.
    /// E.g. <c>IObservableMap&lt;string, object&gt;</c> -> <c>IID_Windows_Foundation_Collections_IObservableMap_string__object_</c>.
    /// </summary>
    private static string BuildIidPropertyNameForGenericInterface(TypeWriter w, GenericInstanceTypeSignature gi)
    {
        TypeSemantics sem = TypeSemanticsFactory.Get(gi);
        string name = w.WriteTemp("%", new System.Action<TextWriter>(_ =>
        {
            WriteTypeName(w, sem, TypedefNameType.ABI, forceWriteNamespace: true);
        }));
        return "IID_" + EscapeTypeNameForIdentifier(name, stripGlobal: true, stripGlobalABI: true);
    }

    /// <summary>
    /// Emits the [UnsafeAccessor] extern method declaration that exposes the IID for a generic
    /// interface instantiation. Mirrors C++ <c>write_unsafe_accessor_for_iid</c>.
    /// </summary>
    /// <param name="isInNullableContext">When <c>true</c>, the accessor's parameter type is
    /// <c>object?</c> (used inside <c>#nullable enable</c> regions); otherwise <c>object</c>.</param>
    private static void EmitUnsafeAccessorForIid(TypeWriter w, GenericInstanceTypeSignature gi, bool isInNullableContext = false)
    {
        string propName = BuildIidPropertyNameForGenericInterface(w, gi);
        string interopName = EncodeInteropTypeName(gi, TypedefNameType.InteropIID);
        w.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"get_IID_");
        w.Write(interopName);
        w.Write("\")]\n");
        w.Write("static extern ref readonly Guid ");
        w.Write(propName);
        w.Write("([UnsafeAccessorType(\"ABI.InterfaceIIDs, WinRT.Interop\")] object");
        if (isInNullableContext) { w.Write("?"); }
        w.Write(" _);\n");
    }

    private static string EscapeIdentifier(string s)
    {
        System.Text.StringBuilder sb = new(s.Length);
        foreach (char c in s)
        {
            sb.Append((c == ' ' || c == ':' || c == '<' || c == '>' || c == '`' || c == ',' || c == '.') ? '_' : c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Writes the IReference&lt;T&gt; IID expression for a value type (used by BoxToUnmanaged).
    /// Mirrors the C++ output: <c>global::ABI.InterfaceIIDs.IID_&lt;EscapedABIName&gt;Reference</c>.
    /// </summary>
    public static void WriteIidReferenceExpression(TypeWriter w, TypeDefinition type)
    {
        (string ns, string name) = type.Names();
        string abiQualified = "global::ABI." + ns + "." + Helpers.StripBackticks(name);
        string id = EscapeTypeNameForIdentifier(abiQualified, stripGlobal: false, stripGlobalABI: true);
        w.Write("global::ABI.InterfaceIIDs.IID_");
        w.Write(id);
        w.Write("Reference");
    }

    /// <summary>
    /// Emits the lazy <c>_objRef_*</c> field definitions for each interface implementation on
    /// the given runtime class (mirrors C++ <c>write_class_objrefs_definition</c>).
    /// The C++ uses <c>replaceDefaultByInner = type.Flags().Sealed()</c>: for sealed classes,
    /// the default interface is emitted as a simple <c>=> NativeObjectReference</c> expression;
    /// for unsealed classes, ALL interfaces (including the default) use the lazy
    /// MakeObjectReference pattern, and the default also gets an <c>init;</c> accessor so the
    /// constructor can set it via <c>_objRef_X = NativeObjectReference</c>.
    /// </summary>
    public static void WriteClassObjRefDefinitions(TypeWriter w, TypeDefinition type)
    {
        // Per-interface _objRef_* getters are emitted in BOTH impl and ref modes with full
        // bodies. C++ write_class_objrefs_definition has no settings.reference_projection
        // gate. Truth ref-mode output keeps the full Interlocked.CompareExchange +
        // NativeObjectReference.As(IID_X(null)) lazy-init bodies. (Only the static factory
        // _objRef_* getters become `throw null;` in ref mode — see WriteStaticFactoryObjRef
        // and WriteAttributedTypes.)

        // Track names emitted so we don't emit duplicates (e.g. when both IFoo and IFoo2
        // produce the same _objRef_<name>).
        HashSet<string> emitted = new(System.StringComparer.Ordinal);
        bool isSealed = type.IsSealed;

        // Pass 1: emit objrefs for ALL directly-declared interfaces first (in InterfaceImpl
        // declaration order). Pass 2 then walks transitive parents to cover any not yet emitted.
        // This mirrors C++ which emits all direct impls first before recursing — so for a class
        // that declares 'IFoo, IBar' where IFoo : IBaz, the order is IFoo, IBar, IBaz, NOT
        // IFoo, IBaz, IBar.
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            if (!IsInterfaceInInheritanceList(impl, includeExclusiveInterface: false)
                && !IsInterfaceForObjRef(impl))
            {
                continue;
            }

            // Mirrors C++ write_class_objrefs_definition (code_writers.h:2960): for fast-abi
            // classes, skip non-default exclusive interfaces — their methods dispatch through
            // the default interface's vtable so a separate objref is unnecessary.
            bool isDefault = impl.HasAttribute("Windows.Foundation.Metadata", "DefaultAttribute");
            if (!isDefault && IsFastAbiClass(type))
            {
                TypeDefinition? implTypeDef = ResolveInterfaceTypeDef(impl.Interface);
                if (implTypeDef is not null && TypeCategorization.IsExclusiveTo(implTypeDef))
                {
                    continue;
                }
            }

            EmitObjRefForInterface(w, impl.Interface, emitted, isDefault: isDefault, useSimplePattern: isSealed && isDefault);
        }
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            if (!IsInterfaceInInheritanceList(impl, includeExclusiveInterface: false)
                && !IsInterfaceForObjRef(impl))
            {
                continue;
            }
            // Same fast-abi guard as the first pass.
            bool isDefault2 = impl.HasAttribute("Windows.Foundation.Metadata", "DefaultAttribute");
            if (!isDefault2 && IsFastAbiClass(type))
            {
                TypeDefinition? implTypeDef = ResolveInterfaceTypeDef(impl.Interface);
                if (implTypeDef is not null && TypeCategorization.IsExclusiveTo(implTypeDef))
                {
                    continue;
                }
            }
            EmitTransitiveInterfaceObjRefs(w, impl.Interface, emitted);
        }
    }

    /// <summary>Emits an _objRef_ field for a single interface impl reference.</summary>
    /// <param name="useSimplePattern">When true, emit the simple expression-bodied form
    /// <c>=> NativeObjectReference</c>. Otherwise emit the lazy MakeObjectReference pattern.</param>
    private static void EmitObjRefForInterface(TypeWriter w, ITypeDefOrRef ifaceRef, HashSet<string> emitted, bool isDefault, bool useSimplePattern = false)
    {
        string objRefName = GetObjRefName(w, ifaceRef);
        if (!emitted.Add(objRefName)) { return; }

        // Mirrors C++ write_class_objrefs_definition: for generic interface instantiations, emit
        // the [UnsafeAccessor] extern method declaration (used by the IID expression in both
        // simple and lazy patterns).
        bool isGenericInstance = ifaceRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature;
        GenericInstanceTypeSignature? gi = isGenericInstance
            ? (GenericInstanceTypeSignature)((TypeSpecification)ifaceRef).Signature!
            : null;

        if (useSimplePattern)
        {
            // Sealed-class default interface: simple expression-bodied property pointing at NativeObjectReference.
            w.Write("private WindowsRuntimeObjectReference ");
            w.Write(objRefName);
            w.Write(" => NativeObjectReference;\n");
            // Emit the unsafe accessor AFTER the field so it can be used to pass the IID in the
            // constructor for the default interface (mirrors C++ ordering at line ~3018-3022).
            if (gi is not null)
            {
                EmitUnsafeAccessorForIid(w, gi);
            }
        }
        else
        {
            // Emit the unsafe accessor BEFORE the lazy field so it's referenced inside the As(...) call.
            if (gi is not null)
            {
                EmitUnsafeAccessorForIid(w, gi);
            }
            // Lazy CompareExchange pattern. For unsealed-class defaults, also emit 'init;' so the
            // constructor can assign NativeObjectReference for the exact-type case.
            w.Write("private WindowsRuntimeObjectReference ");
            w.Write(objRefName);
            w.Write("\n{\n");
            w.Write("    get\n    {\n");
            w.Write("        [MethodImpl(MethodImplOptions.NoInlining)]\n");
            w.Write("        WindowsRuntimeObjectReference MakeObjectReference()\n        {\n");
            w.Write("            _ = global::System.Threading.Interlocked.CompareExchange(\n");
            w.Write("                location1: ref field,\n");
            w.Write("                value: NativeObjectReference.As(");
            WriteIidExpression(w, ifaceRef);
            w.Write("),\n");
            w.Write("                comparand: null);\n\n");
            w.Write("            return field;\n        }\n\n");
            w.Write("        return field ?? MakeObjectReference();\n    }\n");
            if (isDefault) { w.Write("    init;\n"); }
            w.Write("}\n");
        }
    }

    /// <summary>
    /// Walks transitively-inherited interfaces and emits an objref field for each one. Mirrors
    /// the recursive interface walk needed for mapped collection dispatch.
    /// </summary>
    private static void EmitTransitiveInterfaceObjRefs(TypeWriter w, ITypeDefOrRef ifaceRef, HashSet<string> emitted)
    {
        // Resolve the interface to its TypeDefinition; if cross-module, look it up in the cache.
        TypeDefinition? ifaceTd = ResolveInterfaceTypeDef(ifaceRef);
        if (ifaceTd is null) { return; }

        // Compute a substitution context if the parent is a closed generic instance.
        AsmResolver.DotNet.Signatures.GenericContext? ctx = null;
        if (ifaceRef is TypeSpecification ts && ts.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
        {
            ctx = new AsmResolver.DotNet.Signatures.GenericContext(gi, null);
        }

        foreach (InterfaceImplementation childImpl in ifaceTd.Interfaces)
        {
            if (childImpl.Interface is null) { continue; }

            // If the parent is a closed generic, substitute the child's signature.
            ITypeDefOrRef childRef = childImpl.Interface;
            if (ctx is not null)
            {
                AsmResolver.DotNet.Signatures.TypeSignature childSig = childRef.ToTypeSignature(false);
                AsmResolver.DotNet.Signatures.TypeSignature substitutedSig = childSig.InstantiateGenericTypes(ctx.Value);
                AsmResolver.DotNet.ITypeDefOrRef? newRef = substitutedSig.ToTypeDefOrRef();
                if (newRef is not null) { childRef = newRef; }
            }

            // Skip exclusive-to-someone-else interfaces. Mirrors EmitImplType-like check.
            // For now, just emit (no-op if exclusive — the field still works for QI lookup).
            EmitObjRefForInterface(w, childRef, emitted, isDefault: false);
            EmitTransitiveInterfaceObjRefs(w, childRef, emitted);
        }
    }

    /// <summary>
    /// Whether this interface impl needs an _objRef_* field even though it isn't part of the
    /// inheritance list (e.g. ExclusiveTo interfaces still need their objref since instance
    /// methods/properties dispatch through it).
    /// </summary>
    public static bool IsInterfaceForObjRef(InterfaceImplementation impl)
    {
        // For now, emit objrefs for ALL implemented interfaces — instance member dispatch
        // needs to be able to reach them.
        return impl.Interface is not null;
    }
}
