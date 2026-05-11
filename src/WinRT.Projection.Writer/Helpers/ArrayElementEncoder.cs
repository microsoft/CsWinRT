// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Encodes WinRT array element type names for use in ABI marshaller paths (e.g. 'Int32', 'NullableUInt32', 'Single_RGB_BlueGreenRed_Iface').
/// </summary>
internal static class ArrayElementEncoder
{
    /// <summary>
    /// Returns the interop assembly path for an array marshaller of a given element type.
    /// The interop generator names array marshallers <c>ABI.&lt;typeNamespace&gt;.&lt;&lt;assembly&gt;ElementName&gt;ArrayMarshaller</c>
    /// (typeNamespace prefix outside the brackets, and the element inside the brackets uses just the
    /// type name without its namespace because depth=0 in the interop generator's AppendRawTypeName).
    /// </summary>
    internal static string GetArrayMarshallerInteropPath(TypeSignature elementType)
    {
        // The 'encodedElement' passed in uses the depth>0 form (assembly + hyphenated namespace + name),
        // but inside the array brackets the interop generator uses the depth=0 form (assembly + just name).
        // Re-encode the element with the top-level form for accurate matching.
        string topLevelElement = EncodeArrayElementName(elementType);
        // Resolve the element's namespace to determine the path prefix.
        string ns = AbiTypeHelpers.GetMappedNamespace(elementType);
        if (string.IsNullOrEmpty(ns))
        {
            return "ABI.<" + topLevelElement + ">ArrayMarshaller, WinRT.Interop";
        }
        return "ABI." + ns + ".<" + topLevelElement + ">ArrayMarshaller, WinRT.Interop";
    }

    /// <summary>
    /// Encodes the array element type name as the interop generator's AppendRawTypeName at depth=0:
    /// fundamentals use their short C# name; typedefs use just the type name (no namespace) prefixed
    /// with the assembly marker; generic instances include their assembly marker, name, and type arguments.
    /// </summary>
    private static string EncodeArrayElementName(TypeSignature elementType)
    {
        System.Text.StringBuilder sb = new();
        EncodeArrayElementNameInto(sb, elementType);
        return sb.ToString();
    }

    private static void EncodeArrayElementNameInto(System.Text.StringBuilder sb, TypeSignature sig)
    {
        // Special case for System.Guid: the depth=0 (top-level array element) form drops the
        // namespace prefix and uses just the assembly marker + type name, so for Guid this
        // becomes "<#corlib>Guid".
        if (sig is TypeDefOrRefSignature gtd
            && gtd.Type?.Namespace?.Value == "System"
            && gtd.Type?.Name?.Value == "Guid")
        {
            _ = sb.Append("<#corlib>Guid");
            return;
        }
        switch (sig)
        {
            case CorLibTypeSignature corlib:
                InteropTypeNameWriter.EncodeFundamental(sb, corlib, TypedefNameType.Projected);
                return;
            case TypeDefOrRefSignature td:
                EncodeArrayElementForTypeDef(sb, td.Type, generic_args: null);
                return;
            case GenericInstanceTypeSignature gi:
                EncodeArrayElementForTypeDef(sb, gi.GenericType, generic_args: gi.TypeArguments);
                return;
            default:
                _ = sb.Append(sig.FullName);
                return;
        }
    }

    private static void EncodeArrayElementForTypeDef(System.Text.StringBuilder sb, ITypeDefOrRef type, System.Collections.Generic.IList<TypeSignature>? generic_args)
    {
        (string typeNs, string typeName) = type.Names();
        // Apply mapped-type remapping (e.g. Windows.Foundation.IReference -> System.Nullable).
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);
        if (mapped is not null)
        {
            typeNs = mapped.MappedNamespace;
            typeName = mapped.MappedName;
        }
        // Replace generic arity backtick with apostrophe.
        typeName = typeName.Replace('`', '\'');

        // Assembly marker prefix. Pass the type so that third-party (e.g. component-authored)
        // types resolve to their actual assembly name (e.g. <AuthoringTest>) instead of
        // defaulting to <#Windows>.
        _ = sb.Append(InteropTypeNameWriter.GetInteropAssemblyMarker(typeNs, typeName, mapped, type));
        // Top-level: just the type name (no namespace).
        _ = sb.Append(typeName);

        // Generic arguments use the standard EncodeInteropTypeNameInto (depth > 0).
        if (generic_args is { Count: > 0 })
        {
            _ = sb.Append('<');
            for (int i = 0; i < generic_args.Count; i++)
            {
                if (i > 0) { _ = sb.Append('|'); }
                InteropTypeNameWriter.EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
            }
            _ = sb.Append('>');
        }
    }
}
