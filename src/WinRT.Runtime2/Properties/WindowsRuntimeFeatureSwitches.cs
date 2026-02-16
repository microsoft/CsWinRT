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
    /// The configuration property name for <see cref="EnableXamlTypeMarshalling"/>.
    /// </summary>
    private const string EnableXamlTypeMarshallingPropertyName = "CSWINRT_ENABLE_XAML_TYPE_MARSHALLING";

    /// <summary>
    /// The configuration property name for <see cref="EnableIDynamicInterfaceCastableSupport"/>.
    /// </summary>
    private const string EnableIDynamicInterfaceCastableSupportPropertyName = "CSWINRT_ENABLE_IDYNAMICINTERFACECASTABLE_SUPPORT";

    /// <summary>
    /// The configuration property name for <see cref="EnableMarshalingTypeValidation"/>.
    /// </summary>
    private const string EnableMarshalingTypeValidationPropertyName = "CSWINRT_ENABLE_MARSHALING_TYPE_VALIDATION";

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
    /// Gets a value indicating whether marshalling <see cref="Type"/> instances is supported.
    /// </summary>
    [FeatureSwitchDefinition(EnableXamlTypeMarshallingPropertyName)]
    public static bool EnableXamlTypeMarshalling { get; } = GetConfigurationValue(EnableXamlTypeMarshallingPropertyName, defaultValue: true);

    /// <summary>
    /// Gets a value indicating whether or not <see cref="System.Runtime.InteropServices.IDynamicInterfaceCastable"/> should be supported by RCW types (defaults to <see langword="true"/>).
    /// </summary>
    [FeatureSwitchDefinition(EnableIDynamicInterfaceCastableSupportPropertyName)]
    public static bool EnableIDynamicInterfaceCastableSupport { get; } = GetConfigurationValue(EnableIDynamicInterfaceCastableSupportPropertyName, defaultValue: true);

    /// <summary>
    /// Gets a value indicating whether or not validation for <see cref="InteropServices.CreateObjectReferenceMarshalingType"/> values should be enabled (defaults to <see langword="false"/>).
    /// </summary>
    [FeatureSwitchDefinition(EnableMarshalingTypeValidationPropertyName)]
    public static bool EnableMarshalingTypeValidation { get; } = GetConfigurationValue(EnableMarshalingTypeValidationPropertyName, defaultValue: false);

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