// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type representing all associated state for a given <see cref="EventSource{TDelegate}"/> instance.
/// </summary>
/// <typeparam name="T">The type of delegate being managed from the associated event.</typeparam>
[WindowsRuntimeManagedOnlyType]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract unsafe class EventSourceState<T> : IDisposable
    where T : MulticastDelegate
{
    // Registration state and delegate are cached separately to survive the 'EventSource<T>' garbage collection,
    // and to prevent the generated event delegate from impacting the lifetime of the event source.

    /// <summary>
    /// The pointer to the target native object owning the associated event.
    /// </summary>
    private void* _thisPtr;

    /// <summary>
    /// The index of the event the state is associated to.
    /// </summary>
    private readonly int _index;

    /// <summary>
    /// A weak reference to this <see cref="EventSourceState{T}"/> instance.
    /// </summary>
    private readonly WeakReference<object> _weakReferenceToSelf;

    /// <summary>
    /// The pointer to the native CCW for the event invoke delegate (ie. <see cref="EventInvoke"/>).
    /// </summary>
    private void* _eventInvokePtr;

    /// <summary>
    /// The pointer to the reference tracker target interface from <see cref="_eventInvokePtr"/>, if available.
    /// </summary>
    private void* _referenceTrackerTargetPtr;

    /// <summary>
    /// Creates a new <see cref="EventSourceState{TDelegate}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr">The pointer to the target object owning the associated event.</param>
    /// <param name="index">The index of the event the state is associated to.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="thisPtr"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is less than zero.</exception>
    protected EventSourceState(void* thisPtr, int index)
    {
        ArgumentNullException.ThrowIfNull(thisPtr);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _thisPtr = thisPtr;
        _index = index;
        _weakReferenceToSelf = new WeakReference<object>(this);

        EventInvoke = GetEventInvoke();
    }

    /// <summary>
    /// Finalizes the current <see cref="EventSourceState{TDelegate}"/> instance.
    /// </summary>
    ~EventSourceState()
    {
        // The lifetime of this object is managed by the delegate / eventInvoke
        // through its target reference to it. Once the delegate no longer has
        // any references, this object will also no longer have any references.
        OnDispose();
    }

    /// <summary>
    /// Gets the current <typeparamref name="T"/> value with the active subscriptions for the target event.
    /// </summary>
    protected internal T? TargetDelegate { get; private set; }

    /// <summary>
    /// Gets the current <typeparamref name="T"/> delegate instance with the invoke stub for the target delegate.
    /// </summary>
    internal T EventInvoke { get; }

    /// <summary>
    /// Gets or sets the <see cref="EventRegistrationToken"/> for the current event source.
    /// </summary>
    internal EventRegistrationToken Token { get; set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        OnDispose();
    }

    /// <summary>
    /// Gets a <typeparamref name="T"/> instance responsible for actually raising the
    /// event, if any targets are currently available, or for doing nothing if the current
    /// target handler is currently <see langword="null"/>.
    /// </summary>
    /// <returns>The resulting <typeparamref name="T"/> instance to raise the event.</returns>
    /// <remarks>
    /// The returned delegate must capture <see langword="this"/> to ensure the associated state is kept alive.
    /// </remarks>
    protected abstract T GetEventInvoke();

    /// <summary>
    /// Gets the weak reference to the current <see cref="EventSourceState{T}"/> instance, to use
    /// with <see cref="EventSourceCache"/> the cache to allow for proper removal with comparision.
    /// </summary>
    /// <returns>The <see cref="WeakReference{T}"/> instance to this <see cref="EventSourceState{T}"/> object.</returns>
    internal WeakReference<object> GetWeakReferenceToSelf()
    {
        return _weakReferenceToSelf;
    }

    /// <summary>
    /// Adds a new handler to the target delegate.
    /// </summary>
    /// <param name="handler">The new delegate to add.</param>
    internal void AddHandler(T handler)
    {
        TargetDelegate = Unsafe.As<T>(Delegate.Combine(TargetDelegate, handler));
    }

    /// <summary>
    /// Removes a handler to the target delegate.
    /// </summary>
    /// <param name="handler">The delegate to remove.</param>
    internal void RemoveHandler(T handler)
    {
        TargetDelegate = Unsafe.As<T>(Delegate.Remove(TargetDelegate, handler));
    }

    /// <summary>
    /// Initializes the reference tracking for the current event source, if the target object implements it.
    /// </summary>
    /// <param name="eventInvokePtr">The pointer to the native CCW for the event invoke delegate (ie. <see cref="EventInvoke"/>).</param>
    internal void InitalizeReferenceTracking(void* eventInvokePtr)
    {
        _eventInvokePtr = eventInvokePtr;

        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(eventInvokePtr, in WellKnownInterfaceIds.IID_IReferenceTrackerTarget, out _referenceTrackerTargetPtr);

        if (WellKnownErrorCodes.Succeeded(hresult))
        {
            // We don't want to keep ourselves alive, and as long as this object
            // is alive, the CCW still exists (the CCW is keeping it alive).
            _ = IUnknownVftbl.ReleaseUnsafe(_referenceTrackerTargetPtr);
        }
    }

    /// <summary>
    /// Checks whether there are any COM references to the current event source.
    /// </summary>
    /// <returns>Whether there are any COM references to the current instance.</returns>
    internal bool HasComReferences()
    {
        // If we have an event invoke pointer, check that first
        if (_eventInvokePtr != null)
        {
            _ = IUnknownVftbl.AddRefUnsafe(_eventInvokePtr);

            uint comRefCount = IUnknownVftbl.ReleaseUnsafe(_eventInvokePtr);

            if (comRefCount != 0)
            {
                return true;
            }
        }

        // If we have a reference tracker pointer also check it. It's possible for the event invoke
        // to have a reference count of 0, but it's actually kept alive by the reference tracker.
        if (_referenceTrackerTargetPtr != null)
        {
            _ = IReferenceTrackerVftblTargetVftbl.AddRefFromReferenceTrackerUnsafe(_referenceTrackerTargetPtr);

            uint refTrackerCount = IReferenceTrackerVftblTargetVftbl.ReleaseFromReferenceTrackerUnsafe(_referenceTrackerTargetPtr);

            if (refTrackerCount != 0)
            {
                // Note we can't tell if the reference tracker ref is pegged or not, so this is best effort
                // where if there are any reference tracker references, we assume the event has references.
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Disposes the current instance, by removing the cache entry if needed.
    /// </summary>
    private void OnDispose()
    {
        void* thisPtr = _thisPtr;

        _thisPtr = null;

        // We only need to try to remove the cache entry the first time this state is disposed.
        // This will remove the state, and also remove the cache itself if it's now empty. This
        // ensures that no cache object is kept alive unnecessarily when no longer needed.
        if (thisPtr != null)
        {
            EventSourceCache.Remove(_thisPtr, _index, _weakReferenceToSelf);
        }
    }
}
