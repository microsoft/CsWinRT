// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.ApplicationSettings;

namespace WindowsRuntime.Internal;

/// <summary>
/// COM interop interface for retrieving an <see cref="AccountsSettingsPane"/> bound to a Win32 window.
/// </summary>
/// <remarks>
/// See <see href="https://learn.microsoft.com/windows/win32/api/accountssettingspaneinterop/">accountssettingspaneinterop.idl</see>.
/// </remarks>
[ProjectionInternal]
[Guid("D3EE12AD-3865-4362-9746-B75A682DF0E6")]
public interface IAccountsSettingsPaneInterop
{
    /// <summary>
    /// Gets the <see cref="AccountsSettingsPane"/> associated with the specified window.
    /// </summary>
    AccountsSettingsPane GetForWindow(
        HWND appWindow,
        ref Guid riid);

    /// <summary>
    /// Shows the manage-accounts UI for the specified window.
    /// </summary>
    IAsyncAction ShowManageAccountsForWindowAsync(
        HWND appWindow,
        ref Guid riid);

    /// <summary>
    /// Shows the add-account UI for the specified window.
    /// </summary>
    IAsyncAction ShowAddAccountForWindowAsync(
        HWND appWindow,
        ref Guid riid);
}
