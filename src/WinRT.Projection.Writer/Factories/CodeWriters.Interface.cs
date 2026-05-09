// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Interface, class, and ABI emission helpers.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Writes the <c>[Guid("...")]</c> attribute for a type.</summary>
    public static void WriteGuidAttribute(IndentedTextWriter writer, TypeDefinition type)
    {
        bool fullyQualify = type.Namespace == "Windows.Foundation.Metadata";
        writer.Write("[");
        writer.Write(fullyQualify ? "global::System.Runtime.InteropServices.Guid" : "Guid");
        writer.Write("(\"");
        WriteGuid(writer, type, false);
        writer.Write("\")]");
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteGuidAttribute(TypeWriter w, TypeDefinition type)
        => WriteGuidAttribute(w.Writer, type);

    /// <summary>Writes a class or interface inheritance clause: " : Base, Iface1, Iface2&lt;T&gt;".</summary>
    public static void WriteTypeInheritance(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, bool includeExclusiveInterface, bool includeWindowsRuntimeObject)
    {
        string delimiter = " : ";

        // Check the base type. If the class extends another runtime class (not System.Object),
        // emit the projected base type name. WindowsRuntime.WindowsRuntimeObject is a managed
        // type defined in WinRT.Runtime and is never referenced as a base type in any .winmd, so
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
            writer.Write(delimiter);
            // Same-namespace types stay unqualified (e.g. 'AppointmentActionEntity : ActionEntity'):
            // only emit 'global::' when the base class lives in a different namespace.
            ITypeDefOrRef baseType = type.BaseType!;
            (string ns, string name) = baseType.Names();
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            if (!string.IsNullOrEmpty(ns) && ns != context.CurrentNamespace)
            {
                writer.Write("global::");
                writer.Write(ns);
                writer.Write(".");
            }
            writer.Write(IdentifierEscaping.StripBackticks(name));
            delimiter = ", ";
        }
        else if (includeWindowsRuntimeObject)
        {
            writer.Write(delimiter);
            writer.Write("WindowsRuntimeObject");
            delimiter = ", ";
        }

        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }

            bool isOverridable = impl.IsOverridable();

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

            writer.Write(delimiter);
            delimiter = ", ";

            // Emit the interface name (CCW) with mapped-type remapping.
            WriteInterfaceTypeName(writer, context, impl.Interface);

            if (includeWindowsRuntimeObject && !context.Settings.ReferenceProjection)
            {
                writer.Write(", IWindowsRuntimeInterface<");
                WriteInterfaceTypeName(writer, context, impl.Interface);
                writer.Write(">");
            }
        }
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteTypeInheritance(TypeWriter w, TypeDefinition type, bool includeExclusiveInterface, bool includeWindowsRuntimeObject)
        => WriteTypeInheritance(w.Writer, w.Context, type, includeExclusiveInterface, includeWindowsRuntimeObject);

    /// <summary>
    /// Writes the projected name for an interface reference (TypeDefinition, TypeReference, or
    /// generic instance), applying mapped-type remapping (e.g.,
    /// <c>Windows.Foundation.Collections.IMap&lt;K,V&gt;</c> -> <c>System.Collections.Generic.IDictionary&lt;K,V&gt;</c>).
    /// </summary>
    public static void WriteInterfaceTypeName(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef ifaceType)
    {
        if (ifaceType is TypeDefinition td)
        {
            WriteTypedefName(writer, context, td, TypedefNameType.CCW, false);
            WriteTypeParams(writer, td);
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
            // Only emit the global:: prefix when the namespace doesn't match the current emit
            // namespace (mirrors WriteTypedefName behavior -- same-namespace stays unqualified).
            if (!string.IsNullOrEmpty(ns) && ns != context.CurrentNamespace)
            {
                writer.Write("global::");
                writer.Write(ns);
                writer.Write(".");
            }
            writer.Write(IdentifierEscaping.StripBackticks(name));
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
            if (!string.IsNullOrEmpty(ns) && ns != context.CurrentNamespace)
            {
                writer.Write("global::");
                writer.Write(ns);
                writer.Write(".");
            }
            writer.Write(IdentifierEscaping.StripBackticks(name));
            writer.Write("<");
            for (int i = 0; i < gi.TypeArguments.Count; i++)
            {
                if (i > 0) { writer.Write(", "); }
                // Pass forceWriteNamespace=false so type args also respect the current namespace.
                WriteTypeName(writer, context, TypeSemanticsFactory.Get(gi.TypeArguments[i]), TypedefNameType.Projected, false);
            }
            writer.Write(">");
        }
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteInterfaceTypeName(TypeWriter w, ITypeDefOrRef ifaceType)
        => WriteInterfaceTypeName(w.Writer, w.Context, ifaceType);

    /// <summary>Returns the projected property type for <paramref name="prop"/>.</summary>
    public static string WritePropType(ProjectionEmitContext context, PropertyDefinition prop, bool isSetProperty = false)
        => WritePropType(context, prop, null, isSetProperty);

    /// <summary>Returns the projected property type for <paramref name="prop"/>, optionally substituting generic args.</summary>
    public static string WritePropType(ProjectionEmitContext context, PropertyDefinition prop, GenericContext? genCtx, bool isSetProperty = false)
    {
        TypeSignature? typeSig = prop.Signature?.ReturnType;
        if (typeSig is null) { return "object"; }
        if (genCtx is not null) { typeSig = typeSig.InstantiateGenericTypes(genCtx.Value); }
        IndentedTextWriter scratch = new();
        WriteProjectedSignature(scratch, context, typeSig, isSetProperty);
        return scratch.ToString();
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static string WritePropType(TypeWriter w, PropertyDefinition prop, bool isSetProperty = false)
        => WritePropType(w.Context, prop, null, isSetProperty);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static string WritePropType(TypeWriter w, PropertyDefinition prop, GenericContext? genCtx, bool isSetProperty = false)
        => WritePropType(w.Context, prop, genCtx, isSetProperty);

    /// <summary>Emits all method, property, and event signatures of an interface.</summary>
    public static void WriteInterfaceMemberSignatures(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsSpecial()) { continue; }
            MethodSig sig = new(method);
            writer.Write("\n");
            // Only emit Windows.Foundation.Metadata attributes that have a projected form
            // (Overload, DefaultOverload, AttributeUsage, Experimental).
            WriteMethodCustomAttributes(writer, method);
            WriteProjectionReturnType(writer, context, sig);
            writer.Write(" ");
            writer.Write(method.Name?.Value ?? string.Empty);
            writer.Write("(");
            WriteParameterList(writer, context, sig);
            writer.Write(");");
        }

        foreach (PropertyDefinition prop in type.Properties)
        {
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
            // Add 'new' when this interface has a setter-only property AND a property of the same
            // name exists on a base interface (typically the getter-only counterpart). This hides
            // the inherited member.
            string newKeyword = (getter is null && setter is not null
                && FindPropertyInBaseInterfaces(type, prop.Name?.Value ?? string.Empty))
                ? "new " : string.Empty;
            string propType = WritePropType(context, prop);
            writer.Write("\n");
            writer.Write(newKeyword);
            writer.Write(propType);
            writer.Write(" ");
            writer.Write(prop.Name?.Value ?? string.Empty);
            writer.Write(" {");
            if (getter is not null || setter is not null) { writer.Write(" get;"); }
            if (setter is not null) { writer.Write(" set;"); }
            writer.Write(" }");
        }

        foreach (EventDefinition evt in type.Events)
        {
            writer.Write("\nevent ");
            WriteEventType(writer, context, evt);
            writer.Write(" ");
            writer.Write(evt.Name?.Value ?? string.Empty);
            writer.Write(";");
        }
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteInterfaceMemberSignatures(TypeWriter w, TypeDefinition type)
        => WriteInterfaceMemberSignatures(w.Writer, w.Context, type);

    /// <summary>
    /// Recursively walks the base interfaces of <paramref name="type"/> looking for a property
    /// with the given <paramref name="propName"/>. Returns true if any base interface declares
    /// a property with that name (used to decide whether a setter-only property in a derived
    /// interface needs the <c>new</c> modifier to hide the base getter).
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
            // Skip the original setter-defining interface itself. Also dedupe via the visited set.
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
    /// property was found (or <c>null</c> if not found).
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
    /// Emits the projected custom attributes for an interface method (filtered for the projected
    /// attributes: Overload, DefaultOverload, Experimental).
    /// </summary>
    private static void WriteMethodCustomAttributes(IndentedTextWriter writer, MethodDefinition method)
    {
        foreach (CustomAttribute attr in method.CustomAttributes)
        {
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            (string ns, string nm) = attrType.Names();
            if (ns != "Windows.Foundation.Metadata") { continue; }
            string baseName = nm.EndsWith("Attribute", System.StringComparison.Ordinal) ? nm[..^"Attribute".Length] : nm;
            if (baseName is not ("Overload" or "DefaultOverload" or "Experimental"))
            {
                continue;
            }
            writer.Write("[global::Windows.Foundation.Metadata.");
            writer.Write(baseName);
            // Args: only handle string args (sufficient for [Overload(@"X")]). [DefaultOverload] has none.
            if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 0)
            {
                writer.Write("(");
                for (int i = 0; i < attr.Signature.FixedArguments.Count; i++)
                {
                    if (i > 0) { writer.Write(", "); }
                    object? val = attr.Signature.FixedArguments[i].Element;
                    if (val is AsmResolver.Utf8String s)
                    {
                        writer.Write("@\"");
                        writer.Write(s.Value);
                        writer.Write("\"");
                    }
                    else if (val is string ss)
                    {
                        writer.Write("@\"");
                        writer.Write(ss);
                        writer.Write("\"");
                    }
                    else
                    {
                        writer.Write(val?.ToString() ?? string.Empty);
                    }
                }
                writer.Write(")");
            }
            writer.Write("]\n");
        }
    }

    /// <summary>Writes a projected interface declaration.</summary>
    public static void WriteInterface(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // [Default] and overridable interfaces aren't used in the projection. Skip them unless
        // public_exclusiveto is set (or in reference projection or component mode).
        if (!context.Settings.ReferenceProjection &&
            !context.Settings.Component &&
            TypeCategorization.IsExclusiveTo(type) &&
            !context.Settings.PublicExclusiveTo &&
            !IsDefaultOrOverridableInterfaceTypedef(type))
        {
            return;
        }

        if (context.Settings.Component && !TypeCategorization.IsExclusiveTo(type))
        {
            return;
        }

        writer.Write("\n");
        WriteWinRTMetadataAttribute(writer, type, _cacheRef!);
        WriteGuidAttribute(writer, type);
        writer.Write("\n");
        WriteTypeCustomAttributes(writer, context, type, false);

        bool isInternal = (TypeCategorization.IsExclusiveTo(type) && !context.Settings.PublicExclusiveTo) ||
                          TypeCategorization.IsProjectionInternal(type);
        writer.Write(isInternal ? "internal" : "public");
        writer.Write(" interface ");
        WriteTypedefName(writer, context, type, TypedefNameType.CCW, false);
        WriteTypeParams(writer, type);
        WriteTypeInheritance(writer, context, type, false, false);
        writer.Write("\n{");
        WriteInterfaceMemberSignatures(writer, context, type);
        writer.Write("\n}\n");
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteInterface(TypeWriter w, TypeDefinition type)
        => WriteInterface(w.Writer, w.Context, type);

    /// <summary>Returns true if the given exclusive interface is referenced as a [Default] or
    /// [Overridable] interface impl on the class it's exclusive to.</summary>
    private static bool IsDefaultOrOverridableInterfaceTypedef(TypeDefinition iface)
    {
        if (!TypeCategorization.IsExclusiveTo(iface)) { return false; }
        TypeDefinition? classType = GetExclusiveToType(iface);
        if (classType is null) { return false; }
        foreach (InterfaceImplementation impl in classType.Interfaces)
        {
            if (!impl.IsDefaultInterface() && !impl.IsOverridable()) { continue; }
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
            (string ns, string nm) = tr.Names();
            return _cacheRef.Find(ns + "." + nm);
        }
        if (ifaceRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef? gen = gi.GenericType;
            return gen is null ? null : ResolveInterfaceTypeDefForExclusiveCheck(gen);
        }
        return null;
    }
}
