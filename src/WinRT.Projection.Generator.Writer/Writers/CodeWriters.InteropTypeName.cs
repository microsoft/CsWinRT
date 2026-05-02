// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Encoder for the WinRT.Interop assembly type name format used in <c>UnsafeAccessor</c>
/// attributes (e.g. <c>"ABI.System.Collections.Generic.&lt;#corlib&gt;IReadOnlyDictionary'2&lt;string|object&gt;Marshaller, WinRT.Interop"</c>).
/// Mirrors the C++ helpers <c>write_interop_assembly_name</c>, <c>write_interop_dll_type_name</c>,
/// and <c>write_interop_dll_type_name_for_typedef</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Encodes a TypeSignature using the WinRT.Interop name format. Used as the value of an
    /// <c>UnsafeAccessorType</c> attribute argument.
    /// </summary>
    /// <param name="sig">The type signature to encode.</param>
    /// <param name="nameType">Indicates whether to use the projected (no ABI prefix) form or
    /// the ABI-prefixed marshaller form.</param>
    public static string EncodeInteropTypeName(TypeSignature sig, TypedefNameType nameType)
    {
        StringBuilder sb = new();
        EncodeInteropTypeNameInto(sb, sig, nameType);
        return sb.ToString();
    }

    private static void EncodeInteropTypeNameInto(StringBuilder sb, TypeSignature sig, TypedefNameType nameType)
    {
        switch (sig)
        {
            case CorLibTypeSignature corlib:
                EncodeFundamental(sb, corlib, nameType);
                return;
            case TypeDefOrRefSignature td:
                EncodeForTypeDef(sb, td.Type, nameType, generic_args: null);
                return;
            case GenericInstanceTypeSignature gi:
                EncodeForTypeDef(sb, gi.GenericType, nameType, generic_args: gi.TypeArguments);
                return;
            case SzArrayTypeSignature sz:
                if (nameType == TypedefNameType.Projected)
                {
                    EncodeInteropTypeNameInto(sb, sz.BaseType, TypedefNameType.Projected);
                }
                else
                {
                    sb.Append("ABI.System.<");
                    EncodeInteropTypeNameInto(sb, sz.BaseType, TypedefNameType.Projected);
                    sb.Append(">");
                }
                return;
            case ByReferenceTypeSignature br:
                EncodeInteropTypeNameInto(sb, br.BaseType, nameType);
                return;
            case CustomModifierTypeSignature cm:
                EncodeInteropTypeNameInto(sb, cm.BaseType, nameType);
                return;
            default:
                sb.Append(sig.FullName);
                return;
        }
    }

    private static void EncodeFundamental(StringBuilder sb, CorLibTypeSignature corlib, TypedefNameType nameType)
    {
        switch (corlib.ElementType)
        {
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object:
                if (nameType == TypedefNameType.Projected) { sb.Append("object"); }
                else { sb.Append("ABI.System.<object>"); }
                return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean: sb.Append("bool"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char: sb.Append("char"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1: sb.Append("sbyte"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1: sb.Append("byte"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2: sb.Append("short"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2: sb.Append("ushort"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4: sb.Append("int"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4: sb.Append("uint"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8: sb.Append("long"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8: sb.Append("ulong"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4: sb.Append("float"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8: sb.Append("double"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String:
                sb.Append("string");
                return;
        }
        sb.Append(corlib.FullName);
    }

    private static void EncodeForTypeDef(StringBuilder sb, ITypeDefOrRef type, TypedefNameType nameType, System.Collections.Generic.IList<TypeSignature>? generic_args)
    {
        string typeNs = type.Namespace?.Value ?? string.Empty;
        string typeName = type.Name?.Value ?? string.Empty;

        bool isAbi = nameType != TypedefNameType.Projected && nameType != TypedefNameType.InteropIID;
        if (isAbi) { sb.Append("ABI."); }

        // Special case for EventSource on Windows.Foundation event-handler delegate types
        // (e.g. EventHandler<T>, TypedEventHandler<S,R>). Mirrors C++:
        //     ABI.WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'<arity>
        // Note the namespace check uses the ORIGINAL .winmd namespace (before mapping).
        if (nameType == TypedefNameType.EventSource && typeNs == "Windows.Foundation")
        {
            // Determine generic arity from the .winmd type name (e.g. "EventHandler`1" => 1).
            int arity = 0;
            int tickIdx = typeName.IndexOf('`');
            if (tickIdx >= 0 && int.TryParse(typeName.AsSpan(tickIdx + 1), out int parsed))
            {
                arity = parsed;
            }
            sb.Append("WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'");
            sb.Append(arity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            // Append the generic args (if any).
            if (generic_args is { Count: > 0 })
            {
                sb.Append('<');
                for (int i = 0; i < generic_args.Count; i++)
                {
                    if (i > 0) { sb.Append('|'); }
                    EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
                }
                sb.Append('>');
            }
            return;
        }

        // Apply mapped-type remapping
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);
        if (mapped is not null)
        {
            typeNs = mapped.MappedNamespace;
            typeName = mapped.MappedName;
        }
        // Replace generic arity backtick with apostrophe.
        typeName = typeName.Replace('`', '\'');

        if (nameType == TypedefNameType.InteropIID)
        {
            sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped));
            sb.Append(typeName);
        }
        else if (nameType == TypedefNameType.Projected)
        {
            // Replace namespace separator with - within the generic.
            string nsHyphenated = typeNs.Replace('.', '-');
            sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped));
            sb.Append(nsHyphenated);
            sb.Append('-');
            sb.Append(typeName);
        }
        else
        {
            sb.Append(typeNs);
            sb.Append('.');
            sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped));
            sb.Append(typeName);
        }

        if (generic_args is { Count: > 0 })
        {
            sb.Append('<');
            for (int i = 0; i < generic_args.Count; i++)
            {
                if (i > 0) { sb.Append('|'); }
                EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
            }
            sb.Append('>');
        }

        // Append the type-kind suffix (matches C++ write_interop_dll_type_name_for_typedef).
        if (nameType == TypedefNameType.StaticAbiClass)
        {
            sb.Append("Methods");
        }
        else if (nameType == TypedefNameType.ABI)
        {
            sb.Append("Marshaller");
        }
        else if (nameType == TypedefNameType.EventSource)
        {
            sb.Append("EventSource");
        }
    }

    /// <summary>
    /// Returns the assembly marker (e.g. <c>&lt;#corlib&gt;</c>) for a (possibly remapped)
    /// type/namespace. Mirrors C++ <c>write_interop_assembly_name</c>.
    /// </summary>
    private static string GetInteropAssemblyMarker(string typeNs, string typeName, MappedType? mapped)
    {
        if (mapped is not null)
        {
            // Mapped type — check the target namespace to decide marker.
            if (typeNs.StartsWith("System.Numerics", StringComparison.Ordinal))
            {
                return "<System-Numerics-Vectors>";
            }
            if (typeNs == "System.Collections.ObjectModel")
            {
                return "<System-ObjectModel>";
            }
            if (typeNs.StartsWith("System", StringComparison.Ordinal))
            {
                return "<#corlib>";
            }
            // Mapped to a non-System namespace (e.g. Windows.Foundation.IClosable would map back
            // to itself but with EmitAbi=false, etc.) — defer to <#CsWinRT> marker for simplicity.
            return "<#CsWinRT>";
        }
        // Unmapped type — assume Windows.* namespace from the Windows projection assembly.
        if (typeNs.StartsWith("Windows.", StringComparison.Ordinal) || typeNs == "Windows")
        {
            return "<#Windows>";
        }
        if (typeNs.StartsWith("WindowsRuntime", StringComparison.Ordinal))
        {
            return "<#CsWinRT>";
        }
        // Default: use the type's assembly name. We don't have a stable handle on this from the
        // type alone, so fall back to <#Windows> to match the most common case.
        return "<#Windows>";
    }
}
