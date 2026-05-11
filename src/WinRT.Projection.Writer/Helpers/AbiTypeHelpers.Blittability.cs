// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter.Helpers;

internal static partial class AbiTypeHelpers
{
    /// <summary>
    /// Returns whether the given type can be passed across the ABI boundary without per-field marshalling (struct layout matches the ABI representation).
    /// </summary>
    public static bool IsTypeBlittable(MetadataCache cache, TypeDefinition type)
    {
        TypeCategory cat = TypeCategorization.GetCategory(type);

        if (cat == TypeCategory.Enum)
        {
            return true;
        }

        if (cat != TypeCategory.Struct)
        {
            return false;
        }

        // struct itself has a mapped-type entry, return based on its RequiresMarshaling flag
        // BEFORE walking fields. This is critical for XAML structs like Duration / KeyTime /
        // RepeatBehavior which are self-mapped with RequiresMarshaling=false but have a
        // TimeSpan field (Windows.Foundation.TimeSpan -> System.TimeSpan with RequiresMarshaling=true).
        // Without this check, the field walk would incorrectly classify them as non-blittable.
        (string ns, string name) = type.Names();

        if (MappedTypes.Get(ns, name) is { } mapping)
        {
            return !mapping.RequiresMarshaling;
        }

        // Walk fields - all must be blittable
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null)
            {
                continue;
            }

