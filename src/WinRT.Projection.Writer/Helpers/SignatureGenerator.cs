// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Emits the WinRT parametric type signature string used as the input to the WinRT GUID hash
/// algorithm. The signature format mirrors the cswinrt.exe C++ implementation:
/// <list type="bullet">
///   <item>Fundamental types map to a single-character code (e.g. <c>i4</c>, <c>u8</c>, <c>string</c>).</item>
///   <item>Enums become <c>enum(&lt;name&gt;;&lt;underlying&gt;)</c>.</item>
///   <item>Structs become <c>struct(&lt;name&gt;;&lt;field-signatures&gt;)</c>.</item>
///   <item>Delegates become <c>delegate({&lt;guid&gt;})</c>.</item>
///   <item>Interfaces become <c>{&lt;guid&gt;}</c>.</item>
///   <item>Runtime classes become <c>rc(&lt;name&gt;;&lt;default-interface-signature&gt;)</c>.</item>
///   <item>Generic instantiations become <c>pinterface({&lt;open-guid&gt;};&lt;arg-signatures&gt;)</c>.</item>
/// </list>
/// </summary>
internal static class SignatureGenerator
{
    /// <summary>
    /// Gets the signature for a given type.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="semantics">The type semantics whose GUID signature is emitted.</param>
    /// <returns>The emitted GUID signature.</returns>
    public static string GetSignature(ProjectionEmitContext context, TypeSemantics semantics)
    {
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();

        IndentedTextWriter writer = writerOwner.Writer;

        WriteSignature(writer, context, semantics);

        return writer.ToString();
    }

    /// <summary>
    /// Returns the GUID-signature character code for a fundamental WinRT type (e.g. <c>i4</c>
    /// for <see cref="FundamentalType.Int32"/>, <c>string</c> for <see cref="FundamentalType.String"/>).
    /// </summary>
    /// <param name="type">The fundamental type.</param>
    /// <returns>The signature code.</returns>
    /// <exception cref="WellKnownProjectionWriterException">Thrown when <paramref name="type"/>
    /// is not a known fundamental type.</exception>
    private static string GetFundamentalTypeGuidSignature(FundamentalType type) => type switch
    {
        FundamentalType.Boolean => "b1",
        FundamentalType.Char => "c2",
        FundamentalType.Int8 => "i1",
        FundamentalType.UInt8 => "u1",
        FundamentalType.Int16 => "i2",
        FundamentalType.UInt16 => "u2",
        FundamentalType.Int32 => "i4",
        FundamentalType.UInt32 => "u4",
        FundamentalType.Int64 => "i8",
        FundamentalType.UInt64 => "u8",
        FundamentalType.Float => "f4",
        FundamentalType.Double => "f8",
        FundamentalType.String => "string",
        _ => throw WellKnownProjectionWriterExceptions.UnknownFundamentalType()
    };

