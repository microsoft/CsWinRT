// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.PE.Builder;

namespace WindowsRuntime.InteropGenerator.Writers;

/// <summary>
/// Writes interop modules with the default AsmResolver configuration, reusing work that is repeated in method bodies.
/// </summary>
internal static class InteropModuleWriter
{
    /// <summary>
    /// Writes a module to a file, like <see cref="ModuleDefinition.Write(string)"/>.
    /// </summary>
    /// <param name="module">The module to write.</param>
    /// <param name="path">The path of the file to create or overwrite.</param>
    public static void Write(ModuleDefinition module, string path)
    {
        using FileStream stream = File.Create(path);

        Write(module, stream);
    }

    /// <summary>
    /// Writes a module to a stream, like <see cref="ModuleDefinition.Write(Stream)"/>.
    /// </summary>
    /// <param name="module">The module to write.</param>
    /// <param name="stream">The stream to write to.</param>
    public static void Write(ModuleDefinition module, Stream stream)
    {
        // Same as the defaults of 'ModuleDefinition.Write', except for the wrapped method body serializer.
        // All state is created for this write only, so nothing can be observed by a different write.
        DiagnosticBag diagnostics = new();
        DotNetDirectoryFactory factory = new();

        factory.MethodBodySerializer = new MemoizingMethodBodySerializer(factory.MethodBodySerializer, diagnostics);

        module.Write(stream, new ManagedPEImageBuilder(factory, diagnostics), new ManagedPEFileBuilder());
    }
}
