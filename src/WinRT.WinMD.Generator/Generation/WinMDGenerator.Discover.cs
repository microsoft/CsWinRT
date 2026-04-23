// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
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
    /// Sets up a <see cref="PathAssemblyResolver"/> using search directories from all reference assembly
    /// paths to handle type forwarding scenarios where a referenced assembly forwards to another assembly
    /// in the same directory. The assembly is loaded into a <c>.NETCoreApp 10.0</c> runtime context.
    /// </para>
    /// <para>
    /// Type discovery is performed by <see cref="Discovery.AssemblyAnalyzer"/>, which collects all public
    /// classes, interfaces, structs, enums, and delegates (including nested public types).
    /// </para>
    /// </remarks>
    /// <param name="args">The parsed generator arguments.</param>
    /// <returns>A <see cref="WinMDGeneratorDiscoveryState"/> containing the loaded module and discovered types.</returns>
    private static WinMDGeneratorDiscoveryState Discover(WinMDGeneratorArgs args)
    {
        ModuleDefinition inputModule;

        try
        {
            string[] allReferencePaths = [args.InputAssemblyPath, .. args.ReferenceAssemblyPaths];

            // Use the built-in PathAssemblyResolver with both explicit paths and search directories
            // to handle type forwarding scenarios where a referenced assembly forwards to another
            // assembly in the same directory.
            string[] searchDirectories = allReferencePaths
                .Select(Path.GetDirectoryName)
                .Where(d => d is not null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()!;

            PathAssemblyResolver assemblyResolver = PathAssemblyResolver.FromSearchDirectories(searchDirectories);

            DotNetRuntimeInfo targetRuntime = new(".NETCoreApp", new Version(10, 0));
            RuntimeContext runtimeContext = new(targetRuntime, assemblyResolver);

            inputModule = runtimeContext.LoadModule(args.InputAssemblyPath);
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