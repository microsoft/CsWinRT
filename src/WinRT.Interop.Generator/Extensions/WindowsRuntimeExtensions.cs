// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for Windows Runtime types.
/// </summary>
internal static class WindowsRuntimeExtensions
{
    extension(IHasCustomAttribute type)
    {
        /// <summary>
        /// Checks whether a <see cref="IHasCustomAttribute"/> represents a projected Windows Runtime type.
        /// </summary>
        /// <returns>Whether the type represents a projected Windows Runtime type.</returns>
        public bool IsProjectedWindowsRuntimeType => type.HasCustomAttribute("WinRT", "WindowsRuntimeTypeAttribute");
    }

    extension(TypeDefinition type)
    {
        /// <summary>
        /// Checks whether a <see cref="TypeDefinition"/> represents a projected Windows Runtime class type.
        /// </summary>
        /// <returns>Whether the type represents a projected Windows Runtime class type.</returns>
        public bool IsProjectedWindowsRuntimeClassType
        {
            get
            {
                // We only care about classes
                if (type is not { IsClass: true, IsValueType: false, IsDelegate: false })
                {
                    return false;
                }

                // Ignore static types
                if (type.IsAbstract && type.IsSealed)
                {
                    return false;
                }

                // The type also must be a projected type
                return type.IsProjectedWindowsRuntimeType;
            }
        }
    }

    extension(ModuleDefinition module)
    {
        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> is or references the Windows SDK projections assembly.
        /// </summary>
        /// <returns>Whether the module is or references the Windows SDK projections assembly.</returns>
        public bool IsOrReferencesWindowsSDKProjectionsAssembly
        {
            get
            {
                // If the assembly references the Windows SDK projections, gather it
                foreach (AssemblyReference reference in module.AssemblyReferences)
                {
                    if (reference.Name?.AsSpan().SequenceEqual(WellKnownInteropNames.WindowsSDKDllNameUtf8[..^4]) is true)
                    {
                        return true;
                    }
                }

                // Otherwise, check if it's the Windows SDK projections assembly itself
                return module.Name?.AsSpan().SequenceEqual(WellKnownInteropNames.WindowsSDKDllNameUtf8) is true;
            }
        }
    }
}
