// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Errors;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDGenerator"/>
internal static partial class WinMDGenerator
{
    /// <summary>
    /// Loads the input assembly and discovers all public types that should be included in the WinMD.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The assembly is first loaded as a raw PE image so that its target .NET runtime version can be
    /// probed from the metadata. This avoids hardcoding a .NET version and ensures the runtime context
    /// is configured to match the actual assembly. The same PE image is then reused to load the module
    /// into the runtime context, avoiding a redundant file read.
    /// </para>
    /// <para>
    /// A <see cref="PathAssemblyResolver"/> is set up using search directories from all reference assembly
    /// paths to handle type forwarding scenarios where a referenced assembly forwards to another assembly
    /// in the same directory.
    /// </para>
    /// <para>
    /// Type discovery is performed by <see cref="AssemblyAnalyzer"/>, which collects all public
    /// classes, interfaces, structs, enums, and delegates (including nested public types).
    /// </para>
    /// </remarks>
    /// <param name="args">The parsed generator arguments.</param>
    /// <returns>A <see cref="WinMDGeneratorDiscoveryState"/> containing the loaded module and discovered types.</returns>
    private static WinMDGeneratorDiscoveryState Discover(WinMDGeneratorArgs args)
    {
        PEImage inputAssemblyImage;

        // Load the input assembly as a PE image first, so we can probe the .NET version
        try
        {
            inputAssemblyImage = PEImage.FromFile(args.InputAssemblyPath);
        }
        catch (Exception e)
        {
            throw WellKnownWinMDExceptions.InputAssemblyLoadError(e);
        }

        // Probe the .NET runtime version from the input assembly metadata
        if (!TargetRuntimeProber.TryGetLikelyTargetRuntime(inputAssemblyImage, out DotNetRuntimeInfo targetRuntime))
        {
            throw WellKnownWinMDExceptions.InputAssemblyRuntimeVersionNotFound(args.InputAssemblyPath);
        }

        // Set up the assembly resolver with search directories from all reference paths.
        // This handles type forwarding scenarios where a referenced assembly forwards to
        // another assembly in the same directory.
        string[] allReferencePaths = [args.InputAssemblyPath, .. args.ReferenceAssemblyPaths];

        string[] searchDirectories = allReferencePaths
            .Select(Path.GetDirectoryName)
            .Where(directory => directory is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;

        PathAssemblyResolver assemblyResolver = PathAssemblyResolver.FromSearchDirectories(searchDirectories);

        // Initialize the runtime context with the probed version (this will be reused to allow caching)
        RuntimeContext runtimeContext = new(targetRuntime, assemblyResolver);

        ModuleDefinition inputModule;

        // Load the module from the already-loaded PE image, reusing it to avoid a redundant file read
        try
        {
            inputModule = runtimeContext.LoadModule(inputAssemblyImage);
        }
        catch (Exception e)
        {
            throw WellKnownWinMDExceptions.InputAssemblyLoadError(e);
        }

        AssemblyAnalyzer analyzer = new(inputModule);

        return new WinMDGeneratorDiscoveryState
        {
            InputModule = inputModule,
            AssemblyName = analyzer.AssemblyName,
            PublicTypes = analyzer.DiscoverPublicTypes()
        };
    }
}