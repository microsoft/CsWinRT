// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.References;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

internal static partial class AbiTypeHelpers
{
    /// <summary>
    /// Returns the ABI C# type name for a local variable that holds the ABI value of a parameter
    /// or return type. Encapsulates the type-selection dispatch that's repeated across the RcwCaller
    /// out-parameter, ReceiveArray-element, and SzArray-return code paths: reference types
    /// (string/interface/object/generic instance) → <c>void*</c>; system type → <c>global::ABI.System.Type</c>;
    /// HResult/Exception → <c>global::ABI.System.Exception</c>; complex struct → ABI struct name;
    /// mapped value type (DateTime/TimeSpan/etc.) → mapped ABI name; blittable struct → projected
    /// name; primitive → fundamental C# keyword.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The type signature whose ABI local type name is requested.</param>
    /// <returns>The ABI C# type name as a string (no trailing punctuation).</returns>
    public static string GetAbiLocalTypeName(ProjectionEmitContext context, TypeSignature sig)
    {
        if (sig.IsAbiRefLike(context.AbiTypeKindResolver))
        {
            return "void*";
        }

        if (sig.IsSystemType())
        {
            return WellKnownAbiTypeNames.AbiSystemType;
        }

        if (sig.IsHResultException())
        {
            return WellKnownAbiTypeNames.AbiSystemException;
        }

        if (context.AbiTypeKindResolver.IsNonBlittableStruct(sig))
        {
            return GetAbiStructTypeName(context, sig);
        }

        if (IsMappedAbiValueType(sig))
        {
            return GetMappedAbiTypeName(sig);
        }

        if (context.AbiTypeKindResolver.IsBlittableStruct(sig))
        {
            return GetBlittableStructAbiType(context, sig);
        }

        return GetAbiPrimitiveType(context.Cache, sig);
    }

    /// <summary>
    /// Returns the ABI C# type name for a single SZ-array element appearing as the <c>data</c>
    /// parameter in array-marshaller <c>UnsafeAccessor</c> signatures (e.g. <c>ConvertToManaged_X</c>,
    /// <c>Free_X</c>). Dispatched by <see cref="Resolvers.AbiTypeKindResolver.ClassifyArrayElement"/>:
    /// reference-pointer → <c>void*</c>, HResult → <c>global::ABI.System.Exception</c>, mapped value
    /// type → mapped ABI name, complex struct → ABI struct name, blittable struct → blittable ABI
    /// struct, primitive → primitive ABI type.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="elementType">The SZ-array element type.</param>
    /// <returns>The ABI C# type name as a string (no trailing punctuation).</returns>
    public static string GetArrayElementAbiType(ProjectionEmitContext context, TypeSignature elementType)
    {
        return context.AbiTypeKindResolver.ClassifyArrayElement(elementType) switch
        {
            AbiArrayElementKind.RefLikeVoidStar => "void*",
            AbiArrayElementKind.HResultException => WellKnownAbiTypeNames.AbiSystemException,
            AbiArrayElementKind.MappedValueType => GetMappedAbiTypeName(elementType),
            AbiArrayElementKind.NonBlittableStruct => GetAbiStructTypeName(context, elementType),
            AbiArrayElementKind.BlittableStruct => GetBlittableStructAbiType(context, elementType),
            _ => GetAbiPrimitiveType(context.Cache, elementType),
        };
    }

    /// <summary>
    /// Returns the C# type name used for the <c>InlineArray16&lt;T&gt;</c> / <c>ArrayPool&lt;T&gt;</c>
    /// storage <c>T</c> backing a non-blittable PassArray / FillArray parameter. Strings,
    /// runtime classes, and objects all collapse to <c>nint</c> (HSTRING handles / IInspectable
    /// pointers); HResult uses <c>global::ABI.System.Exception</c>; mapped value types and
    /// complex structs use their ABI struct name.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="elementType">The SZ-array element type.</param>
    /// <returns>The storage C# type name as a string (no trailing punctuation).</returns>
    public static string GetArrayElementStorageType(ProjectionEmitContext context, TypeSignature elementType)
    {
        return context.AbiTypeKindResolver.ClassifyArrayElement(elementType) switch
        {
            AbiArrayElementKind.MappedValueType => GetMappedAbiTypeName(elementType),
            AbiArrayElementKind.NonBlittableStruct => GetAbiStructTypeName(context, elementType),
            AbiArrayElementKind.HResultException => WellKnownAbiTypeNames.AbiSystemException,
            _ => "nint",
        };
    }

