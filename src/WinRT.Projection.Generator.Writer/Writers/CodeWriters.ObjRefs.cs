// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionGenerator.Writer;

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
            string ns = td.Namespace?.Value ?? string.Empty;
            string name = td.Name?.Value ?? string.Empty;
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
            string ns = tr.Namespace?.Value ?? string.Empty;
            string name = tr.Name?.Value ?? string.Empty;
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
            // Generic instantiation: full qualified name with type args (matches C++ projected name).
            projected = w.WriteTemp("%", new Action<TextWriter>(_ =>
            {
                WriteInterfaceTypeName(w, ifaceType);
            }));
        }
        else
        {
            projected = w.WriteTemp("%", new Action<TextWriter>(_ =>
            {
                WriteInterfaceTypeName(w, ifaceType);
            }));
        }
        return "_objRef_" + EscapeTypeNameForIdentifier(projected, stripGlobal: true);
    }

    /// <summary>
    /// Writes the IID expression for the given interface impl (used as the second arg to
    /// <c>NativeObjectReference.As(...)</c>). Mirrors C++ <c>write_iid_guid</c>.
    /// </summary>
    public static void WriteIidExpression(TypeWriter w, ITypeDefOrRef ifaceType)
    {
        // Generic instance: use UnsafeAccessor (we don't yet emit those; fall back to ABI.InterfaceIIDs)
        // For now, only handle TypeDef/TypeRef (non-generic) cases. Generic instances will be
        // wired up in a follow-up commit.
        if (ifaceType is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature)
        {
            // Generic instantiation: this requires an UnsafeAccessor delegate. For now, we
            // emit a placeholder that compiles (returns ref to a dummy), but ideally would be
            // the actual UnsafeAccessor. Will be fully ported in a follow-up commit.
            w.Write("default(global::System.Guid)");
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
        string ns = type.Namespace?.Value ?? string.Empty;
        string name = type.Name?.Value ?? string.Empty;
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
        if (w.Settings.ReferenceProjection) { return; }

        // Track names emitted so we don't emit duplicates (e.g. when both IFoo and IFoo2
        // produce the same _objRef_<name>).
        HashSet<string> emitted = new(System.StringComparer.Ordinal);
        bool isSealed = type.IsSealed;

        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            if (!IsInterfaceInInheritanceList(impl, includeExclusiveInterface: false)
                && !IsInterfaceForObjRef(impl))
            {
                continue;
            }

            bool isDefault = TypeCategorization.HasAttribute(impl, "Windows.Foundation.Metadata", "DefaultAttribute");
            EmitObjRefForInterface(w, impl.Interface, emitted, isDefault: isDefault, useSimplePattern: isSealed && isDefault);

            // Walk transitively-inherited interfaces and emit objrefs for them too. This is needed
            // because mapped collection stubs (IList<T>, IDictionary<K,V>) need the _objRef field
            // for IEnumerable<T>/IEnumerable<KeyValuePair<K,V>> to dispatch GetEnumerator through.
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

        if (useSimplePattern)
        {
            // Sealed-class default interface: simple expression-bodied property pointing at NativeObjectReference.
            w.Write("private WindowsRuntimeObjectReference ");
            w.Write(objRefName);
            w.Write(" => NativeObjectReference;\n");
        }
        else
        {
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
