// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;

#pragma warning disable IDE0046

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Extension methods for <see cref="TypeDefinition"/>.
/// </summary>
internal static class TypeDefinitionExtensions
{
    extension(TypeDefinition type)
    {
        /// <summary>
        /// Checks whether the type is a Windows Runtime type (has the <c>[WindowsRuntimeType]</c> marker).
        /// </summary>
        /// <remarks>
        /// Types marked with <c>[WindowsRuntimeType]</c> are projected Windows Runtime types that come
        /// from CsWinRT-generated projection assemblies. This marker indicates the type has a corresponding
        /// Windows Runtime definition. The source contract assembly is recorded separately on the centralized
        /// <c>ABI.WindowsRuntimeMetadataTypes</c> lookup type (the <c>WindowsRuntimeAssemblyName</c> property).
        /// </remarks>
        public bool IsWindowsRuntimeType => type.FindCustomAttributes("WindowsRuntime", "WindowsRuntimeTypeAttribute").Any();

        /// <summary>
        /// Checks whether the type is a Windows Runtime API contract (has the <c>[ApiContract]</c> attribute).
        /// </summary>
        /// <remarks>
        /// In C#, API contracts are projected as enums with <c>[ApiContract]</c>, but in WinMD metadata
        /// they are represented as empty structs per the Windows Runtime type system spec.
        /// </remarks>
        public bool IsApiContract => type.FindCustomAttributes("Windows.Foundation.Metadata", "ApiContractAttribute").Any();

        /// <summary>
        /// Checks whether the type already has the <c>[Version]</c> attribute.
        /// </summary>
        public bool HasVersionAttribute => type.FindCustomAttributes("Windows.Foundation.Metadata", "VersionAttribute").Any();

        /// <summary>
        /// Checks whether the type already has the <c>[ContractVersion]</c> attribute.
        /// </summary>
        public bool HasContractVersionAttribute => type.FindCustomAttributes("Windows.Foundation.Metadata", "ContractVersionAttribute").Any();

        /// <summary>
        /// Gets the version number from the type's <c>[Version]</c> attribute, if present.
        /// </summary>
        /// <returns>
        /// The version number as an integer, or <see langword="null"/> if the type does not have a <c>[Version]</c> attribute.
        /// </returns>
        public int? VersionAttributeValue
        {
            get
            {
                if (type.FindCustomAttributes("Windows.Foundation.Metadata", "VersionAttribute").FirstOrDefault() is not CustomAttribute attribute)
                {
                    return null;
                }

                if (attribute.Signature is { FixedArguments: [{ Element: uint version }] })
                {
                    return (int)version;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the Windows Runtime contract assembly name (i.e. the source <c>.winmd</c> module name) for the type, if available.
        /// </summary>
        /// <returns>
        /// The Windows Runtime contract assembly name (e.g. <c>"Microsoft.UI.Xaml"</c>), or <see langword="null"/>
        /// if no mapping is found for the type.
        /// </returns>
        /// <remarks>
        /// For types from projection assemblies (e.g. <c>Microsoft.WinUI</c>), this returns the original Windows Runtime
        /// contract assembly name so the WinMD can reference types correctly. The mapping is no longer carried on each
        /// type: it lives on the centralized <c>ABI.WindowsRuntimeMetadataTypes</c> lookup type in the implementation
        /// projection (so the build-time-only metadata can be trimmed away), and is read from the type's declaring module.
        /// </remarks>
        public string? WindowsRuntimeAssemblyName
        {
            get
            {
                return type.DeclaringModule is { } module &&
                       module.GetWindowsRuntimeMetadataTypesLookup().TryGetValue((type.Namespace?.Value, type.Name?.Value), out string? stem)
                    ? stem
                    : null;
            }
        }
    }
}
