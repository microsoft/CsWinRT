// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Builders;
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
            if (isParameter)
            {
                writer.Write("ReadOnlySpan<");
                TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(sz.BaseType));
                writer.Write(">");
            }
            else
            {
                TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(sz.BaseType));
                writer.Write("[]");
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

    /// <summary>
    /// Convenience overload of <see cref="WriteProjectedSignature(IndentedTextWriter, ProjectionEmitContext, TypeSignature, bool)"/>
    /// that leases an <see cref="IndentedTextWriter"/> from <see cref="IndentedTextWriterPool"/>,
    /// emits the projected signature into it, and returns the resulting string.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="typeSig">The signature to project.</param>
    /// <param name="isParameter">When <see langword="true"/>, projects SZ-arrays as <see cref="System.ReadOnlySpan{T}"/> (parameter convention) instead of <c>T[]</c>.</param>
    /// <returns>The projected signature.</returns>
    public static string WriteProjectedSignature(ProjectionEmitContext context, TypeSignature typeSig, bool isParameter)
    {
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();
        IndentedTextWriter writer = writerOwner.Writer;
        WriteProjectedSignature(writer, context, typeSig, isParameter);
        return writer.ToString();
    }

    /// <summary>
    /// Writes a parameter's projected type, applying the <see cref="ParameterCategory"/>-specific transformations.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteProjectionParameterType(IndentedTextWriter writer, ProjectionEmitContext context, ParameterInfo p)
    {
        ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
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
                writer.Write("ReadOnlySpan<");
                TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                writer.Write(">");
                break;
            case ParameterCategory.FillArray:
                writer.Write("Span<");
                TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                writer.Write(">");
                break;
            case ParameterCategory.ReceiveArray:
                writer.Write("out ");
                SzArrayTypeSignature? sz = p.Type as SzArrayTypeSignature
                    ?? (p.Type is ByReferenceTypeSignature br ? br.BaseType as SzArrayTypeSignature : null);

                if (sz is not null)
                {
                    TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(sz.BaseType));
                    writer.Write("[]");
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

    /// <summary>
    /// Writes the parameter name (escaped if it would clash with a C# keyword).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteParameterName(IndentedTextWriter writer, ParameterInfo p)
    {
        string name = p.Parameter.Name ?? "param";

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

    /// <summary>
    /// Returns the C# literal text for a constant field's value (or empty when no constant).
    /// </summary>
    /// <param name="field">The field definition.</param>
    /// <returns>The formatted constant value, or an empty string.</returns>
    public static string FormatField(FieldDefinition field)
    {
        if (field.Constant is null)
        {
            return string.Empty;
        }

        return ProjectionFileBuilder.FormatConstant(field.Constant);
    }
}
