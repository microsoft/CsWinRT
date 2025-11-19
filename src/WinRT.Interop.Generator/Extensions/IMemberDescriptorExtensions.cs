// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="IMemberDescriptor"/> type.
/// </summary>
internal static class IMemberDescriptorExtensions
{
    extension(IMemberDescriptor descriptor)
    {
        /// <summary>
        /// When this member is defined in a type, gets the top level enclosing type.
        /// </summary>
        public ITypeDescriptor? TopLevelDeclaringType
        {
            get
            {
                ITypeDescriptor? currentType = descriptor.DeclaringType;

                // If the type has no declaring type, just stop here immediately
                if (currentType is null)
                {
                    return null;
                }

                while (true)
                {
                    ITypeDescriptor? declaringType = currentType.DeclaringType;

                    // We've reached the top level declaring type, so we can return it
                    if (declaringType is null)
                    {
                        return currentType;
                    }

                    currentType = declaringType;
                }
            }
        }
    }
}