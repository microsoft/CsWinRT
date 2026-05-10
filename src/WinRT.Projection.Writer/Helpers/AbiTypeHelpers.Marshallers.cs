// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;
using System;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;
using static WindowsRuntime.ProjectionWriter.References.WellKnownTypeNames;

using WindowsRuntime.ProjectionWriter.Generation;

namespace WindowsRuntime.ProjectionWriter.Helpers;

internal static partial class AbiTypeHelpers
{
    /// <summary>True if the type signature is a Nullable&lt;T&gt; where T is a primitive
    /// supported by an ABI.System.&lt;T&gt;Marshaller (e.g. UInt64Marshaller, Int32Marshaller, etc.).
    /// Returns the fully-qualified marshaller name in <paramref name="marshallerName"/>.</summary>
    internal static bool TryGetNullablePrimitiveMarshallerName(TypeSignature sig, out string? marshallerName)
    {
        marshallerName = null;
        if (sig is not GenericInstanceTypeSignature gi) { return false; }
        ITypeDefOrRef gt = gi.GenericType;
        string ns = gt?.Namespace?.Value ?? string.Empty;
        string name = gt?.Name?.Value ?? string.Empty;
        // In WinMD metadata, Nullable<T> is encoded as Windows.Foundation.IReference<T>.
        // It only later gets projected to System.Nullable<T> by the projection layer.
        bool isNullable = (ns == "System" && name == NullableGeneric)
            || (ns == WindowsFoundation && name == IReferenceGeneric);
        if (!isNullable) { return false; }
        if (gi.TypeArguments.Count != 1) { return false; }
        TypeSignature arg = gi.TypeArguments[0];
        // Map primitive corlib element type to its ABI marshaller name.
        if (arg is CorLibTypeSignature corlib)
        {
            string? mn = corlib.ElementType switch
            {
                ElementType.Boolean => "Boolean",
                ElementType.Char => "Char",
                ElementType.I1 => "SByte",
                ElementType.U1 => "Byte",
                ElementType.I2 => "Int16",
                ElementType.U2 => "UInt16",
                ElementType.I4 => "Int32",
                ElementType.U4 => "UInt32",
                ElementType.I8 => "Int64",
                ElementType.U8 => "UInt64",
                ElementType.R4 => "Single",
                ElementType.R8 => "Double",
                _ => null
            };
            if (mn is null) { return false; }
            marshallerName = AbiPrefix + "System." + mn + MarshallerSuffix;
            return true;
        }
        return false;
    }

    /// <summary>Returns the marshaller name for the inner type T of <c>Nullable&lt;T&gt;</c>.
    ///.: e.g. for <c>Nullable&lt;DateTimeOffset&gt;</c> returns
    /// <c>global::ABI.System.DateTimeOffsetMarshaller</c>; for primitives like <c>Nullable&lt;int&gt;</c>
    /// returns <c>global::ABI.System.Int32Marshaller</c>.</summary>
    internal static string GetNullableInnerMarshallerName(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature innerType)
    {
        // Primitives (Int32, Int64, Boolean, etc.) live in ABI.System with the canonical .NET name.
        if (innerType is CorLibTypeSignature corlib)
        {
            string typeName = corlib.ElementType switch
            {
                ElementType.Boolean => "Boolean",
                ElementType.Char => "Char",
                ElementType.I1 => "SByte",
                ElementType.U1 => "Byte",
                ElementType.I2 => "Int16",
                ElementType.U2 => "UInt16",
                ElementType.I4 => "Int32",
                ElementType.U4 => "UInt32",
                ElementType.I8 => "Int64",
                ElementType.U8 => "UInt64",
                ElementType.R4 => "Single",
                ElementType.R8 => "Double",
                _ => "",
            };
            if (!string.IsNullOrEmpty(typeName))
            {
                return GlobalAbiPrefix + "System." + typeName + MarshallerSuffix;
            }
        }
        // For non-primitive types (DateTimeOffset, TimeSpan, struct/enum types), use GetMarshallerFullName.
        return GetMarshallerFullName(writer, context, innerType);
    }

    /// <summary>Returns the full marshaller name (e.g. <c>global::ABI.Windows.Foundation.UriMarshaller</c>).
    /// When the marshaller would land in the writer's current ABI namespace, returns just the
    /// short marshaller class name (e.g. <c>BasicStructMarshaller</c>) —.
    /// elides the qualifier in same-namespace contexts.</summary>
    internal static string GetMarshallerFullName(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature sig)
    {
        if (sig is TypeDefOrRefSignature td)
        {
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            // Apply mapped type remapping (e.g. System.Uri -> Windows.Foundation.Uri)
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            string nameStripped = IdentifierEscaping.StripBackticks(name);
            // If the writer is currently in the matching ABI namespace, drop the qualifier.
            if (context.InAbiNamespace && string.Equals(context.CurrentNamespace, ns, StringComparison.Ordinal))
            {
                return nameStripped + MarshallerSuffix;
            }
            return GlobalAbiPrefix + ns + "." + nameStripped + MarshallerSuffix;
        }
        return "global::ABI.Object.Marshaller";
    }
}
