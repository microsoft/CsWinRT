// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ComServerHelpers;

/// <summary>
/// Extensions for <see cref="WinRtServer"/>.
/// </summary>
[SupportedOSPlatform("windows8.0")]
public static class WinRtServerExtensions
{
    /// <summary>
    /// Register a type with the server.
    /// </summary>
    /// <typeparam name="T">The type register.</typeparam>
    /// <param name="server">The instance.</param>
    /// <param name="comWrappers">The implementation of <see cref="ComWrappers"/> to use for wrapping.</param>
    /// <returns><see langword="true"/> if type was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Type can only be registered once.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> or <paramref name="comWrappers"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterClass<T>(this WinRtServer server, ComWrappers comWrappers) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(comWrappers);

        return server.RegisterActivationFactory(new GeneralActivationFactory<T>(), comWrappers);
    }

    /// <summary>
    /// Register a type with the server.
    /// </summary>
    /// <typeparam name="T">The type register.</typeparam>
    /// <param name="server">The instance.</param>
    /// <param name="factory">Method to create instance of <typeparamref name="T"/>.</param>
    /// <param name="comWrappers">The implementation of <see cref="ComWrappers"/> to use for wrapping.</param>
    /// <returns><see langword="true"/> if type was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Type can only be registered once.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="server"/>, <paramref name="factory"/>, or <paramref name="comWrappers"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterClass<T>(this WinRtServer server, Func<T> factory, ComWrappers comWrappers) where T : class
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(comWrappers);

        return server.RegisterActivationFactory(new DelegateActivationFactory<T>(factory), comWrappers);
    }

    /// <summary>
    /// Register an activation factory with the server.
    /// </summary>
    /// <typeparam name="T">The type of the factory to register.</typeparam>
    /// <param name="server">The instance.</param>
    /// <param name="comWrappers">The implementation of <see cref="ComWrappers"/> to use for wrapping.</param>
    /// <returns><see langword="true"/> if an instance of <typeparamref name="T"/> was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Only one factory can be registered for a Activatable Class ID.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> or <paramref name="comWrappers"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterActivationFactory<T>(this WinRtServer server, ComWrappers comWrappers) where T : BaseActivationFactory, new()
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(comWrappers);

        return server.RegisterActivationFactory(new T(), comWrappers);
    }
}
