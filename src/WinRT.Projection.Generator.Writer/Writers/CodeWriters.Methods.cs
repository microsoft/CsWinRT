// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Helpers for method/parameter/return type emission. Mirrors various functions in <c>code_writers.h</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>write_projected_signature</c>.</summary>
    public static void WriteProjectedSignature(TypeWriter w, TypeSignature typeSig, bool isParameter)
    {
        // Detect SZ-array
        if (typeSig is SzArrayTypeSignature sz)
        {
            WriteProjectionType(w, TypeSemanticsFactory.Get(sz.BaseType));
            w.Write(isParameter ? "[]" : "[]");
            return;
        }
        if (typeSig is ByReferenceTypeSignature br)
        {
            WriteProjectionType(w, TypeSemanticsFactory.Get(br.BaseType));
            return;
        }
        WriteProjectionType(w, TypeSemanticsFactory.Get(typeSig));
    }

    /// <summary>Mirrors C++ <c>write_projection_parameter_type</c>.</summary>
    public static void WriteProjectionParameterType(TypeWriter w, ParamInfo p)
    {
        ParamCategory cat = ParamHelpers.GetParamCategory(p);
        switch (cat)
        {
            case ParamCategory.Out:
                w.Write("out ");
                WriteProjectedSignature(w, p.Type, true);
                break;
            case ParamCategory.Ref:
                w.Write("in ");
                WriteProjectedSignature(w, p.Type, true);
                break;
            case ParamCategory.PassArray:
                w.Write("global::System.ReadOnlySpan<");
                WriteProjectionType(w, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                w.Write(">");
                break;
            case ParamCategory.FillArray:
                w.Write("global::System.Span<");
                WriteProjectionType(w, TypeSemanticsFactory.Get(((SzArrayTypeSignature)p.Type).BaseType));
                w.Write(">");
                break;
            case ParamCategory.ReceiveArray:
                w.Write("out ");
                {
                    SzArrayTypeSignature? sz = p.Type as SzArrayTypeSignature
                        ?? (p.Type is ByReferenceTypeSignature br ? br.BaseType as SzArrayTypeSignature : null);
                    if (sz is not null)
                    {
                        WriteProjectionType(w, TypeSemanticsFactory.Get(sz.BaseType));
                        w.Write("[]");
                    }
                    else
                    {
                        WriteProjectedSignature(w, p.Type, true);
                    }
                }
                break;
            default:
                WriteProjectedSignature(w, p.Type, true);
                break;
        }
    }

    /// <summary>Mirrors C++ <c>write_parameter_name</c>.</summary>
    public static void WriteParameterName(TypeWriter w, ParamInfo p)
    {
        string name = p.Parameter.Name ?? "param";
        Helpers.WriteEscapedIdentifier(w, name);
    }

    /// <summary>Mirrors C++ <c>write_projection_parameter</c>.</summary>
    public static void WriteProjectionParameter(TypeWriter w, ParamInfo p)
    {
        WriteProjectionParameterType(w, p);
        w.Write(" ");
        WriteParameterName(w, p);
    }

    /// <summary>Mirrors C++ <c>write_projection_return_type</c>.</summary>
    public static void WriteProjectionReturnType(TypeWriter w, MethodSig sig)
    {
        TypeSignature? rt = sig.ReturnType;
        if (rt is null)
        {
            w.Write("void");
            return;
        }
        WriteProjectedSignature(w, rt, false);
    }

    /// <summary>Writes a parameter list separated by commas.</summary>
    public static void WriteParameterList(TypeWriter w, MethodSig sig)
    {
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (i > 0) { w.Write(", "); }
            WriteProjectionParameter(w, sig.Params[i]);
        }
    }

    /// <summary>Writes a constant value as a C# literal. Mirrors C++ <c>write_constant</c> partially.</summary>
    public static string FormatField(FieldDefinition field)
    {
        if (field.Constant is null) { return string.Empty; }
        return FormatConstant(field.Constant);
    }
}
