// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Legacy <see cref="TypeWriter"/> passthrough overloads for the public methods declared in the
/// large 'CodeWriters.Abi.cs' file. The primary implementations now take
/// '(IndentedTextWriter writer, ProjectionEmitContext context, ...)' directly; the wrapper
/// overloads here exist for backward compatibility while the bodies of the Abi family still use
/// the legacy 'TypeWriter w' surface internally.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiEnum(TypeWriter w, TypeDefinition type)
        => WriteAbiEnum(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiStruct(TypeWriter w, TypeDefinition type)
        => WriteAbiStruct(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiDelegate(TypeWriter w, TypeDefinition type)
        => WriteAbiDelegate(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteTempDelegateEventSourceSubclass(TypeWriter w, TypeDefinition type)
        => WriteTempDelegateEventSourceSubclass(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiClass(TypeWriter w, TypeDefinition type)
        => WriteAbiClass(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiInterface(TypeWriter w, TypeDefinition type)
        => WriteAbiInterface(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static bool EmitImplType(TypeWriter w, TypeDefinition type)
        => EmitImplType(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiParameterTypesPointer(TypeWriter w, MethodSig sig)
        => WriteAbiParameterTypesPointer(w.Writer, w.Context, sig);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiParameterTypesPointer(TypeWriter w, MethodSig sig, bool includeParamNames)
        => WriteAbiParameterTypesPointer(w.Writer, w.Context, sig, includeParamNames);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteInterfaceVftbl(TypeWriter w, TypeDefinition type)
        => WriteInterfaceVftbl(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteInterfaceImpl(TypeWriter w, TypeDefinition type)
        => WriteInterfaceImpl(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteInterfaceIdicImpl(TypeWriter w, TypeDefinition type)
        => WriteInterfaceIdicImpl(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteInterfaceMarshaller(TypeWriter w, TypeDefinition type)
        => WriteInterfaceMarshaller(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteIidGuidReference(TypeWriter w, TypeDefinition type)
        => WriteIidGuidReference(w.Writer, w.Context, type);

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiType(TypeWriter w, TypeSemantics semantics)
        => WriteAbiType(w.Writer, w.Context, semantics);
}