// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.PE;

#pragma warning disable IDE0046

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Extensions for the <see cref="RuntimeContext"/> type.
/// </summary>
internal static class RuntimeContextExtensions
{
    extension(RuntimeContext runtimeContext)
    {
        /// <summary>
        /// Loads a .NET module into the context from the provided input file.
        /// </summary>
        /// <param name="filePath">The file path to the input executable to load.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public ModuleDefinition LoadModule(string filePath)
        {
            AssemblyDefinition assemblyDefinition = runtimeContext.LoadAssembly(filePath);

            // Every valid .NET assembly will always have exactly one module. In practice, we should
            // never encounter an assembly with zero or more than one module, but we can still check
            // and ensure that this is the case, just to guard against malformed .NET assemblies too.
            if (assemblyDefinition.Modules is not [ModuleDefinition moduleDefinition])
            {
                throw new BadImageFormatException();
            }

            return moduleDefinition;
        }

        /// <summary>
        /// Loads a .NET module into the context from the provided PE image.
        /// </summary>
        /// <param name="peImage">The PE image containing the .NET metadata.</param>
        /// <returns>The module.</returns>
        /// <exception cref="BadImageFormatException">Occurs when the image does not contain a valid .NET metadata directory.</exception>
        public ModuleDefinition LoadModule(PEImage peImage)
        {
            AssemblyDefinition assemblyDefinition = runtimeContext.LoadAssembly(peImage);

            // Every valid .NET assembly will always have exactly one module. In practice, we should
            // never encounter an assembly with zero or more than one module, but we can still check
            // and ensure that this is the case, just to guard against malformed .NET assemblies too.
            if (assemblyDefinition.Modules is not [ModuleDefinition moduleDefinition])
            {
                throw new BadImageFormatException();
            }

            return moduleDefinition;
        }
    }
}
