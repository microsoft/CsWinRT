// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

internal static partial class AbiMethodBodyFactory
{
    /// <summary>
    /// Emits the call to the appropriate marshaller's ConvertToUnmanaged for a runtime class / object input parameter.
    /// </summary>
    internal static void EmitMarshallerConvertToUnmanaged(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToUnmanaged({argName})");
            return;
        }

        // Runtime class / interface: use ABI.<NS>.<Name>Marshaller
        writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, sig)}.ConvertToUnmanaged({argName})");
    }

    /// <inheritdoc cref="EmitMarshallerConvertToUnmanaged(IndentedTextWriter, ProjectionEmitContext, TypeSignature, string)"/>
    /// <returns>A callback emitting the marshaller's ConvertToUnmanaged call.</returns>
    internal static IndentedTextWriterCallback EmitMarshallerConvertToUnmanaged(ProjectionEmitContext context, TypeSignature sig, string argName)
    {
        return writer => EmitMarshallerConvertToUnmanaged(writer, context, sig, argName);
    }

    /// <summary>
    /// Emits the call to the appropriate marshaller's ConvertToManaged for a runtime class / object return value.
    /// </summary>
    internal static void EmitMarshallerConvertToManaged(IndentedTextWriter writer, ProjectionEmitContext context, TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write($"WindowsRuntimeObjectMarshaller.ConvertToManaged({argName})");
            return;
        }

        writer.Write($"{AbiTypeHelpers.GetMarshallerFullName(writer, context, sig)}.ConvertToManaged({argName})");
    }

    /// <inheritdoc cref="EmitMarshallerConvertToManaged(IndentedTextWriter, ProjectionEmitContext, TypeSignature, string)"/>
    /// <returns>A callback emitting the marshaller's ConvertToManaged call.</returns>
    internal static IndentedTextWriterCallback EmitMarshallerConvertToManaged(ProjectionEmitContext context, TypeSignature sig, string argName)
    {
        return writer => EmitMarshallerConvertToManaged(writer, context, sig, argName);
    }
}
