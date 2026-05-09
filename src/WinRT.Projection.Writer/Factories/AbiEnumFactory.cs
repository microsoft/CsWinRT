// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the ABI marshaller class and the IReference&lt;T&gt; impl for a projected enum type.
/// </summary>
internal static class AbiEnumFactory
{
    /// <summary>Writes the ABI marshaller class and IReference impl for an enum type.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The enum type definition.</param>
    public static void Write(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        CodeWriters.WriteStructEnumMarshallerClass(writer, context, type);
        ReferenceImplFactory.Write(writer, context, type);

        // In component mode, also emit the authoring metadata wrapper for enums.
        if (context.Settings.Component)
        {
            AbiClassFactory.WriteAuthoringMetadataType(writer, context, type);
        }
    }
}
