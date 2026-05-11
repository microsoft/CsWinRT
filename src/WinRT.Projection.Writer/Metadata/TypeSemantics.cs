// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Errors;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Identifies a fundamental WinRT primitive type (those whose ABI representation matches a C#
/// primitive type, plus <see cref="System.String"/>).
/// </summary>
internal enum FundamentalType
{
    /// <summary><see cref="bool"/>.</summary>
    Boolean,

    /// <summary><see cref="char"/>.</summary>
    Char,

    /// <summary><see cref="sbyte"/>.</summary>
    Int8,

    /// <summary><see cref="byte"/>.</summary>
    UInt8,

    /// <summary><see cref="short"/>.</summary>
    Int16,

    /// <summary><see cref="ushort"/>.</summary>
    UInt16,

    /// <summary><see cref="int"/>.</summary>
    Int32,

    /// <summary><see cref="uint"/>.</summary>
    UInt32,

    /// <summary><see cref="long"/>.</summary>
    Int64,

    /// <summary><see cref="ulong"/>.</summary>
    UInt64,

    /// <summary><see cref="float"/>.</summary>
    Float,

    /// <summary><see cref="double"/>.</summary>
    Double,

    /// <summary><see cref="System.String"/>.</summary>
    String,
}

/// <summary>
/// Discriminated union of WinRT type semantics produced by <see cref="TypeSemanticsFactory"/>.
/// </summary>
internal abstract record TypeSemantics
{
    private TypeSemantics() { }

    /// <summary>
    /// A fundamental WinRT primitive (see <see cref="FundamentalType"/>).
    /// </summary>
    /// <param name="Type">The underlying fundamental type.</param>
    public sealed record Fundamental(FundamentalType Type) : TypeSemantics;

    /// <summary>
    /// The corlib <see cref="System.Object"/> type.
    /// </summary>
    public sealed record ObjectType : TypeSemantics;

    /// <summary>
    /// The corlib <see cref="System.Guid"/> type.
    /// </summary>
    public sealed record GuidType : TypeSemantics;

    /// <summary>
    /// The corlib <see cref="System.Type"/> type.
    /// </summary>
    public sealed record SystemType : TypeSemantics;

    /// <summary>
    /// A WinRT class / interface / struct / enum / delegate defined in the loaded metadata.
    /// </summary>
    /// <param name="Type">The type definition.</param>
    public sealed record Definition(TypeDefinition Type) : TypeSemantics;

    /// <summary>
    /// A closed generic instantiation whose generic type is resolved.
    /// </summary>
    /// <param name="GenericType">The open generic type definition.</param>
    /// <param name="GenericArgs">The instantiation arguments.</param>
    public sealed record GenericInstance(TypeDefinition GenericType, List<TypeSemantics> GenericArgs) : TypeSemantics;

    /// <summary>
    /// A closed generic instantiation whose generic type is referenced (cross-assembly).
    /// </summary>
    /// <param name="GenericType">The open generic type reference.</param>
    /// <param name="GenericArgs">The instantiation arguments.</param>
    public sealed record GenericInstanceRef(ITypeDefOrRef GenericType, List<TypeSemantics> GenericArgs) : TypeSemantics;

    /// <summary>
    /// A reference to a type generic parameter at the specified index.
    /// </summary>
    /// <param name="Index">The zero-based parameter index.</param>
    public sealed record GenericTypeIndex(int Index) : TypeSemantics;

    /// <summary>
    /// A reference to a method generic parameter at the specified index.
    /// </summary>
    /// <param name="Index">The zero-based parameter index.</param>
    public sealed record GenericMethodIndex(int Index) : TypeSemantics;

    /// <summary>
    /// A bound generic parameter token (rare; appears in nested generics).
    /// </summary>
    /// <param name="Parameter">The generic parameter.</param>
    public sealed record BoundGenericParameter(GenericParameter Parameter) : TypeSemantics;

    /// <summary>
    /// A reference to a type defined in another assembly.
    /// </summary>
    /// <param name="Type">The type reference.</param>
    /// <param name="IsValueType">Whether the reference points at a value type (struct/enum) or a reference type.</param>
    public sealed record Reference(TypeReference Type, bool IsValueType = false) : TypeSemantics;
}

/// <summary>
/// Static helpers for converting AsmResolver type signatures into <see cref="TypeSemantics"/> instances.
/// </summary>
internal static class TypeSemanticsFactory
{
    public static TypeSemantics Get(TypeSignature signature)
    {
        return signature switch
        {
            CorLibTypeSignature corlib => GetCorLib(corlib.ElementType),
            GenericInstanceTypeSignature gi => GetGenericInstance(gi),
            GenericParameterSignature gp => gp.ParameterType == GenericParameterType.Type
                ? new TypeSemantics.GenericTypeIndex(gp.Index)
                : new TypeSemantics.GenericMethodIndex(gp.Index),
            TypeDefOrRefSignature tdorref => GetFromTypeDefOrRef(tdorref.Type, tdorref.IsValueType),
            SzArrayTypeSignature sz => Get(sz.BaseType), // SZ arrays handled by callers
            ByReferenceTypeSignature br => Get(br.BaseType),
            _ => GetFromTypeDefOrRef(signature.GetUnderlyingTypeDefOrRef() ?? throw WellKnownProjectionWriterExceptions.UnsupportedTypeSignature(signature?.ToString() ?? "<null>")),
        };
    }

