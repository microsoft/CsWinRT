// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A managed, low-level wrapper for a native Windows Runtime object.
/// </summary>
[WindowsRuntimeManagedOnlyType]
[WindowsRuntimeImplementationOnlyMember]
public abstract unsafe partial class WindowsRuntimeObjectReference : IDisposable
{
    /// <summary>
    /// The amount of GC pressure to add for every <see cref="WindowsRuntimeObjectReference"/> instance.
    /// </summary>
    private const long GCPressureBaseInBytes = 1000;

    /// <summary>
    /// The COM pointer wrapped by the current instance.
    /// </summary>
    /// <seealso cref="IInspectableVftbl"/>
    private readonly void* _thisPtr;

    /// <summary>
    /// The reference tracker pointer to manage the lifecycle of the wrapped COM object, if available.
    /// </summary>
    /// <seealso cref="IReferenceTrackerVftbl"/>
    private readonly void* _referenceTrackerPtr;

    /// <summary>
    /// A mask that indicates the state of the current object.
    /// The mask uses the following schema:
    /// <list type="bullet">
    ///     <item>[0, 30]: the number of existing reference tracking leases.</item>
    ///     <item>[31]: whether or not <see cref="IDisposable.Dispose"/> has been called.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Because this counts the additional "managed" references that are alive, the starting
    /// value is 0. That is, there is no additional reference count to increment upon object
    /// creation. This count just indicates how many active callers exist across all stacks.
    /// </remarks>
    private volatile int _referenceTrackingMask;

    /// <summary>
    /// The creation flags to control the lifecycle of the current <see cref="WindowsRuntimeObjectReference"/> instance.
    /// </summary>
    private readonly CreateObjectReferenceFlags _flags;

#if DEBUG
    /// <summary>
    /// The number of active lease-free calls using this object reference.
    /// </summary>
    private int _leaseFreeCallCount;
#endif

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeObjectReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr"><inheritdoc cref="_thisPtr" path="/summary/node()"/></param>
    /// <param name="referenceTrackerPtr"><inheritdoc cref="_referenceTrackerPtr" path="/summary/node()"/></param>
    /// <param name="flags"><inheritdoc cref="_flags" path="/summary/node()"/></param>
    private protected WindowsRuntimeObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
    {
        ArgumentNullException.ThrowIfNull(thisPtr);

        _thisPtr = thisPtr;
        _referenceTrackerPtr = referenceTrackerPtr;
        _flags = flags;

        // We are holding onto a native object or one of its interfaces. This causes native memory to be held
        // onto by this object as well, which the .NET GC is not aware of. To account for this, we manually
        // add memory pressure, to give the GC a hint about this additional memory it can't see. In theory,
        // all the interface QIs can be holding onto the same native object. Here we are taking the simplified
        // approach of having each 'WindowsRuntimeObjectReference' represent some basic native memory pressure,
        // rather than tracking all the 'WindowsRuntimeObjectReference' instances that are connected to the same
        // native object, and only releasing the memory pressure once all of them have been finalized.
        GC.AddMemoryPressure(GCPressureBaseInBytes);
    }

    /// <summary>
    /// Gets whether the underlying COM object is free-threaded (ie. it has no associated context).
    /// </summary>
    public bool IsFreeThreaded
    {
        // This optimization is somewhat similar to what we could do if we made 'IsFreeThreaded' an abstract
        // property and then implemented it in both derived types. However, that would rely on guarded
        // devirtualization to produce the same code, and it might not always kick in in exactly the same
        // way. Because all derived types are defined in this assembly, we can just hardcode this instead.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetType() == typeof(FreeThreadedObjectReference);
    }

    /// <summary>
    /// Gets whether projected calls require a managed lease for this object reference.
    /// </summary>
    /// <remarks>
    /// This predicate must also be used when deciding whether <c>ReleaseObjects</c> can
    /// deterministically dispose an object reference while calls may still be in flight.
    /// </remarks>
    internal bool RequiresManagedCallLease
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !IsFreeThreaded || _referenceTrackerPtr is not null;
    }

    /// <summary>
    /// Gets a value indicating whether the underlying COM object lives in the current thread context.
    /// </summary>
    /// <remarks>
    /// This value will always be <see langword="true"/> if <see cref="IsFreeThreaded"/> is also <see langword="true"/>.
    /// </remarks>
    public bool IsInCurrentContext
    {
        // Use an internal method to check for this, so we can easily change the implementation in the
        // future, if we need to, without having to worry about binary and source compatibility here.
        // This also allows us to do the same optimization we do for 'IsFreeThreaded'
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GetType() == typeof(FreeThreadedObjectReference) || DerivedIsInCurrentContext();
    }

    /// <summary>
    /// Gets whether <see cref="CreateObjectReferenceFlags.IsAggregated"/> is set for the current object reference.
    /// </summary>
    internal bool IsAggregated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _flags.HasFlag(CreateObjectReferenceFlags.IsAggregated);
    }

    /// <summary>
    /// Gets whether <see cref="CreateObjectReferenceFlags.PreventReleaseOnDispose"/> is set for the current object reference.
    /// </summary>
    internal bool PreventReleaseOnDispose
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _flags.HasFlag(CreateObjectReferenceFlags.PreventReleaseOnDispose);
    }

    /// <summary>
    /// Gets whether <see cref="CreateObjectReferenceFlags.PreventReleaseFromTrackerSourceOnDispose"/> is set for the current object reference.
    /// </summary>
    internal bool PreventReleaseFromTrackerSourceOnDispose
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _flags.HasFlag(CreateObjectReferenceFlags.PreventReleaseFromTrackerSourceOnDispose);
    }

    /// <summary>
    /// Copies a selection of flags from the current instance.
    /// </summary>
    /// <param name="requestedFlags">The flags to copy.</param>
    /// <returns>The requested flags.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CreateObjectReferenceFlags CopyFlags(CreateObjectReferenceFlags requestedFlags)
    {
        return _flags & requestedFlags;
    }

    /// <summary>
    /// Tracks a lease-free call in debug builds.
    /// </summary>
    [Conditional("DEBUG")]
    internal void DebugAcquireLeaseFreeCall()
    {
#if DEBUG
        _ = System.Threading.Interlocked.Increment(ref _leaseFreeCallCount);
#endif
    }

    /// <summary>
    /// Stops tracking a lease-free call in debug builds.
    /// </summary>
    [Conditional("DEBUG")]
    internal void DebugReleaseLeaseFreeCall()
    {
#if DEBUG
        int count = System.Threading.Interlocked.Decrement(ref _leaseFreeCallCount);

        Debug.Assert(count >= 0, "A lease-free call value was disposed more than once.");
#endif
    }

    /// <summary>
    /// Verifies that no lease-free calls are in flight in debug builds.
    /// </summary>
    [Conditional("DEBUG")]
    private void DebugAssertNoLeaseFreeCalls()
    {
#if DEBUG
        Debug.Assert(
            System.Threading.Volatile.Read(ref _leaseFreeCallCount) == 0,
            "The object reference was disposed while an AsValueForCall value was active, or that value was not disposed.");
#endif
    }

    /// <summary>
    /// Gets the value to return from <see cref="IsInCurrentContext"/> for the current instance.
    /// </summary>
    /// <returns>The value to return from <see cref="IsInCurrentContext"/>.</returns>
    private protected abstract bool DerivedIsInCurrentContext();
}
