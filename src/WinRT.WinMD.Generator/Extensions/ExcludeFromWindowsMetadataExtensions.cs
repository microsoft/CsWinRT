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
            return declaration.CustomAttributes.Any(static attribute =>
                attribute.Constructor?.DeclaringType?.FullName == "WindowsRuntime.ExcludeFromWindowsMetadataAttribute");
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

            return type?.IsExcludedFromWindowsMetadata() == true ||
                   (declaration is MethodDefinition accessor &&
                    type is not null &&
                   (type.Properties.Any(property =>
                        (property.GetMethod == accessor || property.SetMethod == accessor) &&
                        property.IsExcludedFromWindowsMetadata()) ||
                    type.Events.Any(@event =>
                        (@event.AddMethod == accessor || @event.RemoveMethod == accessor) &&
                        @event.IsExcludedFromWindowsMetadata())));
        }
    }
}