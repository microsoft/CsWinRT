// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Mirrors C++ <c>fundamental_type</c> in <c>helpers.h</c>.
/// </summary>
internal enum FundamentalType
{
    Boolean,
    Char,
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float,
    Double,
    String,
}

/// <summary>
/// Discriminated union of the type semantics from C++ <c>type_semantics</c>.
/// </summary>
internal abstract record TypeSemantics
{
    private TypeSemantics() { }

    public sealed record Fundamental(FundamentalType Type) : TypeSemantics;
    public sealed record Object_ : TypeSemantics;
    public sealed record Guid_ : TypeSemantics;
    public sealed record Type_ : TypeSemantics;
    public sealed record Definition(TypeDefinition Type) : TypeSemantics;
    public sealed record GenericInstance(TypeDefinition GenericType, List<TypeSemantics> GenericArgs) : TypeSemantics;
    public sealed record GenericTypeIndex(int Index) : TypeSemantics;
    public sealed record GenericMethodIndex(int Index) : TypeSemantics;
    public sealed record GenericParameter_(GenericParameter Parameter) : TypeSemantics;
    public sealed record Reference(TypeReference Reference_) : TypeSemantics;
}

/// <summary>
/// Static helpers for converting AsmResolver type signatures into <see cref="TypeSemantics"/> instances.
/// Mirrors <c>get_type_semantics</c> in <c>helpers.h</c>.
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
            TypeDefOrRefSignature tdorref => GetFromTypeDefOrRef(tdorref.Type),
            SzArrayTypeSignature sz => Get(sz.BaseType), // SZ arrays handled by callers
            ByReferenceTypeSignature br => Get(br.BaseType),
            _ => GetFromTypeDefOrRef(signature.GetUnderlyingTypeDefOrRef() ?? throw new System.InvalidOperationException("Unsupported signature: " + signature?.ToString())),
        };
    }

    public static TypeSemantics GetFromTypeDefOrRef(ITypeDefOrRef type)
    {
        if (type is TypeDefinition def)
        {
            return new TypeSemantics.Definition(def);
        }
        if (type is TypeReference reference)
        {
            string ns = reference.Namespace?.Value ?? string.Empty;
            string name = reference.Name?.Value ?? string.Empty;
            if (ns == "System" && name == "Guid") { return new TypeSemantics.Guid_(); }
            if (ns == "System" && name == "Object") { return new TypeSemantics.Object_(); }
            if (ns == "System" && name == "Type") { return new TypeSemantics.Type_(); }
            return new TypeSemantics.Reference(reference);
        }
        if (type is TypeSpecification spec && spec.Signature is GenericInstanceTypeSignature gi)
        {
            return GetGenericInstance(gi);
        }
        return new TypeSemantics.Reference((TypeReference)type);
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
            ElementType.Object => new TypeSemantics.Object_(),
            _ => throw new System.InvalidOperationException($"Unsupported corlib element type: {elementType}")
        };
    }

    private static TypeSemantics GetGenericInstance(GenericInstanceTypeSignature gi)
    {
        ITypeDefOrRef genericType = gi.GenericType;
        TypeDefinition? def = genericType as TypeDefinition;
        // For TypeReference, we generally don't need to resolve - we just need namespace+name.
        List<TypeSemantics> args = new(gi.TypeArguments.Count);
        foreach (TypeSignature arg in gi.TypeArguments)
        {
            args.Add(Get(arg));
        }
        if (def is null)
        {
            // Synthesize - for write_typedef_name, we just need namespace and name.
            return new TypeSemantics.Reference((TypeReference)genericType);
        }
        return new TypeSemantics.GenericInstance(def, args);
    }
}

/// <summary>
/// Type-name kind, mirrors C++ <c>typedef_name_type</c>.
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
/// Type mapping helpers (e.g., <c>to_csharp_type</c>, <c>to_dotnet_type</c>) from C++ <c>code_writers.h</c>.
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
