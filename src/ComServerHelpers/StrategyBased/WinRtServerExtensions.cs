using System;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

namespace ComServerHelpers.StrategyBased;

/// <summary>
/// Extensions for <see cref="WinRtServer"/> when using <see cref="StrategyBasedComWrappers"/>.
/// </summary>
[SupportedOSPlatform("windows8.0")]
public static class WinRtServerExtensions
{
    private static readonly StrategyBasedComWrappers comWrappers = new();

    /// <summary>
    /// Register a type with the server.
    /// </summary>
    /// <typeparam name="T">The type register.</typeparam>
    /// <param name="server">The instance.</param>
    /// <returns><see langword="true"/> if type was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Type can only be registered once.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterClass<T>(this WinRtServer server) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(server);

        return server.RegisterActivationFactory(new GeneralActivationFactory<T>(), comWrappers);
    }

    /// <summary>
    /// Register a type with the server.
    /// </summary>
    /// <typeparam name="T">The type register.</typeparam>
    /// <param name="server">The instance.</param>
    /// <param name="factory">Method to create instance of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> if type was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Type can only be registered once.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterClass<T>(this WinRtServer server, Func<T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(factory);

        return server.RegisterActivationFactory(new DelegateActivationFactory<T>(factory), comWrappers);
    }

    /// <summary>
    /// Register an activation factory with the server.
    /// </summary>
    /// <typeparam name="T">The type of the factory to register.</typeparam>
    /// <param name="server">The instance.</param>
    /// <returns><see langword="true"/> if an instance of <typeparamref name="T"/> was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Only one factory can be registered for a Activatable Class ID.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="server"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    public static bool RegisterActivationFactory<T>(this WinRtServer server) where T : BaseActivationFactory, new()
    {
        ArgumentNullException.ThrowIfNull(server);

        return server.RegisterActivationFactory(new T(), comWrappers);
    }
}
