// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Helpers for method/parameter/return type emission.
/// </summary>
internal static class MethodFactory
{
    /// <summary>
    /// Writes the projected C# type for the given <paramref name="typeSig"/>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="typeSig">The signature to project.</param>
    /// <param name="isParameter">When <see langword="true"/>, projects SZ-arrays as <see cref="System.ReadOnlySpan{T}"/> (parameter convention) instead of <c>T[]</c>.</param>
    public static void WriteProjectedSignature(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature typeSig, bool isParameter)
    {
        if (typeSig is SzArrayTypeSignature sz)
        {
            // SZ arrays project as ReadOnlySpan<T> (matches the property setter parameter
            // convention; pass_array semantics).
            IndentedTextWriterCallback elem = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(sz.BaseType));
            if (isParameter)
            {
                writer.Write($"ReadOnlySpan<{elem}>");
            }
            else
            {
                writer.Write($"{elem}[]");
            }

            return;
        }

        if (typeSig is ByReferenceTypeSignature br)
        {
            TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(br.BaseType));
            return;
        }

        TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(typeSig));
    }

    /// <inheritdoc cref="WriteProjectedSignature(IndentedTextWriter, ProjectionEmitContext, TypeSignature, bool)"/>
    /// <returns>A callback that writes the projected signature to the writer it's appended to.</returns>
    public static IndentedTextWriterCallback WriteProjectedSignature(ProjectionEmitContext context, TypeSignature typeSig, bool isParameter)
    {
        return writer => MethodFactory.WriteProjectedSignature(writer, context, typeSig, isParameter);
    }

    /// <summary>
    /// Writes a parameter's projected type, applying the <see cref="ParameterCategory"/>-specific transformations.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteProjectionParameterType(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p)
    {
        ParameterCategory cat = ParameterCategoryResolver.Resolve(p);
        switch (cat)
        {
            case ParameterCategory.Out:
                writer.Write("out ");
                WriteProjectedSignature(writer, context, p.Type, true);
                break;
            case ParameterCategory.Ref:
                writer.Write("in ");
                WriteProjectedSignature(writer, context, p.Type, true);
                break;
            case ParameterCategory.PassArray:
                {
                    IndentedTextWriterCallback elem = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                    writer.Write($"ReadOnlySpan<{elem}>");
                }
                break;
            case ParameterCategory.FillArray:
                {
                    IndentedTextWriterCallback elem = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                    writer.Write($"Span<{elem}>");
                }
                break;
            case ParameterCategory.ReceiveArray:
                writer.Write("out ");
                SzArrayTypeSignature? sz = p.Type as SzArrayTypeSignature
                    ?? (p.Type is ByReferenceTypeSignature br ? br.BaseType as SzArrayTypeSignature : null);

                if (sz is not null)
                {
                    IndentedTextWriterCallback elem = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(sz.BaseType));
                    writer.Write($"{elem}[]");
                }
                else
                {
                    WriteProjectedSignature(writer, context, p.Type, true);
                }
                break;
            default:
                WriteProjectedSignature(writer, context, p.Type, true);
                break;
        }
    }

    /// <inheritdoc cref="WriteProjectionParameterType(IndentedTextWriter, ProjectionEmitContext, ParameterInfo)"/>
    /// <returns>A callback that writes the projected parameter type to the writer it's appended to.</returns>
    public static IndentedTextWriterCallback WriteProjectionParameterType(ProjectionEmitContext context, ParameterInfo p)
    {
        return writer => MethodFactory.WriteProjectionParameterType(writer, context, p);
    }

    /// <summary>
    /// Writes the parameter name (escaped if it would clash with a C# keyword).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteParameterName(IndentedTextWriter writer, ParameterInfo p)
    {
        string name = p.GetRawName();

        writer.WriteIf(CSharpKeywords.IsKeyword(name), "@");

        writer.Write(name);
    }

    /// <summary>
    /// Writes the parameter's projected type + name (e.g. <c>int @value</c>).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteProjectionParameter(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p)
    {
        WriteProjectionParameterType(writer, context, p);
        writer.Write(" ");
        WriteParameterName(writer, p);
    }

    /// <inheritdoc cref="WriteProjectionParameter(IndentedTextWriter, ProjectionEmitContext, ParameterInfo)"/>
    /// <returns>A callback that writes the parameter (type + name) to the writer it's appended to.</returns>
    public static IndentedTextWriterCallback WriteProjectionParameter(ProjectionEmitContext context, ParameterInfo p)
    {
        return writer => MethodFactory.WriteProjectionParameter(writer, context, p);
    }

    /// <summary>
    /// Writes the projected return type of <paramref name="sig"/> (or <c>"void"</c>).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The method signature.</param>
    public static void WriteProjectionReturnType(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig)
    {
        TypeSignature? rt = sig.ReturnType;

        if (rt is null)
        {
            writer.Write("void");
            return;
        }

        WriteProjectedSignature(writer, context, rt, false);
    }

    /// <inheritdoc cref="WriteProjectionReturnType(IndentedTextWriter, ProjectionEmitContext, MethodSignatureInfo)"/>
    /// <returns>A callback that writes the projected return type to the writer it's appended to.</returns>
    public static IndentedTextWriterCallback WriteProjectionReturnType(ProjectionEmitContext context, MethodSignatureInfo sig)
    {
        return writer => MethodFactory.WriteProjectionReturnType(writer, context, sig);
    }

    /// <summary>
    /// Writes a comma-separated parameter list.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The method signature whose parameters to enumerate.</param>
    public static void WriteParameterList(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig)
    {
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            writer.WriteIf(i > 0, ", ");

            WriteProjectionParameter(writer, context, sig.Parameters[i]);
        }
    }

    /// <inheritdoc cref="WriteParameterList(IndentedTextWriter, ProjectionEmitContext, MethodSignatureInfo)"/>
    /// <returns>A callback that writes the parameter list to the writer it's appended to.</returns>
    public static IndentedTextWriterCallback WriteParameterList(ProjectionEmitContext context, MethodSignatureInfo sig)
    {
        return writer => MethodFactory.WriteParameterList(writer, context, sig);
    }

    /// <summary>
    /// Writes a comma-separated argument-forwarding list (parameter names with in/out modifiers).
    /// When <paramref name="leadingComma"/> is <see langword="true"/>, the list is preceded by
    /// <c>", "</c> (so it can be appended to a call site that already passed one or more args);
    /// otherwise, the comma separator appears only between elements.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The method signature whose parameters to forward.</param>
    /// <param name="leadingComma">When <see langword="true"/>, emits a leading <c>", "</c> before the first element.</param>
    public static void WriteCallArguments(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo sig, bool leadingComma)
    {
        for (int i = 0; i < sig.Parameters.Count; i++)
        {
            IndentedTextWriterCallback p = ClassMembersFactory.WriteParameterNameWithModifier(context, sig.Parameters[i]);
            string sep = (leadingComma || i > 0) ? ", " : "";

            writer.Write($"{sep}{p}");
        }
    }

    /// <inheritdoc cref="WriteCallArguments(IndentedTextWriter, ProjectionEmitContext, MethodSignatureInfo, bool)"/>
    /// <returns>A callback that writes the call-argument list to the writer it's appended to.</returns>
    public static IndentedTextWriterCallback WriteCallArguments(ProjectionEmitContext context, MethodSignatureInfo sig, bool leadingComma)
    {
        return writer => MethodFactory.WriteCallArguments(writer, context, sig, leadingComma);
    }
}
