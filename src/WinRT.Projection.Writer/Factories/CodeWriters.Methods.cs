// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Helpers for method/parameter/return type emission.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Writes the projected C# type for the given <paramref name="typeSig"/>.</summary>
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
                WriteProjectionType(writer, context, TypeSemanticsFactory.Get(sz.BaseType));
                writer.Write(">");
            }
            else
            {
                WriteProjectionType(writer, context, TypeSemanticsFactory.Get(sz.BaseType));
                writer.Write("[]");
            }
            return;
        }
        if (typeSig is ByReferenceTypeSignature br)
        {
            WriteProjectionType(writer, context, TypeSemanticsFactory.Get(br.BaseType));
            return;
        }
        WriteProjectionType(writer, context, TypeSemanticsFactory.Get(typeSig));
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteProjectedSignature(IndentedTextWriter, ProjectionEmitContext, TypeSignature, bool)"/>.</summary>
    public static void WriteProjectedSignature(TypeWriter w, TypeSignature typeSig, bool isParameter)
        => WriteProjectedSignature(w.Writer, w.Context, typeSig, isParameter);

    /// <summary>Writes a parameter's projected type, applying the <see cref="ParamCategory"/>-specific transformations.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteProjectionParameterType(IndentedTextWriter writer, ProjectionEmitContext context, ParamInfo p)
    {
        ParamCategory cat = ParamHelpers.GetParamCategory(p);
        switch (cat)
        {
            case ParamCategory.Out:
                writer.Write("out ");
                WriteProjectedSignature(writer, context, p.Type, true);
                break;
            case ParamCategory.Ref:
                writer.Write("in ");
                WriteProjectedSignature(writer, context, p.Type, true);
                break;
            case ParamCategory.PassArray:
                writer.Write("ReadOnlySpan<");
                WriteProjectionType(writer, context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                writer.Write(">");
                break;
            case ParamCategory.FillArray:
                writer.Write("Span<");
                WriteProjectionType(writer, context, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                writer.Write(">");
                break;
            case ParamCategory.ReceiveArray:
                writer.Write("out ");
                {
                    SzArrayTypeSignature? sz = p.Type as SzArrayTypeSignature
                        ?? (p.Type is ByReferenceTypeSignature br ? br.BaseType as SzArrayTypeSignature : null);
                    if (sz is not null)
                    {
                        WriteProjectionType(writer, context, TypeSemanticsFactory.Get(sz.BaseType));
                        writer.Write("[]");
                    }
                    else
                    {
                        WriteProjectedSignature(writer, context, p.Type, true);
                    }
                }
                break;
            default:
                WriteProjectedSignature(writer, context, p.Type, true);
                break;
        }
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteProjectionParameterType(IndentedTextWriter, ProjectionEmitContext, ParamInfo)"/>.</summary>
    public static void WriteProjectionParameterType(TypeWriter w, ParamInfo p) => WriteProjectionParameterType(w.Writer, w.Context, p);

    /// <summary>Writes the parameter name (escaped if it would clash with a C# keyword).</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteParameterName(IndentedTextWriter writer, ParamInfo p)
    {
        string name = p.Parameter.Name ?? "param";
        if (CSharpKeywords.IsKeyword(name)) { writer.Write("@"); }
        writer.Write(name);
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteParameterName(IndentedTextWriter, ParamInfo)"/>.</summary>
    public static void WriteParameterName(TypeWriter w, ParamInfo p) => WriteParameterName(w.Writer, p);

    /// <summary>Writes the parameter's projected type + name (e.g. <c>int @value</c>).</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="p">The parameter info.</param>
    public static void WriteProjectionParameter(IndentedTextWriter writer, ProjectionEmitContext context, ParamInfo p)
    {
        WriteProjectionParameterType(writer, context, p);
        writer.Write(" ");
        WriteParameterName(writer, p);
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteProjectionParameter(IndentedTextWriter, ProjectionEmitContext, ParamInfo)"/>.</summary>
    public static void WriteProjectionParameter(TypeWriter w, ParamInfo p) => WriteProjectionParameter(w.Writer, w.Context, p);

    /// <summary>Writes the projected return type of <paramref name="sig"/> (or <c>"void"</c>).</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The method signature.</param>
    public static void WriteProjectionReturnType(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig)
    {
        TypeSignature? rt = sig.ReturnType;
        if (rt is null)
        {
            writer.Write("void");
            return;
        }
        WriteProjectedSignature(writer, context, rt, false);
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteProjectionReturnType(IndentedTextWriter, ProjectionEmitContext, MethodSig)"/>.</summary>
    public static void WriteProjectionReturnType(TypeWriter w, MethodSig sig) => WriteProjectionReturnType(w.Writer, w.Context, sig);

    /// <summary>Writes a comma-separated parameter list.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="sig">The method signature whose parameters to enumerate.</param>
    public static void WriteParameterList(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig)
    {
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            WriteProjectionParameter(writer, context, sig.Params[i]);
        }
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to <see cref="WriteParameterList(IndentedTextWriter, ProjectionEmitContext, MethodSig)"/>.</summary>
    public static void WriteParameterList(TypeWriter w, MethodSig sig) => WriteParameterList(w.Writer, w.Context, sig);

    /// <summary>Returns the C# literal text for a constant field's value (or empty when no constant).</summary>
    /// <param name="field">The field definition.</param>
    /// <returns>The formatted constant value, or an empty string.</returns>
    public static string FormatField(FieldDefinition field)
    {
        if (field.Constant is null) { return string.Empty; }
        return FormatConstant(field.Constant);
    }
}
