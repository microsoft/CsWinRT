// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Writers;
using WindowsRuntime.ProjectionWriter.Generation;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the ABI marshaller class and the IReference&lt;T&gt; impl for a projected enum type.
/// </summary>
internal static class AbiEnumFactory
{
    /// <summary>
    /// Writes the ABI marshaller class and IReference impl for an enum type.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The enum type definition.</param>
    public static void WriteAbiEnum(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        StructEnumMarshallerFactory.WriteStructEnumMarshallerClass(writer, context, type);
        ReferenceImplFactory.WriteReferenceImpl(writer, context, type);

        // In component mode, also emit the authoring metadata wrapper for enums.
        if (context.Settings.Component)
        {
            AbiClassFactory.WriteAuthoringMetadataType(writer, context, type);
        }
    }
}
