using System;
using System.Runtime.Versioning;
using WinRT;

namespace ComServerHelpers.CsWinRT;

/// <summary>
/// Extensions for <see cref="ComServer"/> when using <see cref="DefaultComWrappers"/>.
/// </summary>
[SupportedOSPlatform("windows6.0.6000")]
public static class ComServerExtensions
{
    private static readonly DefaultComWrappers comWrappers = new();

    /// <summary>
    /// Register a type with the server.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    /// <typeparam name="TInterface">The interface that <typeparamref name="T"/> implements.</typeparam>
    /// <param name="server">The instance.</param>
    /// <returns><see langword="true"/> if type was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Type can only be registered once.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterClass<T, TInterface>(this ComServer server) where T : class, TInterface, new()
    {
        ArgumentNullException.ThrowIfNull(server);

        return server.RegisterClassFactory(new GeneralClassFactory<T, TInterface>(), comWrappers);
    }

    /// <summary>
    /// Register a type with the server.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    /// <typeparam name="TInterface">The interface that <typeparamref name="T"/> implements.</typeparam>
    /// <param name="server">The instance.</param>
    /// <param name="factory">Method to create instance of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if type was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Type can only be registered once.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterClass<T, TInterface>(this ComServer server, Func<T> factory) where T : class, TInterface
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(factory);

        return server.RegisterClassFactory(new DelegateClassFactory<T, TInterface>(factory), comWrappers);
    }

    /// <summary>
    /// Register a class factory with the server.
    /// </summary>
    /// <typeparam name="T">The type of the factory to register.</typeparam>
    /// <param name="server">The instance.</param>
    /// <returns><see langword="true"/> if an instance of <typeparamref name="T"/> was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Only one factory can be registered for a CLSID.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterClassFactory<T>(this ComServer server) where T : BaseClassFactory, new()
    {
        ArgumentNullException.ThrowIfNull(server);

        return server.RegisterClassFactory(new T(), comWrappers);
    }

    /// <summary>
    /// Unregister a class factory.
    /// </summary>
    /// <param name="server">The instance.</param>
    /// <param name="factory">The class factory to unregister.</param>
    /// <returns><see langword="true"/> if the server was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    public static bool UnregisterClassFactory(this ComServer server, BaseClassFactory factory)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(factory);

        return server.UnregisterClassFactory(factory.Clsid);
    }
}