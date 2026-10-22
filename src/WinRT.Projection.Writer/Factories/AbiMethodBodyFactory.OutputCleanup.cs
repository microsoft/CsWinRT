// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class AbiMethodBodyFactory
{
    /// <summary>
    /// Writes cleanup for an owned scalar ABI value.
    /// </summary>
    private static void EmitDisposeAbiValue(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature type, string value)
    {
        if (type.IsString())
        {
            writer.WriteLine($"HStringMarshaller.Free({value});");
        }
        else if (context.AbiTypeKindResolver.IsReferenceTypeOrGenericInstance(type))
        {
            writer.WriteLine($"WindowsRuntimeUnknownMarshaller.Free({value});");
        }
        else if (type.IsSystemType())
        {
            writer.WriteLine($"global::ABI.System.TypeMarshaller.Dispose({value});");
        }
        else if (context.AbiTypeKindResolver.IsNonBlittableStruct(type))
        {
            writer.WriteLine($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, type)}.Dispose({value});");
        }
    }

    /// <summary>
    /// Gets whether a method publishes parameter outputs that must be rolled back if a later conversion fails.
    /// </summary>
    private static bool RequiresDoAbiOutputCleanup(ProjectionEmitContext context, MethodSignatureInfo signature)
    {
        foreach (ParameterInfo parameter in signature.Parameters)
        {
            ParameterCategory category = ParameterCategoryResolver.Resolve(parameter);

            if (category == ParameterCategory.ReceiveArray ||
                (category == ParameterCategory.Out &&
                    context.AbiTypeKindResolver.RequiresOutParameterCleanup(parameter.Type.StripByRefAndCustomModifiers())) ||
                (category == ParameterCategory.FillArray &&
                    parameter.Type is SzArrayTypeSignature array &&
                    context.AbiTypeKindResolver.RequiresOutParameterCleanup(array.BaseType)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Initializes native fill-array output slots before any output conversion acquires resources.
    /// </summary>
    private static void EmitInitializeDoAbiFillArrays(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo signature)
    {
        foreach ((_, ParameterInfo parameter) in signature.ParametersByCategory(ParameterCategory.FillArray))
        {
            if (parameter.Type is not SzArrayTypeSignature array ||
                !context.AbiTypeKindResolver.RequiresOutParameterCleanup(array.BaseType))
            {
                continue;
            }

            string raw = parameter.GetRawName();
            string pointer = IdentifierEscaping.EscapeIdentifier(raw);
            string storage = AbiTypeHelpers.GetArrayElementStorageType(context, array.BaseType);
            writer.WriteLine(isMultiline: true, $$"""
                if ({{pointer}} is not null)
                {
                    new Span<{{storage}}>(({{storage}}*){{pointer}}, (int)__{{raw}}Size).Clear();
                }
                """);
        }
    }

    /// <summary>
    /// Rolls back native outputs without freeing caller-owned fill-array storage or borrowed inputs.
    /// </summary>
    private static void EmitDoAbiOutputCleanup(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo signature)
    {
        foreach ((_, ParameterInfo parameter) in signature.ParametersByCategory(ParameterCategory.Out))
        {
            string pointer = IdentifierEscaping.EscapeIdentifier(parameter.GetRawName());
            EmitDisposeAbiValue(writer, context, parameter.Type.StripByRefAndCustomModifiers(), $"*{pointer}");
            writer.WriteLine($"*{pointer} = default;");
        }

        foreach ((_, ParameterInfo parameter) in signature.ParametersByCategory(ParameterCategory.ReceiveArray))
        {
            string raw = parameter.GetRawName();
            string pointer = IdentifierEscaping.EscapeIdentifier(raw);
            SzArrayTypeSignature array = parameter.Type.AsSzArray()!;
            string elementAbi = AbiTypeHelpers.GetAbiLocalTypeName(context, array.BaseType);
            UnsafeAccessorFactory.EmitStaticMethod(
                writer,
                accessName: "Free",
                returnType: "void",
                functionName: $"Free_{raw}",
                interopType: ArrayElementEncoder.GetArrayMarshallerInteropPath(context, array.BaseType),
                parameterList: $"uint length, {elementAbi}* data");
            writer.WriteLine(isMultiline: true, $$"""
                if (*{{pointer}} is not null)
                {
                    Free_{{raw}}(null, *__{{raw}}Size, *{{pointer}});
                }

                *{{pointer}} = default;
                *__{{raw}}Size = default;
                """);
        }

        foreach ((_, ParameterInfo parameter) in signature.ParametersByCategory(ParameterCategory.FillArray))
        {
            if (parameter.Type is not SzArrayTypeSignature array ||
                !context.AbiTypeKindResolver.RequiresOutParameterCleanup(array.BaseType))
            {
                continue;
            }

            string raw = parameter.GetRawName();
            string pointer = IdentifierEscaping.EscapeIdentifier(raw);
            string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, array.BaseType);
            string storage = AbiTypeHelpers.GetArrayElementStorageType(context, array.BaseType);
            writer.WriteLine(isMultiline: true, $$"""
                if ({{pointer}} is not null)
                {
                    for (uint __{{raw}}_cleanupIndex = 0; __{{raw}}_cleanupIndex < __{{raw}}Size; __{{raw}}_cleanupIndex++)
                    {
                """);
            writer.IncreaseIndent();
            writer.IncreaseIndent();
            EmitDisposeAbiValue(writer, context, array.BaseType, $"(({elementAbi}*){pointer})[__{raw}_cleanupIndex]");
            writer.DecreaseIndent();
            writer.WriteLine(isMultiline: true, $$"""
                }

                new Span<{{storage}}>(({{storage}}*){{pointer}}, (int)__{{raw}}Size).Clear();
                """);
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
}
