// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WinRT
{
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
        /// The configuration property name for <see cref="EnableDynamicObjectsSupport"/>.
        /// </summary>
        private const string EnableDynamicObjectsSupportPropertyName = "CSWINRT_ENABLE_DYNAMIC_OBJECTS_SUPPORT";

        /// <summary>
        /// The configuration property name for <see cref="UseExceptionResourceKeys"/>.
        /// </summary>
        private const string UseExceptionResourceKeysPropertyName = "CSWINRT_USE_EXCEPTION_RESOURCE_KEYS";

        /// <summary>
        /// The configuration property name for <see cref="EnableDefaultCustomTypeMappings"/>.
        /// </summary>
        private const string EnableDefaultCustomTypeMappingsPropertyName = "CSWINRT_ENABLE_DEFAULT_CUSTOM_TYPE_MAPPINGS";

        /// <summary>
        /// The configuration property name for <see cref="EnableICustomPropertyProviderSupport"/>.
        /// </summary>
        private const string EnableICustomPropertyProviderSupportPropertyName = "CSWINRT_ENABLE_ICUSTOMPROPERTYPROVIDER_SUPPORT";

        /// <summary>
        /// The configuration property name for <see cref="EnableIReferenceSupport"/>.
        /// </summary>
        private const string EnableIReferenceSupportPropertyName = "CSWINRT_ENABLE_IREFERENCE_SUPPORT";

        /// <summary>
        /// The configuration property name for <see cref="EnableIDynamicInterfaceCastableSupport"/>.
        /// </summary>
        private const string EnableIDynamicInterfaceCastableSupportPropertyName = "CSWINRT_ENABLE_IDYNAMICINTERFACECASTABLE";

        /// <summary>
        /// The configuration property name for <see cref="IsWuxMode"/>.
        /// </summary>
        private const string IsWuxModePropertyName = "CSWINRT_ENABLE_WUX_MODE";

        /// <summary>
        /// The backing field for <see cref="EnableDynamicObjectsSupport"/>.
        /// </summary>
        private static int _enableDynamicObjectsSupport;

        /// <summary>
        /// The backing field for <see cref="UseExceptionResourceKeys"/>.
        /// </summary>
        private static int _useExceptionResourceKeys;

        /// <summary>
        /// The backing field for <see cref="EnableDefaultCustomTypeMappings"/>.
        /// </summary>
        private static int _enableDefaultCustomTypeMappings;

        /// <summary>
        /// The backing field for <see cref="EnableICustomPropertyProviderSupport"/>.
        /// </summary>
        private static int _enableICustomPropertyProviderSupport;

        /// <summary>
        /// The backing field for <see cref="EnableIReferenceSupport"/>.
        /// </summary>
        private static int _enableIReferenceSupport;

        /// <summary>
        /// The backing field for <see cref="EnableIDynamicInterfaceCastableSupport"/>.
        /// </summary>
        private static int _enableIDynamicInterfaceCastableSupport;

        /// <summary>
        /// The backing field for <see cref="IsWuxMode"/>.
        /// </summary>
        private static int _isWuxMode;

        /// <summary>
        /// Gets a value indicating whether or not projections support for dynamic objects is enabled (defaults to <see langword="true"/>).
        /// </summary>
        public static bool EnableDynamicObjectsSupport
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetConfigurationValue(EnableDynamicObjectsSupportPropertyName, ref _enableDynamicObjectsSupport, true);
        }

        /// <summary>
        /// Gets a value indicating whether or not exceptions should use resource keys rather than localized messages (defaults to <see langword="false"/>).
        /// </summary>
        public static bool UseExceptionResourceKeys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetConfigurationValue(UseExceptionResourceKeysPropertyName, ref _useExceptionResourceKeys, false);
        }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="Projections"/> should initialize all default type mappings automatically (defaults to <see langword="true"/>).
        /// </summary>
        public static bool EnableDefaultCustomTypeMappings
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetConfigurationValue(EnableDefaultCustomTypeMappingsPropertyName, ref _enableDefaultCustomTypeMappings, true);
        }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="ABI.Microsoft.UI.Xaml.Data.ManagedCustomPropertyProviderVftbl"/> should be enabled (defaults to <see langword="true"/>).
        /// </summary>
        public static bool EnableICustomPropertyProviderSupport
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetConfigurationValue(EnableICustomPropertyProviderSupportPropertyName, ref _enableICustomPropertyProviderSupport, true);
        }

        /// <summary>
        /// Gets a value indicating whether or not <c>IReference&lt;T&gt;</c>, <c>IReferenceArray&lt;T&gt;</c> and <c>IPropertyValue</c> CCW implementations should be supported (defaults to <see langword="true"/>).
        /// </summary>
        public static bool EnableIReferenceSupport
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetConfigurationValue(EnableIReferenceSupportPropertyName, ref _enableIReferenceSupport, true);
        }

        /// <summary>
        /// Gets a value indicating whether or not <see cref="System.Runtime.InteropServices.IDynamicInterfaceCastable"/> should be supported by RCW types (defaults to <see langword="true"/>).
        /// </summary>
        public static bool EnableIDynamicInterfaceCastableSupport
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetConfigurationValue(EnableIDynamicInterfaceCastableSupportPropertyName, ref _enableIDynamicInterfaceCastableSupport, true);
        }

        /// <summary>
        /// Gets a configuration value for a specified property.
        /// </summary>
        /// <param name="propertyName">The property name to retrieve the value for.</param>
        /// <param name="cachedResult">The cached result for the target configuration value.</param>
        /// <returns>The value of the specified configuration setting.</returns>
        private static bool GetConfigurationValue(string propertyName, ref int cachedResult, bool defaultValue)
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

            // Get the configuration switch value, or its default.
            // All feature switches have a default set in the .targets file.
            if (!AppContext.TryGetSwitch(propertyName, out bool isEnabled))
            {
                isEnabled = defaultValue;
            }

            // Update the cached result
            cachedResult = isEnabled ? 1 : -1;

         return isEnabled;
        }

        /// <summary>
        /// <c>true</c> if types from .NET are projected to their Windows.UI.Xaml equivalents instead of their Microsoft.UI.Xaml equivalents.
        /// </summary>
        public static bool IsWuxMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetConfigurationValue(IsWuxModePropertyName, ref _isWuxMode, false);
        }
    }
}