// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Encoder for the WinRT.Interop assembly type name format used in <c>UnsafeAccessor</c>
/// attributes (e.g. <c>"ABI.System.Collections.Generic.&lt;#corlib&gt;IReadOnlyDictionary'2&lt;string|object&gt;Marshaller, WinRT.Interop"</c>).
/// </summary>
internal static class InteropTypeNameWriter
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

    internal static void EncodeInteropTypeNameInto(StringBuilder sb, TypeSignature sig, TypedefNameType nameType)
    {
        // Special case for System.Guid: matches C++ guid_type case in write_interop_dll_type_name.
        if (sig is TypeDefOrRefSignature gtd
            && gtd.Type?.Namespace?.Value == "System"
            && gtd.Type?.Name?.Value == "Guid")
        {
            _ = nameType == TypedefNameType.Projected
                ? sb.Append("System-Guid")
                : sb.Append("ABI.System.<<#corlib>Guid>");
            return;
        }
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
                    _ = sb.Append("ABI.System.<");
                    EncodeInteropTypeNameInto(sb, sz.BaseType, TypedefNameType.Projected);
                    _ = sb.Append(">");
                }
                return;
            case ByReferenceTypeSignature br:
                EncodeInteropTypeNameInto(sb, br.BaseType, nameType);
                return;
            case CustomModifierTypeSignature cm:
                EncodeInteropTypeNameInto(sb, cm.BaseType, nameType);
                return;
            default:
                _ = sb.Append(sig.FullName);
                return;
        }
    }

    internal static void EncodeFundamental(StringBuilder sb, CorLibTypeSignature corlib, TypedefNameType nameType)
    {
        switch (corlib.ElementType)
        {
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object:
                _ = nameType == TypedefNameType.Projected
                    ? sb.Append("object")
                    : sb.Append("ABI.System.<object>");
                return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean: _ = sb.Append("bool"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char: _ = sb.Append("char"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1: _ = sb.Append("sbyte"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1: _ = sb.Append("byte"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2: _ = sb.Append("short"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2: _ = sb.Append("ushort"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4: _ = sb.Append("int"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4: _ = sb.Append("uint"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8: _ = sb.Append("long"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8: _ = sb.Append("ulong"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4: _ = sb.Append("float"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8: _ = sb.Append("double"); return;
            case AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String:
                _ = sb.Append("string");
                return;
        }
        _ = sb.Append(corlib.FullName);
    }

    private static void EncodeForTypeDef(StringBuilder sb, ITypeDefOrRef type, TypedefNameType nameType, System.Collections.Generic.IList<TypeSignature>? generic_args)
    {
        (string typeNs, string typeName) = type.Names();

        bool isAbi = nameType is not (TypedefNameType.Projected or TypedefNameType.InteropIID);
        if (isAbi) { _ = sb.Append("ABI."); }

        // Special case for EventSource on Windows.Foundation event-handler delegate types
        // (e.g. EventHandler<T>, TypedEventHandler<S,R>).
        if (nameType == TypedefNameType.EventSource && typeNs == "Windows.Foundation")
        {
            // Determine generic arity from the .winmd type name (e.g. "EventHandler`1" => 1).
            int arity = 0;
            int tickIdx = typeName.IndexOf('`');
            if (tickIdx >= 0 && int.TryParse(typeName.AsSpan(tickIdx + 1), out int parsed))
            {
                arity = parsed;
            }
            _ = sb.Append("WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'");
            _ = sb.Append(arity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            // Append the generic args (if any).
            if (generic_args is { Count: > 0 })
            {
                _ = sb.Append('<');
                for (int i = 0; i < generic_args.Count; i++)
                {
                    if (i > 0) { _ = sb.Append('|'); }
                    EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
                }
                _ = sb.Append('>');
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
            _ = sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped, type));
            _ = sb.Append(typeName);
        }
        else if (nameType == TypedefNameType.Projected)
        {
            // Replace namespace separator with - within the generic.
            string nsHyphenated = typeNs.Replace('.', '-');
            _ = sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped, type));
            _ = sb.Append(nsHyphenated);
            _ = sb.Append('-');
            _ = sb.Append(typeName);
        }
        else
        {
            _ = sb.Append(typeNs);
            _ = sb.Append('.');
            _ = sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped, type));
            _ = sb.Append(typeName);
        }

        if (generic_args is { Count: > 0 })
        {
            _ = sb.Append('<');
            for (int i = 0; i < generic_args.Count; i++)
            {
                if (i > 0) { _ = sb.Append('|'); }
                EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
            }
            _ = sb.Append('>');
        }

        // Append the type-kind suffix (matches C++ write_interop_dll_type_name_for_typedef).
        if (nameType == TypedefNameType.StaticAbiClass)
        {
            _ = sb.Append("Methods");
        }
        else if (nameType == TypedefNameType.ABI)
        {
            _ = sb.Append("Marshaller");
        }
        else if (nameType == TypedefNameType.EventSource)
        {
            _ = sb.Append("EventSource");
        }
    }

    /// <summary>
    /// Returns the assembly marker (e.g. <c>&lt;#corlib&gt;</c>) for a (possibly remapped)
    /// type/namespace.
    /// </summary>
    internal static string GetInteropAssemblyMarker(string typeNs, string typeName, MappedType? mapped, ITypeDefOrRef? type = null)
    {
        if (mapped is not null)
        {
            // determines the marker.
            if (typeNs.StartsWith("System", StringComparison.Ordinal))
            {
                if (IsMappedTypeInSystemNumericsVectors(typeNs))
                {
                    return "<System-Numerics-Vectors>";
                }
                if (IsMappedTypeInSystemObjectModel(typeNs, typeName))
                {
                    return "<System-ObjectModel>";
                }
                return "<#corlib>";
            }
            // Mapped to a non-System namespace.
            if (!mapped.EmitAbi)
            {
                return "<#CsWinRT>";
            }
            if (typeNs.StartsWith("Windows", StringComparison.Ordinal))
            {
                // unintended template placeholder in C++ that's unreachable in practice (no
                // standard mapped type maps to a Windows.* namespace with EmitAbi=true). We
                // emit the corrected '<#Windows>' so any future addition that hits this
                // branch produces a runtime-resolvable assembly marker rather than garbage.
                return "<#Windows>";
            }
        }
        // Unmapped type.
        if (typeNs.StartsWith("Windows.", StringComparison.Ordinal) || typeNs == "Windows")
        {
            return "<#Windows>";
        }
        if (typeNs.StartsWith("WindowsRuntime", StringComparison.Ordinal))
        {
            return "<#CsWinRT>";
        }
        // For any other type (e.g. user-authored components in third-party .winmd assemblies),
        // use the actual assembly name from the type's resolution scope..
        // uses the .winmd file stem (e.g. "AuthoringTest" for AuthoringTest.winmd).
        if (type is not null)
        {
            string? asmName = GetTypeAssemblyName(type);
            if (!string.IsNullOrEmpty(asmName))
            {
                // Replace '.' with '-' (matches C++ which does std::replace('.', '-')).
                string hyphenated = asmName.Replace('.', '-');
                return "<" + hyphenated + ">";
            }
        }
        return "<#Windows>";
    }

    /// <summary>Returns whether the type lives in <c>System.ObjectModel</c> and is one of the recognized mapped types (used by interop type-name encoding).</summary>
    private static bool IsMappedTypeInSystemObjectModel(string typeNs, string typeName)
    {
        if (typeNs == "System.Collections.Specialized")
        {
            return typeName is "INotifyCollectionChanged"
                or "NotifyCollectionChangedAction"
                or "NotifyCollectionChangedEventArgs"
                or "NotifyCollectionChangedEventHandler";
        }
        if (typeNs == "System.ComponentModel")
        {
            return typeName is "INotifyDataErrorInfo"
                or "INotifyPropertyChanged"
                or "DataErrorsChangedEventArgs"
                or "PropertyChangedEventArgs"
                or "PropertyChangedEventHandler";
        }
        if (typeNs == "System.Windows.Input")
        {
            return typeName == "ICommand";
        }
        return false;
    }

    /// <summary>Returns whether the type lives in <c>System.Numerics.Vectors</c> and is one of the recognized mapped types (used by interop type-name encoding).</summary>
    private static bool IsMappedTypeInSystemNumericsVectors(string typeNs)
    {
        return typeNs == "System.Numerics";
    }

    /// <summary>
    /// Resolves the assembly name (without extension) that defines a given type.
    /// </summary>
    private static string? GetTypeAssemblyName(ITypeDefOrRef type)
    {
        return type.Scope?.GetAssembly()?.Name?.Value;
    }
}