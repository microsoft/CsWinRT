// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WinRT;

/// <summary>
/// A container for all shared <see cref="AppContext"/> configuration switches for CsWinRT.
/// </summary>
/// <remarks>
/// <para>
/// This type uses a very specific setup for configuration switches to ensure ILLink can work the best.
/// This mirrors the architecture of feature switches in the runtime as well, and it's needed so that
/// no static constructor is generated for the type.
/// </para>
/// <para>
/// For more info, see <see href="https://github.com/dotnet/runtime/blob/main/docs/workflow/trimming/feature-switches.md#adding-new-feature-switch"/>.
/// </para>
/// </remarks>
internal static class FeatureSwitches
{
    /// <summary>
    /// The configuration property name for <see cref="IsDebugOutputEnabled"/>.
    /// </summary>
    private const string IsDynamicObjectsSupportEnabledPropertyName = "CSWINRT_ENABLE_DYNAMIC_OBJECTS_SUPPORT";

    /// <summary>
    /// The backing field for <see cref="IsDynamicObjectsSupportEnabled"/>.
    /// </summary>
    private static int _isDynamicObjectsSupportEnabled;

    /// <summary>
    /// Gets a value indicating whether or not projections support for dynamic objects is enabled (defaults to <see langword="true"/>).
    /// </summary>
    public static bool IsDynamicObjectsSupportEnabled
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetConfigurationValue(IsDynamicObjectsSupportEnabledPropertyName, ref _isDynamicObjectsSupportEnabled);
    }

    /// <summary>
    /// Gets a configuration value for a specified property.
    /// </summary>
    /// <param name="propertyName">The property name to retrieve the value for.</param>
    /// <param name="cachedResult">The cached result for the target configuration value.</param>
    /// <returns>The value of the specified configuration setting.</returns>
    private static bool GetConfigurationValue(string propertyName, ref int cachedResult)
    {
        // The cached switch value has 3 states:
        //   0: unknown.
        //   1: true
        //   -1: false
        //
        // This method doesn't need to worry about concurrent accesses to the cached result,
        // as even if the configuration value is retrieved twice, that'll always be the same.
        if (cachedResult < 0)
        {
            return false;
        }

        if (cachedResult > 0)
        {
            return true;
        }

        // Get the configuration switch value, or its default
        if (!AppContext.TryGetSwitch(propertyName, out bool isEnabled))
        {
            isEnabled = GetDefaultConfigurationValue(propertyName);
        }

        // Update the cached result
        cachedResult = isEnabled ? 1 : -1;

        return isEnabled;
    }

    /// <summary>
    /// Gets the default configuration value for a given feature switch.
    /// </summary>
    /// <param name="propertyName">The property name to retrieve the value for.</param>
    /// <returns>The default value for the target <paramref name="propertyName"/>.</returns>
    private static bool GetDefaultConfigurationValue(string propertyName)
    {
        if (propertyName == IsDynamicObjectsSupportEnabledPropertyName)
        {
            return true;
        }

        return false;
    }
}