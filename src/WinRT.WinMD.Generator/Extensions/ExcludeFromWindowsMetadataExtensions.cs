// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Identifies declarations excluded from component metadata.
/// </summary>
internal static class ExcludeFromWindowsMetadataExtensions
{
    extension(IHasCustomAttribute declaration)
    {
        /// <summary>
        /// Gets whether the declaration directly carries the metadata exclusion marker.
        /// </summary>
        public bool HasExcludeFromWindowsMetadata()
        {
            return declaration.FindCustomAttributes("WindowsRuntime.InteropServices", "ExcludeFromWindowsMetadataAttribute").Any();
        }

        /// <summary>
        /// Gets whether a declaration or its containing type is excluded from component metadata.
        /// </summary>
        public bool IsExcludedFromWindowsMetadata()
        {
            if (declaration.HasExcludeFromWindowsMetadata())
            {
                return true;
            }

            TypeDefinition? type = declaration switch
            {
                TypeDefinition nested => nested.DeclaringType,
                MethodDefinition method => method.DeclaringType,
                PropertyDefinition property => property.DeclaringType,
                EventDefinition @event => @event.DeclaringType,
                FieldDefinition field => field.DeclaringType,
                _ => null
            };

            // Nested types and members follow exclusions on their containing type
            if (type?.IsExcludedFromWindowsMetadata() == true)
            {
                return true;
            }

            // Accessors also follow the property or event identified by their method semantics
            return declaration is MethodDefinition accessor &&
                   accessor.IsAccessorMethod &&
                   accessor.Semantics?.Association is IHasCustomAttribute associated &&
                   associated.IsExcludedFromWindowsMetadata();
        }
    }
}