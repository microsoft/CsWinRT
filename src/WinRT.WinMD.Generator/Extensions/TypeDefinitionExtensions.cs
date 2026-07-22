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
    }
}