    public static TypeSemantics GetFromTypeDefOrRef(ITypeDefOrRef type, bool isValueType = false)
    {
        if (type is TypeDefinition def)
        {
            return new TypeSemantics.Definition(def);
        }

        if (type is TypeReference reference)
        {
            (string ns, string name) = reference.Names();

            if (ns == "System" && name == "Guid")
            {
                return new TypeSemantics.GuidType();
            }

            if (ns == "System" && name == "Object")
            {
                return new TypeSemantics.ObjectType();
            }

            if (ns == "System" && name == "Type")
            {
                return new TypeSemantics.SystemType();
            }

            return new TypeSemantics.Reference(reference, isValueType);
        }

        if (type is TypeSpecification spec && spec.Signature is GenericInstanceTypeSignature gi)
        {
            return GetGenericInstance(gi);
        }

        return new TypeSemantics.Reference((TypeReference)type, isValueType);
    }

    private static TypeSemantics GetCorLib(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.Boolean => new TypeSemantics.Fundamental(FundamentalType.Boolean),
            ElementType.Char => new TypeSemantics.Fundamental(FundamentalType.Char),
            ElementType.I1 => new TypeSemantics.Fundamental(FundamentalType.Int8),
            ElementType.U1 => new TypeSemantics.Fundamental(FundamentalType.UInt8),
            ElementType.I2 => new TypeSemantics.Fundamental(FundamentalType.Int16),
            ElementType.U2 => new TypeSemantics.Fundamental(FundamentalType.UInt16),
            ElementType.I4 => new TypeSemantics.Fundamental(FundamentalType.Int32),
            ElementType.U4 => new TypeSemantics.Fundamental(FundamentalType.UInt32),
            ElementType.I8 => new TypeSemantics.Fundamental(FundamentalType.Int64),
            ElementType.U8 => new TypeSemantics.Fundamental(FundamentalType.UInt64),
            ElementType.R4 => new TypeSemantics.Fundamental(FundamentalType.Float),
            ElementType.R8 => new TypeSemantics.Fundamental(FundamentalType.Double),
            ElementType.String => new TypeSemantics.Fundamental(FundamentalType.String),
            ElementType.Object => new TypeSemantics.ObjectType(),
            _ => throw WellKnownProjectionWriterExceptions.UnsupportedCorLibElementType(elementType)
        };
    }

    [SuppressMessage("Style", "IDE0028:Use collection expression",
        Justification = "List<TypeSemantics>(capacity) cannot be expressed as a collection expression.")]
    private static TypeSemantics GetGenericInstance(GenericInstanceTypeSignature gi)
    {
        ITypeDefOrRef genericType = gi.GenericType;
        // Always preserve the type arguments.
        List<TypeSemantics> args = new(gi.TypeArguments.Count);
        foreach (TypeSignature arg in gi.TypeArguments)
        {
            args.Add(Get(arg));
        }

        if (genericType is not TypeDefinition def)
        {
            // Wrap the generic-type reference along with the resolved type arguments.
            return new TypeSemantics.GenericInstanceRef(genericType, args);
        }

        return new TypeSemantics.GenericInstance(def, args);
    }
}

/// <summary>
/// Type-name kind,.
/// </summary>
internal enum TypedefNameType
{
    Projected,
    CCW,
    ABI,
    NonProjected,
    StaticAbiClass,
    EventSource,
    Marshaller,
    ArrayMarshaller,
    InteropIID,
}

/// <summary>
/// Maps the abstract <see cref="FundamentalType"/> enum (a closed set of WinRT
/// fundamental types: bool/char/numeric/string) to its projected C# type name and
/// .NET reflection name.
/// </summary>
internal static class FundamentalTypes
{
    public static string ToCSharpType(FundamentalType t) => t switch
    {
        FundamentalType.Boolean => "bool",
        FundamentalType.Char => "char",
        FundamentalType.Int8 => "sbyte",
        FundamentalType.UInt8 => "byte",
        FundamentalType.Int16 => "short",
        FundamentalType.UInt16 => "ushort",
        FundamentalType.Int32 => "int",
        FundamentalType.UInt32 => "uint",
        FundamentalType.Int64 => "long",
        FundamentalType.UInt64 => "ulong",
        FundamentalType.Float => "float",
        FundamentalType.Double => "double",
        FundamentalType.String => "string",
        _ => "object"
    };

    public static string ToDotNetType(FundamentalType t) => t switch
    {
        FundamentalType.Boolean => "Boolean",
        FundamentalType.Char => "Char",
        FundamentalType.Int8 => "SByte",
        FundamentalType.UInt8 => "Byte",
        FundamentalType.Int16 => "Int16",
        FundamentalType.UInt16 => "UInt16",
        FundamentalType.Int32 => "Int32",
        FundamentalType.UInt32 => "UInt32",
        FundamentalType.Int64 => "Int64",
        FundamentalType.UInt64 => "UInt64",
        FundamentalType.Float => "Single",
        FundamentalType.Double => "Double",
        FundamentalType.String => "String",
        _ => "Object"
    };
}
