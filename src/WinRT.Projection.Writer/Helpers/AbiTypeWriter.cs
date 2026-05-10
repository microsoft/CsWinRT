// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Writers;
using WindowsRuntime.ProjectionWriter.Metadata;
using AsmResolver.DotNet.Signatures;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

using static WindowsRuntime.ProjectionWriter.References.WellKnownTypeNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Writes the ABI projection of a 'TypeSemantics' (or fundamental type) directly to an 'IndentedTextWriter'.
/// </summary>
internal static class AbiTypeWriter
{
    /// <summary>
    /// Writes the C# representation of the ABI type for the given <paramref name="semantics"/> (e.g. fundamental primitives, <c>void*</c> for object/interface types, etc.).
    /// </summary>
    public static void WriteAbiType(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
    {
        switch (semantics)
        {
            case TypeSemantics.Fundamental f:
                writer.Write(GetAbiFundamentalType(f.Type));
                break;
            case TypeSemantics.Object_:
                writer.Write("void*");
                break;
            case TypeSemantics.Guid_:
                writer.Write("Guid");
                break;
            case TypeSemantics.Type_:
                writer.Write("global::WindowsRuntime.InteropServices.WindowsRuntimeTypeName");
                break;
            case TypeSemantics.Definition d:
                if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Enum)
                {
                    // Enums in WinRT ABI use the projected enum type directly (since their C#
                    // layout matches their underlying integer ABI representation 1:1).
                    TypedefNameWriter.WriteTypedefName(writer, context, d.Type, TypedefNameType.Projected, true);
                }
                else if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Struct)
                {
                    (string dNs, string dName) = d.Type.Names();
                    // Special case: mapped value types that require ABI marshalling
                    // (DateTime/TimeSpan -> ABI.System.DateTimeOffset/TimeSpan).
                    if (dNs == WindowsFoundation && dName == "DateTime")
                    {
                        writer.Write("global::ABI.System.DateTimeOffset");
                        break;
                    }
                    if (dNs == WindowsFoundation && dName == "TimeSpan")
                    {
                        writer.Write("global::ABI.System.TimeSpan");
                        break;
                    }
                    if (dNs == WindowsFoundation && dName == HResult)
                    {
                        writer.Write("global::ABI.System.Exception");
                        break;
                    }
                    if (dNs == WindowsUIXamlInterop && dName == TypeName)
                    {
                        // System.Type ABI struct: maps to global::ABI.System.Type, not the
                        // ABI.Windows.UI.Xaml.Interop.TypeName form.
                        writer.Write("global::ABI.System.Type");
                        break;
                    }
                    TypeSignature dts = d.Type.ToTypeSignature();
                    // "Almost-blittable" structs (with bool/char fields but no reference-type
                    // fields) can pass through using the projected type since the C# layout
                    // matches the WinRT ABI directly. Truly complex structs (with string/object/
                    // Nullable<T> fields) need the ABI struct.
                    if (context.AbiTypeShapeResolver.IsAnyStruct(dts))
                    {
                        TypedefNameWriter.WriteTypedefName(writer, context, d.Type, TypedefNameType.Projected, true);
                    }
                    else
                    {
                        TypedefNameWriter.WriteTypedefName(writer, context, d.Type, TypedefNameType.ABI, true);
                    }
                }
                else
                {
                    writer.Write("void*");
                }
                break;
            case TypeSemantics.Reference r:
                // Cross-module typeref: try resolving the type, applying mapped-type translation
                // for the field/parameter type after resolution.
                if (context.Cache is not null)
                {
                    (string rns, string rname) = r.Reference_.Names();
                    // Special case: mapped value types that require ABI marshalling.
                    if (rns == WindowsFoundation && rname == "DateTime")
                    {
                        writer.Write("global::ABI.System.DateTimeOffset");
                        break;
                    }
                    if (rns == WindowsFoundation && rname == "TimeSpan")
                    {
                        writer.Write("global::ABI.System.TimeSpan");
                        break;
                    }
                    if (rns == WindowsFoundation && rname == HResult)
                    {
                        writer.Write("global::ABI.System.Exception");
                        break;
                    }
                    // Look up the type by its ORIGINAL (unmapped) name in the cache.
                    TypeDefinition? rd = context.Cache.Find(rns + "." + rname);
                    // If not found, try the mapped name (for cases where the mapping target is in the cache).
                    if (rd is null)
                    {
                        MappedType? rmapped = MappedTypes.Get(rns, rname);
                        if (rmapped is not null)
                        {
                            rd = context.Cache.Find(rmapped.MappedNamespace + "." + rmapped.MappedName);
                        }
                    }
                    if (rd is not null)
                    {
                        TypeCategory cat = TypeCategorization.GetCategory(rd);
                        if (cat == TypeCategory.Enum)
                        {
                            // Enums use the projected enum type directly (C# layout == ABI layout).
                            TypedefNameWriter.WriteTypedefName(writer, context, rd, TypedefNameType.Projected, true);
                            break;
                        }
                        if (cat == TypeCategory.Struct)
                        {
                            // Special case: HResult is mapped to System.Exception (a reference type)
                            // but its ABI representation is the global::ABI.System.Exception struct
                            // (which wraps the underlying HRESULT int).
                            (string rdNs, string rdName) = rd.Names();
                            if (rdNs == WindowsFoundation && rdName == HResult)
                            {
                                writer.Write("global::ABI.System.Exception");
                                break;
                            }
                            if (context.AbiTypeShapeResolver.IsAnyStruct(rd.ToTypeSignature()))
                            {
                                TypedefNameWriter.WriteTypedefName(writer, context, rd, TypedefNameType.Projected, true);
                            }
                            else
                            {
                                TypedefNameWriter.WriteTypedefName(writer, context, rd, TypedefNameType.ABI, true);
                            }
                            break;
                        }
                    }
                }
                // Unresolved cross-assembly TypeRef. If the signature was encoded as a value type
                // (e.g. WindowId from Microsoft.UI.winmd when that winmd isn't loaded), assume it's
                // a blittable struct and emit the projected type name — the consumer's compiler
                // will resolve it via their own references. Otherwise (encoded as Class) emit
                // void* (it's a runtime class/interface/delegate).
                if (r.IsValueType)
                {
                    (string rns, string rname) = r.Reference_.Names();
                    writer.Write(GlobalPrefix);
                    if (!string.IsNullOrEmpty(rns)) { writer.Write($"{rns}."); }
                    writer.Write(IdentifierEscaping.StripBackticks(rname));
                    break;
                }
                writer.Write("void*");
                break;
            case TypeSemantics.GenericInstance:
                writer.Write("void*");
                break;
            default:
                writer.Write("void*");
                break;
        }
    }

    internal static string GetAbiFundamentalType(FundamentalType t) => t switch
    {
        FundamentalType.Boolean => "bool",
        FundamentalType.Char => "char",
        FundamentalType.String => "void*",
        _ => FundamentalTypes.ToCSharpType(t)
    };
}
