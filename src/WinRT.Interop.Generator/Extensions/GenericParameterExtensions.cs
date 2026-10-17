// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="GenericParameter"/> type.
/// </summary>
internal static class GenericParameterExtensions
{
    extension(GenericParameter parameter)
    {
        /// <summary>
        /// Gets whether a given generic parameter is covariant or contravariant.
        /// </summary>
        public bool IsVariant => (parameter.Attributes & GenericParameterAttributes.VarianceMask) != 0;
    }
}
