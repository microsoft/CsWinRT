// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Resources;
using System.Runtime.CompilerServices;

#nullable enable

namespace WinRT
{
    /// <summary>
    /// A strongly-typed resource class, for looking up localized strings, etc.
    /// This is manually written to add support for trimming all resources when
    /// the corresponding 'CsWinRTUseExceptionResourceKeys' is set, to save size.
    /// </summary>
    internal static class WinRTRuntimeErrorStrings
    {
        private static ResourceManager? s_resourceManager;

        /// <summary>
        /// Gets the cached <see cref="System.Resources.ResourceManager"/> instance used by this class.
        /// </summary>
        private static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new("WinRT.WinRTRuntimeErrorStrings", typeof(WinRTRuntimeErrorStrings).Assembly));
        
        internal static string Arg_IndexOutOfRangeException => GetResourceString();

        internal static string Arg_KeyNotFound => GetResourceString();

        internal static string Arg_KeyNotFoundWithKey => GetResourceString();

        internal static string Arg_RankMultiDimNotSupported => GetResourceString();

        internal static string Argument_AddingDuplicate => GetResourceString();

        internal static string Argument_AddingDuplicateWithKey => GetResourceString();

        internal static string Argument_IndexOutOfArrayBounds => GetResourceString();

        internal static string Argument_InsufficientSpaceToCopyCollection => GetResourceString();

        internal static string ArgumentOutOfRange_Index => GetResourceString();

        internal static string ArgumentOutOfRange_IndexLargerThanMaxValue => GetResourceString();

        internal static string InvalidOperation_CannotRemoveLastFromEmptyCollection => GetResourceString();

        internal static string InvalidOperation_CollectionBackingDictionaryTooLarge => GetResourceString();

        internal static string InvalidOperation_CollectionBackingListTooLarge => GetResourceString();

        internal static string InvalidOperation_EnumEnded => GetResourceString();

        internal static string InvalidOperation_EnumFailedVersion => GetResourceString();

        internal static string InvalidOperation_EnumNotStarted => GetResourceString();

        internal static string NotSupported_KeyCollectionSet => GetResourceString();

        internal static string NotSupported_ValueCollectionSet => GetResourceString();

        /// <summary>
        /// Gets a given resource string, while also respecting the feature switches in use.
        /// </summary>
        /// <param name="resourceKey">The resource key to retrieve.</param>
        /// <returns>The resulting resource string (or just the key, if the feature switch is set).</returns>
        private static string GetResourceString([CallerMemberName] string? resourceKey = null)
        {
            // If the feature switch is set, just return the resource key itself.
            // This allows trimming the resource manager and embedded resources.
            if (FeatureSwitches.UseExceptionResourceKeys)
            {
                return resourceKey!;
            }

            // Normal path retrieving the resource from the embedded .resx files.
            // The returned string should always be not-null (unless it's missing).
            return ResourceManager.GetString(resourceKey!)!;
        }
    }
}

// Restore in case this file is merged with others.
#nullable restore