// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Centralised emitters for <c>[UnsafeAccessor]</c> static extern declarations. Consolidates
/// the boilerplate around the attribute scaffolding so call sites only need to express the parts
/// that vary (access name, return type, function name, interop type, and parameter list).
/// </summary>
internal static class UnsafeAccessorFactory
{
    /// <summary>
    /// Emits an <c>[UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]</c> static extern declaration
    /// targeting a static method on the type identified by <paramref name="interopType"/>:
    /// <code>
    /// [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "{accessName}")]
    /// static extern {returnType} {functionName}([UnsafeAccessorType("{interopType}")] object _, {parameterList});
    /// </code>
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="accessName">The metadata name of the static method being accessed.</param>
    /// <param name="returnType">The C# return type of the accessor (as a syntax string).</param>
    /// <param name="functionName">The C# name of the accessor (i.e. the local extern method).</param>
    /// <param name="interopType">The assembly-qualified interop type the accessor punches into.</param>
    /// <param name="parameterList">The trailing parameter list (after the synthetic <c>object _</c>
    /// receiver parameter) WITHOUT a leading comma. Pass <see cref="string.Empty"/> for the
    /// parameter-less form; otherwise the comma separator is inserted automatically.</param>
    public static void EmitStaticMethod(
        IndentedTextWriter writer,
        string accessName,
        string returnType,
        string functionName,
        string interopType,
        string parameterList)
    {
        string commaPrefix = parameterList.Length > 0 ? ", " : "";

        writer.WriteLine(isMultiline: true, $$"""
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "{{accessName}}")]
            static extern {{returnType}} {{functionName}}([UnsafeAccessorType("{{interopType}}")] object _{{commaPrefix}}{{parameterList}});
            """);
    }

    /// <summary>
    /// Emits an <c>[UnsafeAccessor(UnsafeAccessorKind.Constructor)]</c> static extern declaration
    /// whose return type is annotated with <c>[return: UnsafeAccessorType("{interopType}")]</c>:
    /// <code>
    /// [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    /// [return: UnsafeAccessorType("{interopType}")]
    /// static extern object {functionName}({parameterList});
    /// </code>
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="interopType">The assembly-qualified interop type whose constructor is being accessed.</param>
    /// <param name="functionName">The C# name of the constructor accessor (i.e. the local extern method).</param>
    /// <param name="parameterList">The constructor's full parameter list (no leading comma).</param>
    public static void EmitConstructorReturningObject(
        IndentedTextWriter writer,
        string interopType,
        string functionName,
        string parameterList)
    {
        writer.WriteLine(isMultiline: true, $$"""
            [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
            [return: UnsafeAccessorType("{{interopType}}")]
            static extern object {{functionName}}({{parameterList}});
            """);
    }

    /// <summary>
    /// Emits the <c>[UnsafeAccessor]</c> extern method declaration that exposes the IID for a
    /// generic interface instantiation.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="gi">The generic interface instantiation whose IID accessor is being emitted.</param>
    public static void EmitIidAccessor(IndentedTextWriter writer, ProjectionEmitContext context, GenericInstanceTypeSignature gi)
    {
        string propName = ObjRefNameGenerator.BuildIidPropertyNameForGenericInterface(context, gi);
        string interopName = InteropTypeNameWriter.EncodeInteropTypeName(gi, TypedefNameType.InteropIID);

        writer.WriteLine(isMultiline: true, $$"""
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "get_IID_{{interopName}}")]
            static extern ref readonly Guid {{propName}}([UnsafeAccessorType("ABI.InterfaceIIDs, WinRT.Interop")] object _);
            """);
    }
}
