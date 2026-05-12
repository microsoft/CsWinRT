// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

internal static partial class AbiTypeHelpers
{
    /// <summary>
    /// Returns the ABI type name for a blittable struct (the projected type name).
    /// </summary>
    internal static string GetBlittableStructAbiType(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature sig)
    {
        // Mapped value types (DateTime/TimeSpan) use the ABI type, not the projected type.
        if (IsMappedAbiValueType(sig))
        {
            return GetMappedAbiTypeName(sig);
        }

        string result = MethodFactory.WriteProjectedSignature(context, sig, false);
        return result;
    }

    /// <summary>Returns the ABI struct type name for a complex struct (e.g. global::ABI.Windows.Web.Http.HttpProgress).
    /// When the writer is currently in the matching ABI namespace, returns just the
    /// short type name (e.g. <c>HttpProgress</c>) to mirror the original code which uses the
    /// unqualified name in same-namespace contexts.</summary>
    internal static string GetAbiStructTypeName(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature sig)
    {
        if (sig is TypeDefOrRefSignature td)
        {
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            // If this struct is mapped, use the mapped namespace+name (e.g.
            // 'Windows.UI.Xaml.Interop.TypeName' is mapped to 'System.Type', so the ABI struct
            // is 'global::ABI.System.Type', not 'global::ABI.Windows.UI.Xaml.Interop.TypeName').
            MappedType? mapped = MappedTypes.Get(ns, name);

            if (mapped is { } m)
            {
                ns = m.MappedNamespace;
                name = m.MappedName;
            }

            return GlobalAbiPrefix + ns + "." + IdentifierEscaping.StripBackticks(name);
        }

        return "global::ABI.Object";
    }

    /// <summary>
    /// Returns the C# primitive keyword (e.g. <c>"bool"</c>, <c>"int"</c>) for an ABI corlib element type, or <see langword="null"/> when <paramref name="sig"/> is not a primitive.
    /// </summary>
    internal static string GetAbiPrimitiveType(MetadataCache cache, TypeSignature sig)
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
                def = cache.Find(ns + "." + name);
            }

            if (def is not null && TypeCategorization.GetCategory(def) == TypeCategory.Enum)
            {
                return cache is null ? "int" : GetProjectedEnumName(def);
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
        MappedType? mapped = MappedTypes.Get(ns, name);

        if (mapped is { } m)
        {
            ns = m.MappedNamespace;
            name = m.MappedName;
        }

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
