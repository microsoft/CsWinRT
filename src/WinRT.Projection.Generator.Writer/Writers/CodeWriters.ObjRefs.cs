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
    /// Mirrors C++ <c>write_objref_type_name</c>: takes the projected interface name, strips
    /// the <c>global::</c> prefix and replaces non-identifier characters with <c>_</c>.
    /// </summary>
    public static string GetObjRefName(TypeWriter w, ITypeDefOrRef ifaceType)
    {
        string projected = w.WriteTemp("%", new Action<TextWriter>(_ =>
        {
            // Use the same projection logic that the inheritance list uses: applies
            // mapped-type remapping (e.g. IClosable -> System.IDisposable).
            WriteInterfaceTypeName(w, ifaceType);
        }));
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
    /// Emits the lazy <c>_objRef_*</c> field definitions for each interface implementation on
    /// the given runtime class (mirrors C++ <c>write_class_objrefs_definition</c>).
    /// </summary>
    public static void WriteClassObjRefDefinitions(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.ReferenceProjection) { return; }

        // Track names emitted so we don't emit duplicates (e.g. when both IFoo and IFoo2
        // produce the same _objRef_<name>).
        HashSet<string> emitted = new(System.StringComparer.Ordinal);

        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            if (!IsInterfaceInInheritanceList(impl, includeExclusiveInterface: false)
                && !IsInterfaceForObjRef(impl))
            {
                continue;
            }

            string objRefName = GetObjRefName(w, impl.Interface);
            if (!emitted.Add(objRefName)) { continue; }

            bool isDefault = TypeCategorization.HasAttribute(impl, "Windows.Foundation.Metadata", "DefaultAttribute");
            if (isDefault)
            {
                // Default interface: simple expression-bodied property pointing at NativeObjectReference.
                w.Write("private WindowsRuntimeObjectReference ");
                w.Write(objRefName);
                w.Write(" => NativeObjectReference;\n");
            }
            else
            {
                // Non-default interface: lazy CompareExchange pattern.
                w.Write("private WindowsRuntimeObjectReference ");
                w.Write(objRefName);
                w.Write("\n{\n");
                w.Write("    get\n    {\n");
                w.Write("        [MethodImpl(MethodImplOptions.NoInlining)]\n");
                w.Write("        WindowsRuntimeObjectReference MakeObjectReference()\n        {\n");
                w.Write("            _ = global::System.Threading.Interlocked.CompareExchange(\n");
                w.Write("                location1: ref field,\n");
                w.Write("                value: NativeObjectReference.As(");
                WriteIidExpression(w, impl.Interface);
                w.Write("),\n");
                w.Write("                comparand: null);\n\n");
                w.Write("            return field;\n        }\n\n");
                w.Write("        return field ?? MakeObjectReference();\n    }\n}\n");
            }
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
