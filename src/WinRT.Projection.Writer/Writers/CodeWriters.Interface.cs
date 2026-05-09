// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter;

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

        // Check the base type. If the class extends another runtime class (not System.Object),
        // emit the projected base type name. Mirrors C++ write_type_inheritance, which only
        // checks for object_type — WindowsRuntime.WindowsRuntimeObject is a managed type
        // defined in WinRT.Runtime and is never referenced as a base type in any .winmd, so
        // there is no need to check for it here.
        bool hasNonObjectBase = false;
        if (type.BaseType is not null)
        {
            string? baseNs = type.BaseType.Namespace?.Value;
            string? baseName = type.BaseType.Name?.Value;
            hasNonObjectBase = !(baseNs == "System" && baseName == "Object");
        }

        if (hasNonObjectBase)
        {
            w.Write(delimiter);
            // Write the projected base type name. Same-namespace types stay unqualified (e.g.
            // 'AppointmentActionEntity : ActionEntity') — only emit 'global::' when the base
            // class lives in a different namespace (mirrors C++ write_typedef_name behavior).
            ITypeDefOrRef baseType = type.BaseType!;
            string ns = baseType.Namespace?.Value ?? string.Empty;
            string name = baseType.Name?.Value ?? string.Empty;
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
            // Mirror C++ write_interface_required which calls write_custom_attributes for method.CustomAttribute().
            // Only emit Windows.Foundation.Metadata attributes that have a projected form (Overload, DefaultOverload, AttributeUsage, Experimental).
            WriteMethodCustomAttributes(w, method);
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
            // Mirror C++ code_writers.h:5642 — emit 'new' when the property is setter-only
            // on this interface AND a property of the same name exists in any base interface
            // (typically the getter-only counterpart). This hides the inherited member.
            string newKeyword = (getter is null && setter is not null
                && FindPropertyInBaseInterfaces(type, prop.Name?.Value ?? string.Empty))
                ? "new " : string.Empty;
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
    /// Recursively walks the base interfaces of <paramref name="type"/> looking for a property
    /// with the given <paramref name="propName"/>. Mirrors C++ <c>find_property_interface</c>
    /// at code_writers.h:4154-4185 (returns true if any base interface declares a property
    /// with that name; used to decide whether a setter-only property in a derived interface
    /// needs the <c>new</c> modifier to hide the base getter).
    /// </summary>
    private static bool FindPropertyInBaseInterfaces(TypeDefinition type, string propName)
    {
        if (string.IsNullOrEmpty(propName)) { return false; }
        System.Collections.Generic.HashSet<TypeDefinition> visited = new();
        return FindPropertyInBaseInterfacesRecursive(type, propName, visited);
    }

    private static bool FindPropertyInBaseInterfacesRecursive(TypeDefinition type, string propName, System.Collections.Generic.HashSet<TypeDefinition> visited)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            TypeDefinition? baseIface = ResolveInterface(impl.Interface);
            if (baseIface is null) { continue; }
            // Skip the original setter-defining interface itself (matches C++ check
            // 'setter_iface != type'). Also dedupe via the visited set.
            if (baseIface == type) { continue; }
            if (!visited.Add(baseIface)) { continue; }
            foreach (PropertyDefinition prop in baseIface.Properties)
            {
                if ((prop.Name?.Value ?? string.Empty) == propName) { return true; }
            }
            if (FindPropertyInBaseInterfacesRecursive(baseIface, propName, visited)) { return true; }
        }
        return false;
    }

    /// <summary>
    /// Like <see cref="FindPropertyInBaseInterfaces"/> but returns the base interface where the
    /// property was found (or <c>null</c> if not found). Mirrors the C++ tool's
    /// <c>find_property_interface</c> which returns a pair&lt;TypeDef, bool&gt;.
    /// </summary>
    public static TypeDefinition? FindPropertyInterfaceInBases(TypeDefinition type, string propName)
    {
        if (string.IsNullOrEmpty(propName)) { return null; }
        System.Collections.Generic.HashSet<TypeDefinition> visited = new();
        return FindPropertyInterfaceInBasesRecursive(type, propName, visited);
    }

    private static TypeDefinition? FindPropertyInterfaceInBasesRecursive(TypeDefinition type, string propName, System.Collections.Generic.HashSet<TypeDefinition> visited)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            TypeDefinition? baseIface = ResolveInterface(impl.Interface);
            if (baseIface is null) { continue; }
            if (baseIface == type) { continue; }
            if (!visited.Add(baseIface)) { continue; }
            foreach (PropertyDefinition prop in baseIface.Properties)
            {
                if ((prop.Name?.Value ?? string.Empty) == propName) { return baseIface; }
            }
            TypeDefinition? deeper = FindPropertyInterfaceInBasesRecursive(baseIface, propName, visited);
            if (deeper is not null) { return deeper; }
        }
        return null;
    }

    /// <summary>
    /// Emits the projected custom attributes for an interface method. Mirrors C++
    /// <c>write_custom_attributes</c> filtered for the projected attributes.
    /// </summary>
    private static void WriteMethodCustomAttributes(TypeWriter w, MethodDefinition method)
    {
        foreach (CustomAttribute attr in method.CustomAttributes)
        {
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            string ns = attrType.Namespace?.Value ?? string.Empty;
            string nm = attrType.Name?.Value ?? string.Empty;
            if (ns != "Windows.Foundation.Metadata") { continue; }
            string baseName = nm.EndsWith("Attribute", System.StringComparison.Ordinal) ? nm[..^"Attribute".Length] : nm;
            // Only the attributes the C++ tool considers projected (see code_writers.h).
            if (baseName is not ("Overload" or "DefaultOverload" or "Experimental"))
            {
                continue;
            }
            w.Write("[global::Windows.Foundation.Metadata.");
            w.Write(baseName);
            // Args: only handle string args (sufficient for [Overload(@"X")]). [DefaultOverload] has none.
            if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 0)
            {
                w.Write("(");
                for (int i = 0; i < attr.Signature.FixedArguments.Count; i++)
                {
                    if (i > 0) { w.Write(", "); }
                    object? val = attr.Signature.FixedArguments[i].Element;
                    if (val is AsmResolver.Utf8String s)
                    {
                        w.Write("@\"");
                        w.Write(s.Value);
                        w.Write("\"");
                    }
                    else if (val is string ss)
                    {
                        w.Write("@\"");
                        w.Write(ss);
                        w.Write("\"");
                    }
                    else
                    {
                        w.Write(val?.ToString() ?? string.Empty);
                    }
                }
                w.Write(")");
            }
            w.Write("]\n");
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
