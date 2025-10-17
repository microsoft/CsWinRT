// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime;

/// <summary>
/// A container for all shared <see cref="AppContext"/> configuration switches for CsWinRT.
/// </summary>
/// <remarks>
/// This type uses a very specific setup for configuration switches to ensure ILLink can work the best.
/// This mirrors the architecture of feature switches in the runtime as well, and it's needed so that
/// no static constructor is generated for the type.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.featureswitchdefinitionattribute"/>
/// <seealso href="https://github.com/dotnet/runtime/blob/main/docs/workflow/trimming/feature-switches.md#adding-new-feature-switch"/>
internal static class WindowsRuntimeFeatureSwitches
{
    /// <summary>
    /// The configuration property name for <see cref="EnableManifestFreeActivation"/>.
    /// </summary>
    private const string EnableManifestFreeActivationPropertyName = "CSWINRT_ENABLE_MANIFEST_FREE_ACTIVATION";

    /// <summary>
    /// The configuration property name for <see cref="ManifestFreeActivationReportOriginalException"/>.
    /// </summary>
    private const string ManifestFreeActivationReportOriginalExceptionPropertyName = "CSWINRT_MANIFEST_FREE_ACTIVATION_REPORT_ORIGINAL_EXCEPTION";

    /// <summary>
    /// The configuration property name for <see cref="UseWindowsUIXamlProjections"/>.
    /// </summary>
    private const string UseWindowsUIXamlProjectionsPropertyName = "CSWINRT_USE_WINDOWS_UI_XAML_PROJECTIONS";

    /// <summary>
    /// The configuration property name for <see cref="EnableDefaultCustomTypeMappings"/>.
    /// </summary>
    private const string EnableDefaultCustomTypeMappingsPropertyName = "CSWINRT_ENABLE_DEFAULT_CUSTOM_TYPE_MAPPINGS";

    /// <summary>
    /// Gets a value indicating whether or not manifest free WinRT activation is supported (defaults to <see langword="true"/>).
    /// </summary>
    [FeatureSwitchDefinition(EnableManifestFreeActivationPropertyName)]
    public static bool EnableManifestFreeActivation { get; } = GetConfigurationValue(EnableManifestFreeActivationPropertyName, defaultValue: true);

    /// <summary>
    /// Gets a value indicating whether or not the original exception should be thrown if activation fails when <see cref="EnableManifestFreeActivation"/> is disabled (defaults to <see langword="false"/>).
    /// </summary>
    [FeatureSwitchDefinition(ManifestFreeActivationReportOriginalExceptionPropertyName)]
    public static bool ManifestFreeActivationReportOriginalException { get; } = GetConfigurationValue(ManifestFreeActivationReportOriginalExceptionPropertyName, defaultValue: false);

    /// <summary>
    /// Gets a value indicating whether to project .NET types to their <c>Windows.UI.Xaml</c> equivalents instead of their <c>Microsoft.UI.Xaml</c> equivalents.
    /// </summary>
    [FeatureSwitchDefinition(UseWindowsUIXamlProjectionsPropertyName)]
    public static bool UseWindowsUIXamlProjections { get; } = GetConfigurationValue(UseWindowsUIXamlProjectionsPropertyName, defaultValue: false);

    /// <summary>
    /// Gets a value indicating whether or not should initialize all default type mappings automatically (defaults to <see langword="true"/>).
    /// </summary>
    public static bool EnableDefaultCustomTypeMappings { get; } = GetConfigurationValue(EnableDefaultCustomTypeMappingsPropertyName, defaultValue: false);

    /// <summary>
    /// Gets a configuration value for a specified property.
    /// </summary>
    /// <param name="propertyName">The property name to retrieve the value for.</param>
    /// <param name="defaultValue">The default property value to use as a fallback.</param>
    /// <returns>The value of the specified configuration setting.</returns>
    private static bool GetConfigurationValue([ConstantExpected] string propertyName, [ConstantExpected] bool defaultValue)
    {
        return AppContext.TryGetSwitch(propertyName, out bool isEnabled) ? isEnabled : defaultValue;
    }
}
