// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.PE.Builder;

namespace WindowsRuntime.InteropGenerator.Writers;

/// <summary>
/// Writes interop modules with the default AsmResolver configuration, reusing work that would otherwise be repeated.
/// </summary>
internal static class InteropModuleWriter
{
    /// <summary>
    /// Writes a module to a file, like <see cref="ModuleDefinition.Write(string)"/>.
    /// </summary>
    /// <param name="module">The module to write.</param>
    /// <param name="path">The path of the file to create or overwrite.</param>
    /// <param name="payloads">The serialized custom attribute payloads to reuse, which are consumed by this write.</param>
    public static void Write(ModuleDefinition module, string path, CustomAttributePayloadCache? payloads = null)
    {
        // Take the payloads before creating the file, so they are consumed (and validated) even if that fails
        using (payloads?.SubstituteSignatures(module))
        {
            using FileStream stream = File.Create(path);

            WriteCore(module, stream);
        }
    }

    /// <summary>
    /// Writes a module to a stream, like <see cref="ModuleDefinition.Write(Stream)"/>.
    /// </summary>
    /// <param name="module">The module to write.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="payloads">The serialized custom attribute payloads to reuse, which are consumed by this write.</param>
    public static void Write(ModuleDefinition module, Stream stream, CustomAttributePayloadCache? payloads = null)
    {
        // Original signatures are restored (and the payloads discarded) even if the write fails
        using (payloads?.SubstituteSignatures(module))
        {
            WriteCore(module, stream);
        }
    }

    /// <summary>
    /// Writes a module to a stream, with any payload substitution already in place.
    /// </summary>
    /// <param name="module">The module to write.</param>
    /// <param name="stream">The stream to write to.</param>
    private static void WriteCore(ModuleDefinition module, Stream stream)
    {
        // Same as the defaults of 'ModuleDefinition.Write', except for the wrapped method body serializer.
        // All state is created for this write only, so nothing can be observed by a different write.
        DiagnosticBag diagnostics = new();
        DotNetDirectoryFactory factory = new();

        factory.MethodBodySerializer = new MemoizingMethodBodySerializer(factory.MethodBodySerializer, diagnostics);

        module.Write(stream, new ManagedPEImageBuilder(factory, diagnostics), new ManagedPEFileBuilder());
    }
}
