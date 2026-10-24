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
    /// Gets the last receive-array output that can rely solely on its allocating marshaller for failure cleanup.
    /// </summary>
    /// <remarks>
    /// Output conversion runs scalar outs, receive arrays, non-blittable fill-array copy-back, then the return.
    /// Allocating ConvertToUnmanaged helpers release partial results on failure without modifying the output slots.
    /// The ABI callback initializes those slots to default beforehand. If no later output conversion can throw,
    /// the final receive array needs no method-wide rollback: failure publishes nothing, and success cannot be
    /// followed by another output-conversion failure. Earlier owned outputs still need rollback if this conversion fails.
    /// Non-blittable fill-array copy-back must still be treated as potentially throwing even when its ABI elements
    /// need no disposal.
    /// </remarks>
    private static ParameterInfo? GetFinalDoAbiReceiveArray(ProjectionEmitContext context, MethodSignatureInfo signature, bool isArraySetter)
    {
        if (signature.ReturnType is { } returnType &&
            !context.AbiTypeKindResolver.IsBlittableAbiElement(returnType))
        {
            return null;
        }

        ParameterInfo? finalReceiveArray = null;
        foreach (ParameterInfo parameter in signature.Parameters)
        {
            ParameterCategory category = ParameterCategoryResolver.Resolve(parameter);
            if (category == ParameterCategory.ReceiveArray)
            {
                finalReceiveArray = parameter;
            }
            else if (!isArraySetter && category == ParameterCategory.FillArray &&
                parameter.Type is SzArrayTypeSignature array &&
                !context.AbiTypeKindResolver.IsBlittableAbiElement(array.BaseType))
            {
                // Fill-array copy-back follows all receive arrays, regardless of declaration order.
                return null;
            }
        }

        return finalReceiveArray;
    }

    /// <summary>
    /// Gets whether a method publishes parameter outputs that must be rolled back if a later conversion fails.
    /// </summary>
    private static bool RequiresDoAbiOutputCleanup(ProjectionEmitContext context, MethodSignatureInfo signature, ParameterInfo? finalReceiveArray)
    {
        foreach (ParameterInfo parameter in signature.Parameters)
        {
            ParameterCategory category = ParameterCategoryResolver.Resolve(parameter);

            if ((category == ParameterCategory.ReceiveArray && parameter != finalReceiveArray) ||
                (category == ParameterCategory.Out &&
                    context.AbiTypeKindResolver.RequiresAbiDisposal(parameter.Type.StripByRefAndCustomModifiers())) ||
                (category == ParameterCategory.FillArray &&
                    parameter.Type is SzArrayTypeSignature array &&
                    context.AbiTypeKindResolver.RequiresAbiDisposal(array.BaseType)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clears resource-owning native fill-array slots before any output conversion acquires resources.
    /// </summary>
    private static void EmitClearDoAbiFillArrays(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo signature)
    {
        foreach ((_, ParameterInfo parameter) in signature.ParametersByCategory(ParameterCategory.FillArray))
        {
            if (parameter.Type is not SzArrayTypeSignature array ||
                !context.AbiTypeKindResolver.RequiresAbiDisposal(array.BaseType))
            {
                continue;
            }

            string raw = parameter.GetRawName();
            string pointer = IdentifierEscaping.EscapeIdentifier(raw);
            string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, array.BaseType);
            UnsafeAccessorFactory.EmitStaticMethod(
                writer,
                accessName: "Clear",
                returnType: "void",
                functionName: $"Clear_{raw}",
                interopType: ArrayElementEncoder.GetArrayMarshallerInteropPath(context, array.BaseType),
                parameterList: $"uint length, {elementAbi}* data");
            writer.WriteLine($"Clear_{raw}(null, __{raw}Size, ({elementAbi}*){pointer});");
        }
    }

    /// <summary>
    /// Rolls back native outputs without freeing caller-owned fill-array storage or borrowed inputs.
    /// </summary>
    private static void EmitDoAbiOutputCleanup(IndentedTextWriter writer, ProjectionEmitContext context, MethodSignatureInfo signature, ParameterInfo? finalReceiveArray)
    {
        foreach ((_, ParameterInfo parameter) in signature.ParametersByCategory(ParameterCategory.Out))
        {
            string pointer = IdentifierEscaping.EscapeIdentifier(parameter.GetRawName());
            EmitDisposeAbiValue(writer, context, parameter.Type.StripByRefAndCustomModifiers(), $"*{pointer}");
            writer.WriteLine($"*{pointer} = default;");
        }

        foreach ((_, ParameterInfo parameter) in signature.ParametersByCategory(ParameterCategory.ReceiveArray))
        {
            if (parameter == finalReceiveArray)
            {
                continue;
            }

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
                !context.AbiTypeKindResolver.RequiresAbiDisposal(array.BaseType))
            {
                continue;
            }

            string raw = parameter.GetRawName();
            string pointer = IdentifierEscaping.EscapeIdentifier(raw);
            string elementAbi = AbiTypeHelpers.GetArrayElementAbiType(context, array.BaseType);
            UnsafeAccessorFactory.EmitStaticMethod(
                writer,
                accessName: "DisposeAndClear",
                returnType: "void",
                functionName: $"DisposeAndClear_{raw}",
                interopType: ArrayElementEncoder.GetArrayMarshallerInteropPath(context, array.BaseType),
                parameterList: $"uint length, {elementAbi}* data");
            writer.WriteLine($"DisposeAndClear_{raw}(null, __{raw}Size, ({elementAbi}*){pointer});");
        }
    }
}
