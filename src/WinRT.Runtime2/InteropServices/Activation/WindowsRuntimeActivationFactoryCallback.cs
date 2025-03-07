// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A callback to activate a Windows Runtime object with custom logic.
/// </summary>
/// <param name="additionalParameters">The additional parameters to provide to the activation callback.</param>
/// <param name="baseInterface">The <see cref="WindowsRuntimeObject"/> instance being constructed.</param>
/// <param name="innerInterface">The resulting non-delegating <c>IInspectable</c> object.</param>
/// <param name="defaultInterface">The resulting default interface pointer.</param>
/// <see href="https://learn.microsoft.com/en-us/uwp/winrt-cref/winrt-type-system#composable-activation"/>
public unsafe delegate void WindowsRuntimeActivationFactoryCallback(
    ReadOnlySpan<object?> additionalParameters,
    WindowsRuntimeObject? baseInterface,
    out void* innerInterface,
    out void* defaultInterface);
