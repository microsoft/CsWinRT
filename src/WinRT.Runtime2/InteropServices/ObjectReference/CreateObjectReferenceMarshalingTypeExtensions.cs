// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Windows.Foundation.Metadata;

#pragma warning disable IDE0046, IDE0072

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="CreateObjectReferenceMarshalingType"/> type.
/// </summary>
internal static class CreateObjectReferenceMarshalingTypeExtensions
{
    extension(CreateObjectReferenceMarshalingType)
    {
        /// <summary>
        /// Creates a <see cref="CreateObjectReferenceMarshalingType"/> value from a given <see cref="MarshalingType"/> value.
        /// </summary>
        /// <param name="marshalingType">The input <see cref="MarshalingType"/> value to convert.</param>
        /// <returns>The <see cref="CreateObjectReferenceMarshalingType"/> value corresponding to <paramref name="marshalingType"/>.</returns>
        [SupportedOSPlatform("Windows10.0.10240.0")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CreateObjectReferenceMarshalingType FromMetadata(MarshalingType marshalingType)
        {
            // Match the statically known marshalling types to the corresponding internal value to enable
            // additional optimizations. We're intentionally not using a 'switch' statement here because
            // the codegen for it is a bit more convoluted, and we want this method to be easily inlined.
            if (marshalingType == MarshalingType.Agile)
            {
                return CreateObjectReferenceMarshalingType.Agile;
            }

            if (marshalingType == MarshalingType.Standard)
            {
                return CreateObjectReferenceMarshalingType.Standard;
            }

            // For any other case, we just fallback to the original behavior, being that we have no available
            // static type information. We could change this theory to e.g. just throw if the type is 'None'.
            return CreateObjectReferenceMarshalingType.Unknown;
        }
    }
}