    /// <summary>
    /// Writes the WinRT GUID parametric signature for <paramref name="semantics"/> into
    /// <paramref name="writer"/>. Used as input to <c>GuidGenerator.Generate</c> to compute the
    /// IID for a generic interface instantiation, or as the inner signature of a containing
    /// composite (struct field, runtime-class default interface, etc.).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context (used for cross-module type resolution).</param>
    /// <param name="semantics">The type semantics whose signature is emitted.</param>
    private static void WriteSignature(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
    {
        switch (semantics)
        {
            case TypeSemantics.GuidType:
                writer.Write("g16");
                break;
            case TypeSemantics.ObjectType:
                writer.Write("cinterface(IInspectable)");
                break;
            case TypeSemantics.Fundamental f:
                writer.Write(GetFundamentalTypeGuidSignature(f.Type));
                break;
            case TypeSemantics.Definition d:
                WriteSignatureForType(writer, context, d.Type);
                break;
            case TypeSemantics.Reference r:
                {
                    // Resolve the reference to a 'TypeDefinition' (cross-module struct field, etc.).
                    (string ns, string name) = r.Type.Names();
                    TypeDefinition? resolved = null;

                    if (context.Cache is not null)
                    {
                        resolved = r.Type.TryResolve(context.Cache.RuntimeContext) ?? context.Cache.Find(ns, name);
                    }

                    if (resolved is not null)
                    {
                        WriteSignatureForType(writer, context, resolved);
                    }
                }
                break;
            case TypeSemantics.GenericInstance gi:
                writer.Write("pinterface({");
                IidExpressionGenerator.WriteGuid(writer, gi.GenericType, true);
                writer.Write("};");
                for (int i = 0; i < gi.GenericArgs.Count; i++)
                {
                    writer.WriteIf(i > 0, ";");

                    WriteSignature(writer, context, gi.GenericArgs[i]);
                }
                writer.Write(")");
                break;
            case TypeSemantics.GenericInstanceRef gir:
                {
                    // Cross-module generic instance (e.g. Windows.Foundation.IReference<UInt64>
                    // appearing as a struct field). Resolve the generic type to a TypeDefinition
                    // so we can extract its [Guid]; recurse on each type argument.
                    (string ns, string name) = gir.GenericType.Names();
                    TypeDefinition? resolved = null;

                    if (context.Cache is not null)
                    {
                        resolved = gir.GenericType.TryResolve(context.Cache.RuntimeContext) ?? context.Cache.Find(ns, name);
                    }

                    if (resolved is not null)
                    {
                        writer.Write("pinterface({");
                        IidExpressionGenerator.WriteGuid(writer, resolved, true);
                        writer.Write("};");
                        for (int i = 0; i < gir.GenericArgs.Count; i++)
                        {
                            writer.WriteIf(i > 0, ";");

                            WriteSignature(writer, context, gir.GenericArgs[i]);
                        }
                        writer.Write(")");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Writes the GUID signature fragment for <paramref name="type"/>, dispatched by category:
    /// enums emit <c>enum(name;underlying)</c>, structs emit <c>struct(name;field-sigs)</c>,
    /// delegates emit <c>delegate({guid})</c>, interfaces emit <c>{guid}</c>, and runtime classes
    /// emit <c>rc(name;default-interface-sig)</c> (or fall back to <c>{guid}</c> when no default
    /// interface is declared).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type to emit a signature for.</param>
    private static void WriteSignatureForType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        TypeKind kind = TypeKindResolver.Resolve(type);
        switch (kind)
        {
            case TypeKind.Enum:
                writer.Write("enum(");
                TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.NonProjected, true);
                TypedefNameWriter.WriteTypeParams(writer, type);
                writer.Write(";");
                writer.Write(type.IsFlagsEnum ? "u4" : "i4");
                writer.Write(")");
                break;
            case TypeKind.Struct:
                writer.Write("struct(");
                TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.NonProjected, true);
                TypedefNameWriter.WriteTypeParams(writer, type);
                writer.Write(";");
                bool first = true;
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic)
                    {
                        continue;
                    }

                    if (field.Signature is null)
                    {
                        continue;
                    }

                    writer.WriteIf(!first, ";");

                    first = false;
                    WriteSignature(writer, context, TypeSemanticsFactory.Get(field.Signature.FieldType));
                }
                writer.Write(")");
                break;
            case TypeKind.Delegate:
                writer.Write("delegate({");
                IidExpressionGenerator.WriteGuid(writer, type, true);
                writer.Write("})");
                break;
            case TypeKind.Interface:
                writer.Write("{");
                IidExpressionGenerator.WriteGuid(writer, type, true);
                writer.Write("}");
                break;
            case TypeKind.Class:
                ITypeDefOrRef? defaultIface = type.GetDefaultInterface();

                if (defaultIface is TypeDefinition di)
                {
                    writer.Write("rc(");
                    TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.NonProjected, true);
                    TypedefNameWriter.WriteTypeParams(writer, type);
                    writer.Write(";");
                    WriteSignature(writer, context, new TypeSemantics.Definition(di));
                    writer.Write(")");
                }
                else
                {
                    writer.Write("{");
                    IidExpressionGenerator.WriteGuid(writer, type, true);
                    writer.Write("}");
                }

                break;
        }
    }
}
