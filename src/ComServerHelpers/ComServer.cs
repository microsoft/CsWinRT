// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ComServerHelpers.Internal;
using ComServerHelpers.Internal.Windows;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using WinRT;
using static Windows.Win32.PInvoke;

namespace ComServerHelpers;

/// <summary>
/// An Out of Process COM Server.
/// </summary>
/// <remarks>
/// <para>Allows for types to be created using COM activation instead of WinRT activation like <see cref="WinRtServer"/>.</para>
/// <para>Typical usage is to call from a <see langword="using"/> block, using <see cref="WaitForFirstObjectAsync"/> to not close until it is safe to do so.</para>
/// <code language="cs">
/// <![CDATA[
/// using (ComServer server = new ComServer())
/// {
///     server.RegisterClass<RemoteThing, IRemoteThing>();
///     server.Start();
///     await server.WaitForFirstObjectAsync();
/// }
/// ]]>
/// </code>
/// </remarks>
/// <see cref="IDisposable"/>
/// <threadsafety static="true" instance="false"/>
[SupportedOSPlatform("windows6.0.6000")]
public sealed class ComServer : IDisposable
{
    /// <summary>
    /// Map of class factories and the registration cookie from the CLSID that the factory creates.
    /// </summary>
    private readonly Dictionary<Guid, (BaseClassFactory factory, uint cookie)> factories = [];

    /// <summary>
    /// Tracks the creation of the first instance after server is started.
    /// </summary>
    private TaskCompletionSource<object>? firstInstanceCreated;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComServer"/> class.
    /// </summary>
    public unsafe ComServer()
    {
        Utils.SetDefaultGlobalOptions();
    }

    /// <summary>
    /// Register a class factory with the server.
    /// </summary>
    /// <param name="factory">The class factory to register.</param>
    /// <param name="comWrappers">The implementation of <see cref="ComWrappers"/> to use for wrapping.</param>
    /// <returns><see langword="true"/> if <paramref name="factory"/> was registered; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Only one factory can be registered for a CLSID.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> or <paramref name="comWrappers"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    /// <seealso cref="UnregisterClassFactory(Guid)"/>
    public unsafe bool RegisterClassFactory(BaseClassFactory factory, ComWrappers comWrappers)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(comWrappers);
        if (IsRunning)
        {
            throw new InvalidOperationException("Can only add class factories when server is not running.");
        }

        Guid clsid = factory.Clsid;
        if (factories.ContainsKey(clsid))
        {
            return false;
        }

        factory.InstanceCreated += Factory_InstanceCreated;
        nint wrapper = 0;
        try
        {
            wrapper = Utils.StrategyBasedComWrappers.GetOrCreateComInterfaceForObject(new BaseClassFactoryWrapper(factory, comWrappers), CreateComInterfaceFlags.None);

            uint cookie;
            CoRegisterClassObject(&clsid, (IUnknown*)wrapper, CLSCTX.CLSCTX_LOCAL_SERVER, (REGCLS.REGCLS_MULTIPLEUSE | REGCLS.REGCLS_SUSPENDED), &cookie).ThrowOnFailure();

            factories.Add(clsid, (factory, cookie));
        }
        catch (Exception)
        {
            if (wrapper != 0)
            {
                Marshal.Release(wrapper);
            }
            throw;
        }

        return true;
    }

    /// <summary>
    /// Unregister a class factory.
    /// </summary>
    /// <param name="clsid">The CLSID of the server to remove.</param>
    /// <returns><see langword="true"/> if the server was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    /// <exception cref="InvalidOperationException">The server is running.</exception>
    /// <seealso cref="RegisterClassFactory(BaseClassFactory, ComWrappers)"/>
    public unsafe bool UnregisterClassFactory(Guid clsid)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (IsRunning)
        {
            throw new InvalidOperationException("Can only remove class factories when server is not running.");
        }

        if (!factories.TryGetValue(clsid, out (BaseClassFactory factory, uint cookie) data))
        {
            return false;
        }
        factories.Remove(clsid);

        data.factory.InstanceCreated -= Factory_InstanceCreated;

        CoRevokeClassObject(data.cookie).ThrowOnFailure();
        return true;
    }

    private void Factory_InstanceCreated(object? sender, InstanceCreatedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        InstanceCreated?.Invoke(this, e);
        firstInstanceCreated?.TrySetResult(e.Instance);
    }

    /// <summary>
    /// Gets a value indicating whether the server is running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Starts the server.
    /// </summary>
    /// <remarks>Calling <see cref="Start"/> is non-blocking.</remarks>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (IsRunning)
        {
            return;
        }

        firstInstanceCreated = new();
        CoResumeClassObjects().ThrowOnFailure();
        IsRunning = true;
    }

    /// <summary>
    /// Stops the server.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (!IsRunning)
        {
            return;
        }

        firstInstanceCreated = null;
        IsRunning = false;
        CoSuspendClassObjects().ThrowOnFailure();
    }

    /// <summary>
    /// Wait for the server to have created an object since it was started.
    /// </summary>
    /// <returns>The first object created if the server is running; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ObjectDisposedException">The instance is disposed.</exception>
    public async Task<object?> WaitForFirstObjectAsync()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        TaskCompletionSource<object>? local = firstInstanceCreated;
        if (local is null)
        {
            return null;
        }
        return await local.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a value indicating whether the instance is disposed.
    /// </summary>
    public bool IsDisposed
    {
        get;
        private set;
    }

    /// <summary>
    /// Force the server to stop and release all resources.
    /// </summary>
    public void Dispose()
    {
        if (!IsDisposed)
        {
            try
            {
                _ = CoSuspendClassObjects();

                IsRunning = false;

                foreach (var clsid in factories.Keys)
                {
                  _ = UnregisterClassFactory(clsid);
                }
            }
            finally
            {
                IsDisposed = true;
            }
        }
    }

    /// <summary>
    /// Occurs when the server creates an object.
    /// </summary>
    public event EventHandler<InstanceCreatedEventArgs>? InstanceCreated;
}
