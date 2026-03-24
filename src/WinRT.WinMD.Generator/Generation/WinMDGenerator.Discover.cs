// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using WindowsRuntime.WinMDGenerator.Discovery;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.WinMDGenerator.Resolvers;

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
            PathAssemblyResolver assemblyResolver = new(allReferencePaths);
            inputModule = ModuleDefinition.FromFile(args.InputAssemblyPath, assemblyResolver.ReaderParameters);
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