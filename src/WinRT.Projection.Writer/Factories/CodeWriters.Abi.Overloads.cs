// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Primary <see cref="IndentedTextWriter"/>+<see cref="ProjectionEmitContext"/> overloads for the
/// large 'CodeWriters.Abi.cs' family. Each overload is a thin wrapper that builds a transient
/// <see cref="TypeWriter"/> sharing the underlying buffer + context, and delegates to the legacy
/// <see cref="TypeWriter"/>-based implementation. The per-call-site flattening will follow in a
/// future mechanical sweep once the surrounding helpers are also migrated.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Primary overload of <see cref="WriteAbiEnum(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteAbiEnum(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteAbiEnum(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteAbiStruct(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteAbiStruct(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteAbiStruct(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteAbiDelegate(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteAbiDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteAbiDelegate(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteTempDelegateEventSourceSubclass(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteTempDelegateEventSourceSubclass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteTempDelegateEventSourceSubclass(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteAbiClass(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteAbiClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteAbiClass(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteAbiInterface(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteAbiInterface(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteAbiInterface(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="EmitImplType(TypeWriter, TypeDefinition)"/>.</summary>
    public static bool EmitImplType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => EmitImplType(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteAbiParameterTypesPointer(TypeWriter, MethodSig)"/>.</summary>
    public static void WriteAbiParameterTypesPointer(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig)
        => WriteAbiParameterTypesPointer(new TypeWriter(writer, context), sig);

    /// <summary>Primary overload of <see cref="WriteAbiParameterTypesPointer(TypeWriter, MethodSig, bool)"/>.</summary>
    public static void WriteAbiParameterTypesPointer(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, bool includeParamNames)
        => WriteAbiParameterTypesPointer(new TypeWriter(writer, context), sig, includeParamNames);

    /// <summary>Primary overload of <see cref="WriteInterfaceVftbl(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteInterfaceVftbl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteInterfaceVftbl(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteInterfaceImpl(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteInterfaceImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteInterfaceImpl(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteInterfaceIdicImpl(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteInterfaceIdicImpl(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteInterfaceIdicImpl(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteInterfaceMarshaller(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteInterfaceMarshaller(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteInterfaceMarshaller(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteIidGuidReference(TypeWriter, TypeDefinition)"/>.</summary>
    public static void WriteIidGuidReference(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
        => WriteIidGuidReference(new TypeWriter(writer, context), type);

    /// <summary>Primary overload of <see cref="WriteAbiType(TypeWriter, TypeSemantics)"/>.</summary>
    public static void WriteAbiType(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
        => WriteAbiType(new TypeWriter(writer, context), semantics);
}
