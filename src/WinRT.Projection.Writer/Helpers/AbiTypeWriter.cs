// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.References;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;
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
            case TypeSemantics.ObjectType:
                writer.Write("void*");
                break;
            case TypeSemantics.GuidType:
                writer.Write("Guid");
                break;
            case TypeSemantics.SystemType:
                writer.Write("global::WindowsRuntime.InteropServices.WindowsRuntimeTypeName");
                break;
            case TypeSemantics.Definition d:
                if (d.Type.IsEnum)
                {
                    // Enums in WinRT ABI use the projected enum type directly (since their C#
                    // layout matches their underlying integer ABI representation 1:1).
                    TypedefNameWriter.WriteTypedefName(writer, context, d.Type, TypedefNameType.Projected, true);
                }
                else if (d.Type.IsStruct)
                {
                    (string dNs, string dName) = d.Type.Names();

                    // Special case: mapped value types that require ABI marshalling
                    // (DateTime/TimeSpan -> ABI.System.DateTimeOffset/TimeSpan).
                    if (dNs == WindowsFoundation && dName == "DateTime")
                    {
                        writer.Write(WellKnownAbiTypeNames.AbiSystemDateTimeOffset);
                        break;
                    }

                    if (dNs == WindowsFoundation && dName == "TimeSpan")
                    {
                        writer.Write(WellKnownAbiTypeNames.AbiSystemTimeSpan);
                        break;
                    }

                    if (dNs == WindowsFoundation && dName == HResult)
                    {
                        writer.Write(WellKnownAbiTypeNames.AbiSystemException);
                        break;
                    }

                    if (dNs == WindowsUIXamlInterop && dName == TypeName)
                    {
                        // System.Type ABI struct: maps to global::ABI.System.Type, not the
                        // ABI.Windows.UI.Xaml.Interop.TypeName form.
                        writer.Write(WellKnownAbiTypeNames.AbiSystemType);
                        break;
                    }

                    TypeSignature dts = d.Type.ToTypeSignature();

                    // Blittable structs (whose projected layout matches the WinRT ABI
                    // representation, including ones with bool/char fields) can pass through
                    // using the projected type. Complex structs (with string/object/runtime-class/
                    // Nullable<T>/marshalling-mapped fields) need the ABI struct.
                    if (context.AbiTypeKindResolver.IsBlittableStruct(dts))
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
                (string rns, string rname) = r.Type.Names();

                // Special case: mapped value types that require ABI marshalling.
                if (rns == WindowsFoundation && rname == "DateTime")
                {
                    writer.Write(WellKnownAbiTypeNames.AbiSystemDateTimeOffset);
                    break;
                }

                if (rns == WindowsFoundation && rname == "TimeSpan")
                {
                    writer.Write(WellKnownAbiTypeNames.AbiSystemTimeSpan);
                    break;
                }

                if (rns == WindowsFoundation && rname == HResult)
                {
                    writer.Write(WellKnownAbiTypeNames.AbiSystemException);
                    break;
                }

                // Look up the type by its ORIGINAL (unmapped) name in the cache.
                TypeDefinition? rd = context.Cache.Find(rns, rname);

                // If not found, try the mapped name (for cases where the mapping target is in the cache).
                if (rd is null)
                {
                    if (MappedTypes.Get(rns, rname) is { } rmapped)
                    {
                        rd = context.Cache.Find(rmapped.MappedNamespace, rmapped.MappedName);
                    }
                }

                if (rd is not null)
                {
                    TypeKind kind = TypeKindResolver.Resolve(rd);

                    if (kind == TypeKind.Enum)
                    {
                        // Enums use the projected enum type directly (C# layout == ABI layout).
                        TypedefNameWriter.WriteTypedefName(writer, context, rd, TypedefNameType.Projected, true);
                        break;
                    }

                    if (kind == TypeKind.Struct)
                    {
                        // Special case: HResult is mapped to System.Exception (a reference type)
                        // but its ABI representation is the global::ABI.System.Exception struct
                        // (which wraps the underlying HRESULT int).
                        (string rdNs, string rdName) = rd.Names();

                        if (rdNs == WindowsFoundation && rdName == HResult)
                        {
                            writer.Write(WellKnownAbiTypeNames.AbiSystemException);
                            break;
                        }

                        if (context.AbiTypeKindResolver.IsBlittableStruct(rd.ToTypeSignature()))
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

                // Unresolved cross-assembly TypeRef. If the signature was encoded as a value type
                // (e.g. WindowId from Microsoft.UI.winmd when that winmd isn't loaded), assume it's
                // a blittable struct and emit the projected type name — the consumer's compiler
                // will resolve it via their own references. Otherwise (encoded as Class) emit
                // void* (it's a runtime class/interface/delegate).
                if (r.IsValueType)
                {
                    writer.Write(GlobalPrefix);

                    if (!string.IsNullOrEmpty(rns))
                    {
                        writer.Write($"{rns}.");
                    }

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

    /// <inheritdoc cref="WriteAbiType(IndentedTextWriter, ProjectionEmitContext, TypeSemantics)"/>
    /// <returns>A callback that writes the ABI type to the writer it's appended to.</returns>
    public static WriteAbiTypeCallback WriteAbiType(ProjectionEmitContext context, TypeSemantics semantics)
    {
        return new(context, semantics);
    }

    /// <summary>
    /// Returns the ABI C# type name for a fundamental type (e.g. <c>"bool"</c> -&gt; <c>"byte"</c>, <c>"char"</c> -&gt; <c>"ushort"</c>).
    /// </summary>
    internal static string GetAbiFundamentalType(FundamentalType t) => t switch
    {
        FundamentalType.Boolean => "bool",
        FundamentalType.Char => "char",
        FundamentalType.String => "void*",
        _ => FundamentalTypes.ToCSharpType(t)
    };
}
