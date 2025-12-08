// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Xaml;

/// <summary>
/// An interface complementing <see cref="GeneratedBindableCustomPropertyProviderAttribute"/> providing the implementation to expose the specified properties.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.icustompropertyprovider"/>
/// <seealso href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.data.icustompropertyprovider"/>
public interface IBindableCustomPropertyProvider
{
    /// <summary>
    /// Get the generated <see cref="BindableCustomProperty"/> implementation representing the specified property name.
    /// </summary>
    /// <param name="name">The name of the property to get.</param>
    /// <returns>The <see cref="BindableCustomProperty"/> implementation for the property specified by <paramref name="name"/>.</returns>
    BindableCustomProperty GetProperty(ReadOnlySpan<char> name);

    /// <summary>
    /// Get the generated <see cref="BindableCustomProperty"/> implementation representing the specified index property type.
    /// </summary>
    /// <param name="indexParameterType">The index property to get.</param>
    /// <returns>The <see cref="BindableCustomProperty"/> implementation for the property specified by <paramref name="indexParameterType"/>.</returns>
    BindableCustomProperty GetProperty(Type indexParameterType);
}