            if (!IsFieldTypeBlittable(cache, field.Signature.FieldType))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns whether <paramref name="sig"/>, treated as a struct-field type, is blittable
    /// at the WinRT ABI. Used by <see cref="IsTypeBlittable"/> to walk struct fields.
    /// </summary>
    /// <param name="cache">The metadata cache used to resolve cross-module references.</param>
    /// <param name="sig">The field signature to test.</param>
    /// <returns><see langword="true"/> if the field's storage layout matches its ABI layout.</returns>
    internal static bool IsFieldTypeBlittable(MetadataCache cache, TypeSignature sig)
    {
        if (sig is CorLibTypeSignature corlib)
        {
            // ALL fundamentals (including Boolean, Char) are considered blittable here;
            // only String is non-blittable. Object is not a fundamental — it's handled below.
            return corlib.ElementType switch
            {
                ElementType.String => false,
                ElementType.Object => false,
                _ => true
            };
        }

        // For TypeRef/TypeDef, resolve and check blittability.
        if (sig is TypeDefOrRefSignature todr)
        {
            string fNs = todr.Type?.Namespace?.Value ?? string.Empty;
            string fName = todr.Type?.Name?.Value ?? string.Empty;
            // System.Guid is a fundamental blittable type .
            // Same applies to System.IntPtr / UIntPtr (used in some struct layouts).
            if (fNs == "System" && (fName is "Guid" or "IntPtr" || fName == "UIntPtr"))
            {
                return true;
            }

            // Mapped struct types: blittable iff the mapping does NOT require marshalling
            MappedType? mapped = MappedTypes.Get(fNs, fName);

            if (mapped is { RequiresMarshaling: true })
            {
                return false;
            }

            if (todr.Type is TypeDefinition td)
            {
                return IsTypeBlittable(cache, td);
            }

            // Cross-module: try metadata cache.
            if (todr.Type is TypeReference tr)
            {
                (string ns, string name) = tr.Names();
                TypeDefinition? resolved = cache.Find(ns + "." + name);

                if (resolved is not null)
                {
                    return IsTypeBlittable(cache, resolved);
                }
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// Resolves a <see cref="AsmResolver.DotNet.Signatures.TypeDefOrRefSignature"/> to its
    /// <see cref="TypeDefinition"/>, handling both in-assembly (already a TypeDefinition) and
    /// cross-assembly/TypeRef-row references via the metadata cache. Returns <c>null</c> when
    /// the reference cannot be resolved.
    /// </summary>
    internal static TypeDefinition? TryResolveStructTypeDef(MetadataCache cache, TypeDefOrRefSignature tdr)
    {
        if (tdr.Type is TypeDefinition td)
        {
            return td;
        }

        if (tdr.Type is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            return cache.Find(ns + "." + name);
        }

        return null;
    }

    /// <summary>
    /// True if the type signature represents an enum (resolves cross-module typerefs).
    /// </summary>
    internal static bool IsEnumType(MetadataCache cache, TypeSignature sig)
    {
        if (sig is not TypeDefOrRefSignature td)
        {
            return false;
        }

        if (td.Type is TypeDefinition def)
        {
            return TypeCategorization.GetCategory(def) == TypeCategory.Enum;
        }

        if (td.Type is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            TypeDefinition? resolved = cache.Find(ns + "." + name);
            return resolved is not null && TypeCategorization.GetCategory(resolved) == TypeCategory.Enum;
        }

        return false;
    }

    /// <summary>
    /// True if the type signature represents a WinRT runtime class, interface, or delegate (reference type marshallable via *Marshaller).
    /// </summary>
    internal static bool IsRuntimeClassOrInterface(MetadataCache cache, TypeSignature sig)
    {
        if (sig is TypeDefOrRefSignature td)
        {
            // Same-module: use the resolved category directly.
            if (td.Type is TypeDefinition def)
            {
                TypeCategory cat = TypeCategorization.GetCategory(def);
                return cat is TypeCategory.Class or TypeCategory.Interface or TypeCategory.Delegate;
            }

            // Cross-module typeref: try to resolve via the metadata cache to check category.
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;

            if (ns == "System")
            {
                return name switch
                {
                    "Uri" or "Type" or "IDisposable" or "Exception" => true,
                    _ => false,
                };
            }

            if (cache is not null)
            {
                TypeDefinition? resolved = cache.Find(ns + "." + name);

                if (resolved is not null)
                {
                    TypeCategory cat = TypeCategorization.GetCategory(resolved);
                    return cat is TypeCategory.Class or TypeCategory.Interface or TypeCategory.Delegate;
                }
            }

            // Unresolved cross-assembly TypeRef (e.g. a referenced winmd we don't have loaded).
            // Fall back to the signature's encoding: WinRT metadata distinguishes value types
            // (encoded as ValueType) from reference types (encoded as Class). If the signature
            // has IsValueType == false, then it MUST be one of class/interface/delegate (since
            // primitives/enums/strings/object are encoded with their own element type). This
            // mirrors how the original code's abi_marshaler abstraction handles unknown types — it
            // dispatches based on the metadata semantics, not on resolution.
            return !td.IsValueType;
        }

        return false;
    }

    /// <summary>True if the type is a blittable primitive (or enum) directly representable
    /// at the ABI: bool/byte/sbyte/short/ushort/int/uint/long/ulong/float/double/char and enums.</summary>
    internal static bool IsBlittablePrimitive(MetadataCache cache, TypeSignature sig)
    {
        if (sig is CorLibTypeSignature corlib)
        {
            return corlib.ElementType is
                ElementType.Boolean or
                ElementType.I1 or
                ElementType.U1 or
                ElementType.I2 or
                ElementType.U2 or
                ElementType.I4 or
                ElementType.U4 or
                ElementType.I8 or
                ElementType.U8 or
                ElementType.R4 or
                ElementType.R8 or
                ElementType.Char;
        }

        // Enum (TypeDefOrRef-based value type with non-Object base) - same module or cross-module
        if (sig is TypeDefOrRefSignature td)
        {
            if (td.Type is TypeDefinition def && TypeCategorization.GetCategory(def) == TypeCategory.Enum)
            {
                return true;
            }

            // Cross-module enum: try to resolve via the metadata cache.
            if (td.Type is TypeReference tr)
            {
                (string ns, string name) = tr.Names();
                TypeDefinition? resolved = cache.Find(ns + "." + name);

                if (resolved is not null && TypeCategorization.GetCategory(resolved) == TypeCategory.Enum)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>True for any struct type that can be passed directly across the WinRT ABI
    /// (no per-field marshalling required). This includes blittable structs and "almost-blittable"
    /// structs that have only primitive fields like bool/char (whose C# layout matches the WinRT ABI).
    /// Excludes structs with reference type fields (string/object/runtime classes/etc.).</summary>
    /// <summary>True for structs that have at least one reference type field (string, generic
    /// instance Nullable&lt;T&gt;, etc.). These need per-field marshalling via the *Marshaller class
    /// (ConvertToUnmanaged/ConvertToManaged/Dispose).</summary>
    internal static bool IsComplexStruct(MetadataCache cache, TypeSignature sig)
    {
        if (sig is not TypeDefOrRefSignature td)
        {
            return false;
        }

        TypeDefinition? def = td.Type as TypeDefinition;

        if (def is null && td.Type is TypeReference tr)
        {
            (string ns, string name) = tr.Names();

            if (ns == "System" && name == "Guid")
            {
                return false;
            }

            def = cache.Find(ns + "." + name);
        }

        if (def is null)
        {
            return false;
        }

        TypeCategory cat = TypeCategorization.GetCategory(def);

        if (cat != TypeCategory.Struct)
        {
            return false;
        }

        // RequiresMarshaling, regardless of inner field layout. So for mapped types like
        // Duration, KeyTime, RepeatBehavior (RequiresMarshaling=false), they're never "complex".
        string sNs = td.Type?.Namespace?.Value ?? string.Empty;
        string sName = td.Type?.Name?.Value ?? string.Empty;
        MappedType? sMapped = MappedTypes.Get(sNs, sName);

        if (sMapped is not null)
        {
            return false;
        }

        // A struct is "complex" if it has any field that is not a blittable primitive nor an
        // almost-blittable struct (i.e. has a string/object/Nullable<T>/etc. field).
        foreach (FieldDefinition field in def.Fields)
        {
            if (field.IsStatic || field.Signature is null)
            {
                continue;
            }

            TypeSignature ft = field.Signature.FieldType;

            if (IsBlittablePrimitive(cache, ft))
            {
                continue;
            }

            if (IsAnyStruct(cache, ft))
            {
                continue;
            }

            return true;
        }
        return false;
    }

    internal static bool IsAnyStruct(MetadataCache cache, TypeSignature sig)
    {
        if (sig is not TypeDefOrRefSignature td)
        {
            return false;
        }

        TypeDefinition? def = td.Type as TypeDefinition;

        if (def is null && td.Type is TypeReference trEarly)
        {
            (string ns, string name) = trEarly.Names();

            if (ns == "System" && name == "Guid")
            {
                return true;
            }

            def = cache.Find(ns + "." + name);
        }

        if (def is null)
        {
            return false;
        }

        // Mapped struct types short-circuit based on the mapping's RequiresMarshaling flag
        // (only applies to actual structs, not mapped interfaces like IAsyncAction).
        if (TypeCategorization.GetCategory(def) == TypeCategory.Struct)
        {
            string sNs = td.Type?.Namespace?.Value ?? string.Empty;
            string sName = td.Type?.Name?.Value ?? string.Empty;
            MappedType? sMapped = MappedTypes.Get(sNs, sName);

            if (sMapped is { } sMappedVal)
            {
                return !sMappedVal.RequiresMarshaling;
            }
        }

        TypeCategory cat = TypeCategorization.GetCategory(def);

        if (cat != TypeCategory.Struct)
        {
            return false;
        }

        // Reject if any instance field is a reference type (string/object/runtime class/etc.).
        foreach (FieldDefinition field in def.Fields)
        {
            if (field.IsStatic || field.Signature is null)
            {
                continue;
            }

            TypeSignature ft = field.Signature.FieldType;

            if (ft is CorLibTypeSignature corlibField)
            {
                if (corlibField.ElementType is
                    ElementType.String or
                    ElementType.Object)
                { return false; }
                continue;
            }

            // Recurse: nested struct must also pass IsAnyStruct, otherwise reject.
            if (IsBlittablePrimitive(cache, ft))
            {
                continue;
            }

            if (IsAnyStruct(cache, ft))
            {
                continue;
            }

            return false;
        }
        return true;
    }
}
