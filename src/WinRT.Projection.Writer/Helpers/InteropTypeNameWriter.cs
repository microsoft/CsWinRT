// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Metadata;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Helpers;

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
        // Special case for System.Guid: emitted with assembly-qualified form.
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
            case ElementType.Object:
                _ = nameType == TypedefNameType.Projected
                    ? sb.Append("object")
                    : sb.Append("ABI.System.<object>");
                return;
            case ElementType.Boolean: _ = sb.Append("bool"); return;
            case ElementType.Char: _ = sb.Append("char"); return;
            case ElementType.I1: _ = sb.Append("sbyte"); return;
            case ElementType.U1: _ = sb.Append("byte"); return;
            case ElementType.I2: _ = sb.Append("short"); return;
            case ElementType.U2: _ = sb.Append("ushort"); return;
            case ElementType.I4: _ = sb.Append("int"); return;
            case ElementType.U4: _ = sb.Append("uint"); return;
            case ElementType.I8: _ = sb.Append("long"); return;
            case ElementType.U8: _ = sb.Append("ulong"); return;
            case ElementType.R4: _ = sb.Append("float"); return;
            case ElementType.R8: _ = sb.Append("double"); return;
            case ElementType.String:
                _ = sb.Append("string");
                return;
        }
        _ = sb.Append(corlib.FullName);
    }

    private static void EncodeForTypeDef(StringBuilder sb, ITypeDefOrRef type, TypedefNameType nameType, IList<TypeSignature>? generic_args)
    {
        (string typeNs, string typeName) = type.Names();

        bool isAbi = nameType is not (TypedefNameType.Projected or TypedefNameType.InteropIID);

        if (isAbi)
        {
            _ = sb.Append("ABI.");
        }

        // Special case for EventSource on Windows.Foundation event-handler delegate types
        // (e.g. EventHandler<T>, TypedEventHandler<S,R>).
        if (nameType == TypedefNameType.EventSource && typeNs == WindowsFoundation)
        {
            // Determine generic arity from the .winmd type name (e.g. "EventHandler`1" => 1).
            int arity = 0;
            int tickIdx = typeName.IndexOf('`');

            if (tickIdx >= 0 && int.TryParse(typeName.AsSpan(tickIdx + 1), out int parsed))
            {
                arity = parsed;
            }

            _ = sb.Append("WindowsRuntime.InteropServices.<#CsWinRT>EventHandlerEventSource'");
            _ = sb.Append(arity.ToString(CultureInfo.InvariantCulture));
            // Append the generic args (if any).
            if (generic_args is { Count: > 0 })
            {
                _ = sb.Append('<');
                for (int i = 0; i < generic_args.Count; i++)
                {
                    if (i > 0)
                    {
                        _ = sb.Append('|');
                    }

                    EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
                }
                _ = sb.Append('>');
            }

            return;
        }

        // Apply mapped-type remapping
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);

        if (mapped is { } m)
        {
            typeNs = m.MappedNamespace;
            typeName = m.MappedName;
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
                if (i > 0)
                {
                    _ = sb.Append('|');
                }

                EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
            }
            _ = sb.Append('>');
        }

        // Append the type-kind suffix (e.g. "Methods" for the static ABI methods class).
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
        if (mapped is { } m)
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
            if (!m.EmitAbi)
            {
                return "<#CsWinRT>";
            }

            if (typeNs.StartsWith("Windows", StringComparison.Ordinal))
            {
                // Unreachable in practice: no standard mapped type maps to a Windows.* namespace
                // with EmitAbi=true. Emit '<#Windows>' so any future addition that hits this
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
                // Replace '.' with '-' for the assembly tag (e.g. "WinRT.Interop" -> "WinRT-Interop").
                string hyphenated = asmName.Replace('.', '-');
                return "<" + hyphenated + ">";
            }
        }

        return "<#Windows>";
    }

    /// <summary>
    /// Returns whether the type lives in <c>System.ObjectModel</c> and is one of the recognized mapped types (used by interop type-name encoding).
    /// </summary>
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

    /// <summary>
    /// Returns whether the type lives in <c>System.Numerics.Vectors</c> and is one of the recognized mapped types (used by interop type-name encoding).
    /// </summary>
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
