// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Type-name emission helpers, mirroring C++ <c>code_writers.h</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>write_fundamental_type</c>.</summary>
    public static void WriteFundamentalType(TextWriter w, FundamentalType t)
    {
        w.Write(FundamentalTypes.ToCSharpType(t));
    }

    /// <summary>Mirrors C++ <c>write_fundamental_non_projected_type</c>.</summary>
    public static void WriteFundamentalNonProjectedType(TextWriter w, FundamentalType t)
    {
        w.Write(FundamentalTypes.ToDotNetType(t));
    }

    /// <summary>Mirrors C++ <c>write_typedef_name</c>: writes the C# type name for a typed reference.</summary>
    public static void WriteTypedefName(TypeWriter w, TypeDefinition type, TypedefNameType nameType = TypedefNameType.Projected, bool forceWriteNamespace = false)
    {
        bool authoredType = w.Settings.Component && w.Settings.Filter.Includes(type);
        string typeNamespace = type.Namespace?.Value ?? string.Empty;
        string typeName = type.Name?.Value ?? string.Empty;

        if (nameType == TypedefNameType.NonProjected)
        {
            w.Write(typeNamespace);
            w.Write(".");
            w.Write(typeName);
            return;
        }

        MappedType? proj = MappedTypes.Get(typeNamespace, typeName);
        if (proj is not null)
        {
            typeNamespace = proj.MappedNamespace;
            typeName = proj.MappedName;
        }

        // Exclusive interfaces handling: simplified port — we don't try to resolve exclusive_to_type from
        // attributes here. Only used in component mode which we don't fully implement here yet.
        TypedefNameType nameToWrite = nameType;
        if (authoredType && TypeCategorization.IsExclusiveTo(type) && nameToWrite == TypedefNameType.Projected)
        {
            // Fallback: switch to CCW if the type is not the default interface for its exclusive class.
            nameToWrite = TypedefNameType.CCW;
        }

        // Authored interfaces that aren't exclusive use the same authored interface.
        if (authoredType && nameToWrite == TypedefNameType.CCW &&
            TypeCategorization.GetCategory(type) == TypeCategory.Interface &&
            !TypeCategorization.IsExclusiveTo(type))
        {
            nameToWrite = TypedefNameType.Projected;
        }

        if (nameToWrite == TypedefNameType.EventSource && typeNamespace == "System")
        {
            w.Write("global::WindowsRuntime.InteropServices.");
        }
        else if (forceWriteNamespace ||
            typeNamespace != w.CurrentNamespace ||
            (nameToWrite == TypedefNameType.Projected && (w.InAbiNamespace || w.InAbiImplNamespace)) ||
            (nameToWrite == TypedefNameType.ABI && !w.InAbiNamespace) ||
            (nameToWrite == TypedefNameType.EventSource && !w.InAbiNamespace) ||
            (nameToWrite == TypedefNameType.CCW && authoredType && !w.InAbiImplNamespace) ||
            (nameToWrite == TypedefNameType.CCW && !authoredType && (w.InAbiNamespace || w.InAbiImplNamespace)))
        {
            w.Write("global::");
            if (nameToWrite is TypedefNameType.ABI or TypedefNameType.StaticAbiClass or TypedefNameType.EventSource)
            {
                w.Write("ABI.");
            }
            else if (authoredType && nameToWrite == TypedefNameType.CCW)
            {
                w.Write("ABI.Impl.");
            }
            w.Write(typeNamespace);
            w.Write(".");
        }

        if (nameToWrite == TypedefNameType.StaticAbiClass)
        {
            w.WriteCode(typeName);
            w.Write("Methods");
        }
        else if (nameToWrite == TypedefNameType.EventSource)
        {
            w.WriteCode(typeName);
            w.Write("EventSource");
        }
        else
        {
            w.WriteCode(typeName);
        }
    }

    /// <summary>Mirrors C++ <c>write_type_params</c>: writes <c>&lt;T1, T2&gt;</c> for generic types.</summary>
    public static void WriteTypeParams(TypeWriter w, TypeDefinition type)
    {
        if (type.GenericParameters.Count == 0) { return; }
        w.Write("<");
        for (int i = 0; i < type.GenericParameters.Count; i++)
        {
            if (i > 0) { w.Write(", "); }
            // For now, emit "T0", "T1" style placeholders - full generic args support requires the writer's stack.
            string? gpName = type.GenericParameters[i].Name?.Value;
            w.Write(gpName ?? $"T{i}");
        }
        w.Write(">");
    }

    /// <summary>Mirrors C++ <c>write_type_name</c>: writes the typedef name + generic params.</summary>
    public static void WriteTypeName(TypeWriter w, TypeSemantics semantics, TypedefNameType nameType = TypedefNameType.Projected, bool forceWriteNamespace = false)
    {
        switch (semantics)
        {
            case TypeSemantics.Fundamental f:
                WriteFundamentalType(w, f.Type);
                break;
            case TypeSemantics.Object_:
                w.Write("object");
                break;
            case TypeSemantics.Guid_:
                w.Write("System.Guid");
                break;
            case TypeSemantics.Type_:
                w.Write("System.Type");
                break;
            case TypeSemantics.Definition d:
                WriteTypedefName(w, d.Type, nameType, forceWriteNamespace);
                WriteTypeParams(w, d.Type);
                break;
            case TypeSemantics.GenericInstance gi:
                WriteTypedefName(w, gi.GenericType, nameType, forceWriteNamespace);
                w.Write("<");
                for (int i = 0; i < gi.GenericArgs.Count; i++)
                {
                    if (i > 0) { w.Write(", "); }
                    WriteTypeName(w, gi.GenericArgs[i], nameType, forceWriteNamespace);
                }
                w.Write(">");
                break;
            case TypeSemantics.GenericInstanceRef gir:
                // Emit the type reference's full name with global:: qualification, applying mapped-type
                // remapping if applicable (e.g., Windows.Foundation.IReference`1<T> -> System.Nullable<T>,
                // Windows.Foundation.TypedEventHandler`2<S,R> -> System.EventHandler<S,R>).
                {
                    string ns = gir.GenericType.Namespace?.Value ?? string.Empty;
                    string name = gir.GenericType.Name?.Value ?? string.Empty;
                    MappedType? mapped = MappedTypes.Get(ns, name);
                    if (mapped is not null)
                    {
                        ns = mapped.MappedNamespace;
                        name = mapped.MappedName;
                    }
                    w.Write("global::");
                    w.Write(ns);
                    w.Write(".");
                    w.WriteCode(name);
                    w.Write("<");
                    for (int i = 0; i < gir.GenericArgs.Count; i++)
                    {
                        if (i > 0) { w.Write(", "); }
                        WriteTypeName(w, gir.GenericArgs[i], nameType, forceWriteNamespace);
                    }
                    w.Write(">");
                }
                break;
            case TypeSemantics.Reference r:
                {
                    string ns = r.Reference_.Namespace?.Value ?? string.Empty;
                    string name = r.Reference_.Name?.Value ?? string.Empty;
                    MappedType? mapped = MappedTypes.Get(ns, name);
                    if (mapped is not null)
                    {
                        ns = mapped.MappedNamespace;
                        name = mapped.MappedName;
                    }
                    if (!string.IsNullOrEmpty(ns))
                    {
                        w.Write("global::");
                        w.Write(ns);
                        w.Write(".");
                    }
                    w.WriteCode(name);
                }
                break;
            case TypeSemantics.GenericTypeIndex gti:
                w.Write($"T{gti.Index}");
                break;
        }
    }

    /// <summary>Mirrors C++ <c>write_projection_type</c>: writes a projected type name (.NET-style).</summary>
    public static void WriteProjectionType(TypeWriter w, TypeSemantics semantics)
    {
        WriteTypeName(w, semantics, TypedefNameType.Projected, false);
    }

    /// <summary>
    /// Writes the event handler type for an EventDefinition. Handles all the cases:
    /// TypeDefinition, TypeReference, TypeSpecification (generic instances like <c>EventHandler&lt;T&gt;</c>),
    /// and any other ITypeDefOrRef.
    /// </summary>
    public static void WriteEventType(TypeWriter w, EventDefinition evt)
    {
        if (evt.EventType is null)
        {
            w.Write("global::Windows.Foundation.EventHandler");
            return;
        }
        WriteTypeName(w, TypeSemanticsFactory.GetFromTypeDefOrRef(evt.EventType), TypedefNameType.Projected, true);
    }
}
