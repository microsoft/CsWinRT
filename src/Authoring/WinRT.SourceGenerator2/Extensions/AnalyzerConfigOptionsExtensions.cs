// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WindowsRuntime.SourceGenerator.Extensions;

/// <summary>
/// Extensions for <see cref="AnalyzerConfigOptions"/>.
/// </summary>
internal static class AnalyzerConfigOptionsExtensions
{
    extension(AnalyzerConfigOptions options)
    {
        /// <summary>
        /// Gets the value of the <c>"PublishAot"</c> property.
        /// </summary>
        /// <returns>The value of the <c>"PublishAot"</c> property.</returns>
        public bool GetPublishAot()
        {
            return options.GetBooleanProperty("PublishAot");
        }

        /// <summary>
        /// Tries to get the value of a boolean MSBuild property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The resulting property value.</param>
        /// <returns>Whether <paramref name="propertyValue"/> was retrieved successfully.</returns>
        public bool TryGetBooleanProperty(ReadOnlySpan<char> propertyName, out bool propertyValue)
        {
            if (options.TryGetValue($"build_property.{propertyName}", out string? propertyText))
            {
                return bool.TryParse(propertyText, out propertyValue);
            }

            propertyValue = false;

            return false;
        }

        /// <summary>
        /// Get the value of a boolean MSBuild property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="defaultValue">The default value to use if the property is not found.</param>
        /// <returns>The resulting property value.</returns>
        public bool GetBooleanProperty(ReadOnlySpan<char> propertyName, bool defaultValue = false)
        {
            return options.TryGetBooleanProperty(propertyName, out bool propertyValue) ? propertyValue : defaultValue;
        }
    }
}
