// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Interface, class, and ABI emission helpers.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>write_guid_attribute</c>.</summary>
    public static void WriteGuidAttribute(TypeWriter w, TypeDefinition type)
    {
        bool fullyQualify = type.Namespace == "Windows.Foundation.Metadata";
        w.Write("[");
        w.Write(fullyQualify ? "global::System.Runtime.InteropServices.Guid" : "Guid");
        w.Write("(\"");
        WriteGuid(w, type, false);
        w.Write("\")]");
    }

    /// <summary>Mirrors C++ <c>write_type_inheritance</c> (object base case).</summary>
    public static void WriteTypeInheritance(TypeWriter w, TypeDefinition type, bool includeExclusiveInterface, bool includeWindowsRuntimeObject)
    {
        string delimiter = " : ";

        // Check the base type. If the class extends another runtime class (not System.Object,
        // not WindowsRuntime.WindowsRuntimeObject), emit the projected base type name.
        bool hasNonObjectBase = false;
        if (type.BaseType is not null)
        {
            string? baseNs = type.BaseType.Namespace?.Value;
            string? baseName = type.BaseType.Name?.Value;
            bool isObject = (baseNs == "System" && baseName == "Object")
                || (baseNs == "WindowsRuntime" && baseName == "WindowsRuntimeObject");
            hasNonObjectBase = !isObject;
        }

        if (hasNonObjectBase)
        {
            w.Write(delimiter);
            // Write the projected base type name (e.g., 'global::Windows.UI.Composition.CompositionObject').
            ITypeDefOrRef baseType = type.BaseType!;
            string ns = baseType.Namespace?.Value ?? string.Empty;
            string name = baseType.Name?.Value ?? string.Empty;
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            w.Write("global::");
            if (!string.IsNullOrEmpty(ns)) { w.Write(ns); w.Write("."); }
            w.Write(Helpers.StripBackticks(name));
            delimiter = ", ";
        }
        else if (includeWindowsRuntimeObject)
        {
            w.Write(delimiter);
            w.Write("WindowsRuntimeObject");
            delimiter = ", ";
        }

        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }

            bool isOverridable = Helpers.IsOverridable(impl);

            // For TypeDef interfaces, check exclusive_to attribute to decide inclusion.
            // For TypeRef interfaces, attempt to resolve via the runtime context.
            bool isExclusive = false;
            if (impl.Interface is TypeDefinition ifaceTypeDef)
            {
                isExclusive = TypeCategorization.IsExclusiveTo(ifaceTypeDef);
            }
            else
            {
                TypeDefinition? resolved = ResolveInterface(impl.Interface);
                if (resolved is not null)
                {
                    isExclusive = TypeCategorization.IsExclusiveTo(resolved);
                }
            }

            if (!(isOverridable || !isExclusive || includeExclusiveInterface))
            {
                continue;
            }

            w.Write(delimiter);
            delimiter = ", ";

            // Emit the interface name (CCW) with mapped-type remapping
            WriteInterfaceTypeName(w, impl.Interface);

            if (includeWindowsRuntimeObject && !w.Settings.ReferenceProjection)
            {
                w.Write(", IWindowsRuntimeInterface<");
                WriteInterfaceTypeName(w, impl.Interface);
                w.Write(">");
            }
        }
    }

    /// <summary>
    /// Writes the projected name for an interface reference (TypeDefinition, TypeReference, or
    /// generic instance), applying mapped-type remapping (e.g.,
    /// <c>Windows.Foundation.Collections.IMap&lt;K,V&gt;</c> → <c>System.Collections.Generic.IDictionary&lt;K,V&gt;</c>).
    /// </summary>
    public static void WriteInterfaceTypeName(TypeWriter w, ITypeDefOrRef ifaceType)
    {
        if (ifaceType is TypeDefinition td)
        {
            WriteTypedefName(w, td, TypedefNameType.CCW, false);
            WriteTypeParams(w, td);
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
            // Only emit the global:: prefix if the namespace doesn't match the current emit namespace
            // (mirrors WriteTypedefName behavior — same-namespace types stay unqualified).
            if (!string.IsNullOrEmpty(ns) && ns != w.CurrentNamespace)
            {
                w.Write("global::");
                w.Write(ns);
                w.Write(".");
            }
            w.WriteCode(Helpers.StripBackticks(name));
        }
        else if (ifaceType is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef gt = gi.GenericType;
            string ns = gt.Namespace?.Value ?? string.Empty;
            string name = gt.Name?.Value ?? string.Empty;
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            if (!string.IsNullOrEmpty(ns) && ns != w.CurrentNamespace)
            {
                w.Write("global::");
                w.Write(ns);
                w.Write(".");
            }
            w.WriteCode(Helpers.StripBackticks(name));
            w.Write("<");
            for (int i = 0; i < gi.TypeArguments.Count; i++)
            {
                if (i > 0) { w.Write(", "); }
                // Pass forceWriteNamespace=false so type args also respect the current namespace.
                WriteTypeName(w, TypeSemanticsFactory.Get(gi.TypeArguments[i]), TypedefNameType.Projected, false);
            }
            w.Write(">");
        }
    }

    /// <summary>Mirrors C++ <c>write_prop_type</c>.</summary>
    public static string WritePropType(TypeWriter w, PropertyDefinition prop, bool isSetProperty = false)
    {
        return WritePropType(w, prop, null, isSetProperty);
    }

    public static string WritePropType(TypeWriter w, PropertyDefinition prop, AsmResolver.DotNet.Signatures.GenericContext? genCtx, bool isSetProperty = false)
    {
        TypeSignature? typeSig = prop.Signature?.ReturnType;
        if (typeSig is null) { return "object"; }
        if (genCtx is not null) { typeSig = typeSig.InstantiateGenericTypes(genCtx.Value); }
        return w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectedSignature(w, typeSig, isSetProperty)));
    }

    /// <summary>Mirrors C++ <c>write_interface_member_signatures</c>.</summary>
    public static void WriteInterfaceMemberSignatures(TypeWriter w, TypeDefinition type)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            if (Helpers.IsSpecial(method)) { continue; }
            MethodSig sig = new(method);
            w.Write("\n");
            WriteProjectionReturnType(w, sig);
            w.Write(" ");
            w.Write(method.Name?.Value ?? string.Empty);
            w.Write("(");
            WriteParameterList(w, sig);
            w.Write(");");
        }

        foreach (PropertyDefinition prop in type.Properties)
        {
            (MethodDefinition? getter, MethodDefinition? setter) = Helpers.GetPropertyMethods(prop);
            // 'new' qualifier - simplified: skip (would require base interface property lookup).
            string newKeyword = string.Empty;
            string propType = WritePropType(w, prop);
            w.Write("\n");
            w.Write(newKeyword);
            w.Write(propType);
            w.Write(" ");
            w.Write(prop.Name?.Value ?? string.Empty);
            w.Write(" {");
            if (getter is not null || setter is not null) { w.Write(" get;"); }
            if (setter is not null) { w.Write(" set;"); }
            w.Write(" }");
        }

        foreach (EventDefinition evt in type.Events)
        {
            w.Write("\nevent ");
            WriteEventType(w, evt);
            w.Write(" ");
            w.Write(evt.Name?.Value ?? string.Empty);
            w.Write(";");
        }
    }

    /// <summary>
    /// Mirrors C++ <c>write_interface</c>. Emits an interface projection.
    /// </summary>
    public static void WriteInterface(TypeWriter w, TypeDefinition type)
    {
        // Mirrors C++ write_interface skip rule: exclusive interfaces other than the default
        // and overridable one are not used in the projection. Skip them unless public_exclusiveto
        // is set (or in reference projection or component mode).
        if (!w.Settings.ReferenceProjection &&
            !w.Settings.Component &&
            TypeCategorization.IsExclusiveTo(type) &&
            !w.Settings.PublicExclusiveTo &&
            !IsDefaultOrOverridableInterfaceTypedef(type))
        {
            return;
        }

        if (w.Settings.Component && !TypeCategorization.IsExclusiveTo(type))
        {
            return;
        }

        w.Write("\n");
        WriteWinRTMetadataAttribute(w, type, _cacheRef!);
        WriteGuidAttribute(w, type);
        w.Write("\n");
        WriteTypeCustomAttributes(w, type, false);

        bool isInternal = (TypeCategorization.IsExclusiveTo(type) && !w.Settings.PublicExclusiveTo) ||
                          TypeCategorization.IsProjectionInternal(type);
        w.Write(isInternal ? "internal" : "public");
        w.Write(" interface ");
        WriteTypedefName(w, type, TypedefNameType.CCW, false);
        WriteTypeParams(w, type);
        WriteTypeInheritance(w, type, false, false);
        w.Write("\n{");
        WriteInterfaceMemberSignatures(w, type);
        w.Write("\n}\n");
    }

    /// <summary>Mirrors C++ <c>is_default_or_overridable_interface_typedef</c>: returns true if the
    /// given exclusive interface is referenced as a [Default] or [Overridable] interface impl on
    /// the class it's exclusive to.</summary>
    private static bool IsDefaultOrOverridableInterfaceTypedef(TypeDefinition iface)
    {
        if (!TypeCategorization.IsExclusiveTo(iface)) { return false; }
        TypeDefinition? classType = GetExclusiveToType(iface);
        if (classType is null) { return false; }
        foreach (InterfaceImplementation impl in classType.Interfaces)
        {
            if (!Helpers.IsDefaultInterface(impl) && !Helpers.IsOverridable(impl)) { continue; }
            ITypeDefOrRef? implRef = impl.Interface;
            if (implRef is null) { continue; }
            TypeDefinition? implDef = ResolveInterfaceTypeDefForExclusiveCheck(implRef);
            if (implDef is not null && implDef == iface) { return true; }
        }
        return false;
    }

    private static TypeDefinition? ResolveInterfaceTypeDefForExclusiveCheck(ITypeDefOrRef ifaceRef)
    {
        if (ifaceRef is TypeDefinition td) { return td; }
        if (ifaceRef is TypeReference tr && _cacheRef is not null)
        {
            string ns = tr.Namespace?.Value ?? string.Empty;
            string nm = tr.Name?.Value ?? string.Empty;
            return _cacheRef.Find(ns + "." + nm);
        }
        if (ifaceRef is TypeSpecification ts && ts.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef? gen = gi.GenericType;
            return gen is null ? null : ResolveInterfaceTypeDefForExclusiveCheck(gen);
        }
        return null;
    }
}
