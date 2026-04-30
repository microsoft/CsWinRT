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

        if (includeWindowsRuntimeObject)
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
            // For TypeRef interfaces, we can't easily resolve - default to include all unless excluded.
            bool isExclusive = false;
            if (impl.Interface is TypeDefinition ifaceTypeDef)
            {
                isExclusive = TypeCategorization.IsExclusiveTo(ifaceTypeDef);
            }

            if (!(isOverridable || !isExclusive || includeExclusiveInterface))
            {
                continue;
            }

            w.Write(delimiter);
            delimiter = ", ";

            // Emit the interface name (CCW)
            if (impl.Interface is TypeDefinition ifaceType)
            {
                WriteTypedefName(w, ifaceType, TypedefNameType.CCW, false);
                WriteTypeParams(w, ifaceType);
            }
            else if (impl.Interface is TypeReference tr)
            {
                w.Write("global::");
                w.Write(tr.Namespace?.Value ?? string.Empty);
                w.Write(".");
                w.WriteCode(tr.Name?.Value ?? string.Empty);
            }
            else if (impl.Interface is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
            {
                // Generic instance interface
                ITypeDefOrRef gt = gi.GenericType;
                w.Write("global::");
                w.Write(gt.Namespace?.Value ?? string.Empty);
                w.Write(".");
                w.WriteCode(gt.Name?.Value ?? string.Empty);
                w.Write("<");
                for (int i = 0; i < gi.TypeArguments.Count; i++)
                {
                    if (i > 0) { w.Write(", "); }
                    WriteTypeName(w, TypeSemanticsFactory.Get(gi.TypeArguments[i]), TypedefNameType.Projected, true);
                }
                w.Write(">");
            }

            if (includeWindowsRuntimeObject && !w.Settings.ReferenceProjection)
            {
                w.Write(", IWindowsRuntimeInterface<");
                if (impl.Interface is TypeDefinition ifaceType2)
                {
                    WriteTypedefName(w, ifaceType2, TypedefNameType.CCW, false);
                    WriteTypeParams(w, ifaceType2);
                }
                else if (impl.Interface is TypeReference tr2)
                {
                    w.Write("global::");
                    w.Write(tr2.Namespace?.Value ?? string.Empty);
                    w.Write(".");
                    w.WriteCode(tr2.Name?.Value ?? string.Empty);
                }
                else if (impl.Interface is TypeSpecification ts2 && ts2.Signature is GenericInstanceTypeSignature gi2)
                {
                    ITypeDefOrRef gt2 = gi2.GenericType;
                    w.Write("global::");
                    w.Write(gt2.Namespace?.Value ?? string.Empty);
                    w.Write(".");
                    w.WriteCode(gt2.Name?.Value ?? string.Empty);
                    w.Write("<");
                    for (int i = 0; i < gi2.TypeArguments.Count; i++)
                    {
                        if (i > 0) { w.Write(", "); }
                        WriteTypeName(w, TypeSemanticsFactory.Get(gi2.TypeArguments[i]), TypedefNameType.Projected, true);
                    }
                    w.Write(">");
                }
                w.Write(">");
            }
        }
    }

    /// <summary>Mirrors C++ <c>write_prop_type</c>.</summary>
    public static string WritePropType(TypeWriter w, PropertyDefinition prop, bool isSetProperty = false)
    {
        TypeSignature? typeSig = prop.Signature?.ReturnType;
        if (typeSig is null) { return "object"; }
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
            if (evt.EventType is TypeDefinition etDef)
            {
                WriteTypedefName(w, etDef, TypedefNameType.Projected, false);
                WriteTypeParams(w, etDef);
            }
            else if (evt.EventType is TypeReference etRef)
            {
                w.Write("global::");
                w.Write(etRef.Namespace?.Value ?? string.Empty);
                w.Write(".");
                w.WriteCode(etRef.Name?.Value ?? string.Empty);
            }
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
        // Skip exclusive interfaces in non-component, non-reference mode (unless public_exclusiveto).
        // Simplified - also skip if not a default-or-overridable interface.
        if (!w.Settings.ReferenceProjection &&
            !w.Settings.Component &&
            TypeCategorization.IsExclusiveTo(type) &&
            !w.Settings.PublicExclusiveTo)
        {
            // We may still need to emit if it's a default/overridable interface used by a class.
            // Simplified port: emit anyway when not in component/reference mode.
            // The C++ checks is_default_or_overridable_interface_typedef which requires resolving
            // exclusive_to_type. We omit that resolution here for simplicity.
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
}
