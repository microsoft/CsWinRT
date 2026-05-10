// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;
using WindowsRuntime.ProjectionWriter.Helpers;
using AsmResolver.DotNet.Signatures;

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
}
