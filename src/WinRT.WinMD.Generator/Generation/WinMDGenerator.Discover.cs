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
    /// Loads the input assembly and discovers public types.
    /// </summary>
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