    /// <summary>
    /// Returns the ABI type name for a blittable struct (the projected type name; mapped value
    /// types like <c>DateTime</c> / <c>TimeSpan</c> return their mapped ABI name instead).
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The blittable struct type signature.</param>
    /// <returns>The ABI C# type name as a string (no trailing punctuation).</returns>
    public static string GetBlittableStructAbiType(ProjectionEmitContext context, TypeSignature sig)
    {
        // Mapped value types (DateTime/TimeSpan) use the ABI type, not the projected type.
        if (IsMappedAbiValueType(sig))
        {
            return GetMappedAbiTypeName(sig);
        }

        string result = MethodFactory.WriteProjectedSignature(context, sig, false).Format();
        return result;
    }

    /// <summary>
    /// Returns the ABI struct type name for a complex struct
    /// (e.g. <c>global::ABI.Windows.Web.Http.HttpProgress</c>). Mapped structs route through
    /// their mapped namespace+name (e.g. <c>Windows.UI.Xaml.Interop.TypeName</c> →
    /// <c>global::ABI.System.Type</c>).
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The complex struct type signature.</param>
    /// <returns>The ABI C# type name as a string (no trailing punctuation).</returns>
    public static string GetAbiStructTypeName(ProjectionEmitContext context, TypeSignature sig)
    {
        if (sig is TypeDefOrRefSignature td)
        {
            (string ns, string name) = td.Type.Names();

            // If this struct is mapped, use the mapped namespace+name (e.g.
            // 'Windows.UI.Xaml.Interop.TypeName' is mapped to 'System.Type', so the ABI struct
            // is 'global::ABI.System.Type', not 'global::ABI.Windows.UI.Xaml.Interop.TypeName').
            _ = MappedTypes.ApplyMapping(ref ns, ref name);

            return GlobalAbiPrefix + ns + "." + IdentifierEscaping.StripBackticks(name);
        }

        return "global::ABI.Object";
    }

    /// <summary>
    /// Returns the C# primitive keyword (e.g. <c>"bool"</c>, <c>"int"</c>) for an ABI corlib element type, or <see langword="null"/> when <paramref name="sig"/> is not a primitive.
    /// </summary>
    public static string GetAbiPrimitiveType(MetadataCache cache, TypeSignature sig)
    {
        if (sig is CorLibTypeSignature corlib)
        {
            return corlib.ElementType switch
            {
                ElementType.Boolean => "bool",
                ElementType.Char => "char",
                _ => GetAbiFundamentalTypeFromCorLib(corlib.ElementType),
            };
        }

        // Enum: use the projected enum type as the ABI signature
        if (sig is TypeDefOrRefSignature td)
        {
            TypeDefinition? def = td.Type as TypeDefinition;

            if (def is null && td.Type is TypeReference tr)
            {
                (string ns, string name) = tr.Names();
                def = cache.Find(ns, name);
            }

            if (def is not null && def.IsEnum)
            {
                return GetProjectedEnumName(def);
            }
        }

        return "int";
    }

    private static string GetProjectedEnumName(TypeDefinition def)
    {
        (string ns, string name) = def.Names();

        // Apply mapped-type translation so consumers see the projected (.NET) enum name
        // (e.g. Windows.UI.Xaml.Interop.NotifyCollectionChangedAction →
        // System.Collections.Specialized.NotifyCollectionChangedAction). Same
        // remapping that WriteTypedefName performs.
        _ = MappedTypes.ApplyMapping(ref ns, ref name);

        return string.IsNullOrEmpty(ns) ? GlobalPrefix + name : GlobalPrefix + ns + "." + name;
    }

    private static string GetAbiFundamentalTypeFromCorLib(ElementType et)
    {
        return et switch
        {
            ElementType.I1 => "sbyte",
            ElementType.U1 => "byte",
            ElementType.I2 => "short",
            ElementType.U2 => "ushort",
            ElementType.I4 => "int",
            ElementType.U4 => "uint",
            ElementType.I8 => "long",
            ElementType.U8 => "ulong",
            ElementType.R4 => "float",
            ElementType.R8 => "double",
            _ => "int",
        };
    }
}
