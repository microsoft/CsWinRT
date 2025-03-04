// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A <see cref="WindowsRuntimeObjectReference"/> implementation tied to a specific context.
/// </summary>
internal sealed unsafe class ContextAwareObjectReference : WindowsRuntimeObjectReference
{
    /// <summary>
    /// The context callback instance used to marshal the target COM object across different contexts.
    /// </summary>
    /// <seealso cref="IContextCallbackVftbl"/>
    private readonly void* _contextCallbackPtr;

    /// <summary>
    /// The object context instance for the current context.
    /// </summary>
    private readonly nuint _contextToken;

    /// <summary>
    /// The interface id for the wrapped native object (also used to resolve agile references).
    /// </summary>
    private readonly Guid _iid;

    /// <summary>
    /// The lazy-initialized cache of context-specific object references.
    /// </summary>
    private volatile ConcurrentDictionary<nuint, WindowsRuntimeObjectReference?>? _cachedContexts;

    /// <summary>
    /// The lazy-initialized agile reference for the current object.
    /// </summary>
    /// <remarks>
    /// Note: this object can be one of the following:
    /// <list type="bullet">
    ///   <item>If we haven't initialized this field yet, <see langword="null"/>.</item>
    ///   <item>If we couldn't retrieved an agile reference, a dummy placeholder object to detect initialization.</item>
    ///   <item>Otherwise, a <see cref="FreeThreadedObjectReference"/> instance.</item>
    /// </list>
    /// </remarks>
    private volatile object? _agileReference;

    /// <summary>
    /// Creates a new <see cref="ContextAwareObjectReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='thisPtr']/node()"/></param>
    /// <param name="referenceTrackerPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='referenceTrackerPtr']/node()"/></param>
    /// <param name="flags"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='flags']/node()"/></param>
    /// <param name="iid"><inheritdoc cref="_iid" path="/summary/node()"/></param>
    public ContextAwareObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        in Guid iid,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
        : base(thisPtr, referenceTrackerPtr, flags)
    {
        _contextCallbackPtr = WindowsRuntimeImports.CoGetObjectContext(in WellKnownInterfaceIds.IID_IContextCallback);
        _contextToken = WindowsRuntimeImports.CoGetContextToken();
        _iid = iid;
    }

    /// <summary>
    /// Creates a new <see cref="ContextAwareObjectReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='thisPtr']/node()"/></param>
    /// <param name="referenceTrackerPtr"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='referenceTrackerPtr']/node()"/></param>
    /// <param name="flags"><inheritdoc cref="WindowsRuntimeObjectReference(void*, void*, CreateObjectReferenceFlags)" path="/param[@name='flags']/node()"/></param>
    /// <param name="contextCallbackPtr"><inheritdoc cref="_contextCallbackPtr" path="/summary/node()"/></param>
    /// <param name="contextToken"><inheritdoc cref="_contextToken" path="/summary/node()"/></param>
    /// <param name="iid"><inheritdoc cref="_iid" path="/summary/node()"/></param>
    public ContextAwareObjectReference(
        void* thisPtr,
        void* referenceTrackerPtr,
        void* contextCallbackPtr,
        nuint contextToken,
        in Guid iid,
        CreateObjectReferenceFlags flags = CreateObjectReferenceFlags.None)
        : base(thisPtr, referenceTrackerPtr, flags)
    {
        _contextCallbackPtr = contextCallbackPtr;
        _contextToken = contextToken;
        _iid = iid;
    }

    /// <summary>
    /// Gets the cache of context-specific object references.
    /// </summary>
    private ConcurrentDictionary<nuint, WindowsRuntimeObjectReference?> CachedContexts
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            ConcurrentDictionary<nuint, WindowsRuntimeObjectReference?> InitializeCachedContexts()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref _cachedContexts,
                    value: new ConcurrentDictionary<nuint, WindowsRuntimeObjectReference?>(concurrencyLevel: 1, capacity: 16),
                    comparand: null);

                return _cachedContexts;
            }

            return _cachedContexts ?? InitializeCachedContexts();
        }
    }

    /// <summary>
    /// Gets the agile reference for the current object, if available.
    /// </summary>
    private WindowsRuntimeObjectReference? AgileReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            object? InitializeAgileReference()
            {
                // Helper stub to set '_agileReference' to a new agile reference instance
                static void InitializeAgileReference(object state)
                {
                    ContextAwareObjectReference @this = Unsafe.As<ContextAwareObjectReference>(state);

                    _ = Interlocked.CompareExchange(
                        location1: ref @this._agileReference,
                        value: @this.AsAgileUnsafe(),
                        comparand: null);
                }

                // Dispatch the call, and ignore any errors here (we allow the agile reference to remain 'null')
                _ = ContextCallback.CallInContextUnsafe(
                    contextCallbackPtr: _contextCallbackPtr,
                    contextToken: _contextToken,
                    callback: &InitializeAgileReference,
                    state: this);

                // We want to mark the field as initialized, in case the callback hasn't been invoked, to avoid
                // trying again every time. To do this, we just set the field to a placeholder if it's still 'null'.
                _ = Interlocked.CompareExchange(
                    location1: ref _agileReference,
                    value: PlaceholderNullAgileReference.Instance,
                    comparand: null);

                // At this point we can return whatever the updated value is
                return _agileReference;
            }

            object? agileReference = _agileReference;

            // Initialize the field, if this is the first invocation
            agileReference ??= InitializeAgileReference();

            // Check if we got the placeholder value, and return 'null' if so.
            // Otherwise, we can rely on the instance being an object reference.
            return agileReference == PlaceholderNullAgileReference.Instance
                ? null
                : Unsafe.As<WindowsRuntimeObjectReference>(agileReference);
        }
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeObjectReference"/> instance for the current context.
    /// </summary>
    /// <returns>Tthe <see cref="WindowsRuntimeObjectReference"/> instance for the current context.</returns>
    /// <remarks>
    /// The resulting object reference will be <see langword="null"/> if the current context is the same as the original context.
    /// </remarks>
    private WindowsRuntimeObjectReference? GetObjectReferenceForCurrentContext()
    {
        nuint currentContext = WindowsRuntimeImports.CoGetContextToken();

        return _contextCallbackPtr == null || currentContext == _contextToken
            ? null
            : CachedContexts.GetOrAdd(currentContext, CachedContextsObjectReferenceFactory.Value, this);
    }

    /// <inheritdoc/>
    private protected override bool DerivedIsInCurrentContext()
    {
        return _contextToken == 0 || _contextToken == WindowsRuntimeImports.CoGetContextToken();
    }

    /// <inheritdoc/>
    private protected override unsafe void* GetThisPtrWithContextUnsafe()
    {
        WindowsRuntimeObjectReference? cachedReference = GetObjectReferenceForCurrentContext();

        // If we don't have a cached reference, we can just return the current pointer.
        // Otherwise, we can resolve the pointer from the cached agile reference we got.
        return cachedReference is null
            ? GetThisPtrWithoutContextUnsafe()
            : cachedReference.GetThisPtrUnsafe();
    }

    /// <inheritdoc/>
    private protected override void NativeReleaseWithContextUnsafe()
    {
        // Stub to do the native release without context (as this is invoked on the original context).
        // This avoids the overhead of going through 'GetThisPtrWithContextUnsafe()' unnecessarily.
        static void NativeReleaseWithoutContextUnsafe(object state)
        {
            ContextAwareObjectReference @this = Unsafe.As<ContextAwareObjectReference>(state);

            @this.NativeReleaseWithoutContextUnsafe();
        }

        // Marshal the native release call to the original context
        HRESULT hresult = ContextCallback.CallInContextUnsafe(
            contextCallbackPtr: _contextCallbackPtr,
            contextToken: _contextToken,
            callback: &NativeReleaseWithoutContextUnsafe,
            state: this);

        // If the operation fails, just release without context as a best effort
        if (hresult < 0)
        {
            base.NativeReleaseWithoutContextUnsafe();
        }
    }

    /// <summary>
    /// A factory for creating context-specific object references.
    /// </summary>
    private static class CachedContextsObjectReferenceFactory
    {
        /// <summary>
        /// Gets the cached factory for cached object references.
        /// </summary>
        /// <remarks>
        /// We have a single lambda expression in this type, so we can manually rewrite it to a <c>static readonly</c>
        /// field. This avoids the extra logic to lazily initialized it (it's already lazily initialized because it's
        /// in a <c>beforefieldinit</c> type which is only used when the lambda is actually needed), and also it allows
        /// storing the entire delegate in the Frozen Object Heap (FOH).
        /// </remarks>
        public static readonly Func<nuint, ContextAwareObjectReference, WindowsRuntimeObjectReference?> Value = CreateForCurrentContext;

        /// <summary>
        /// A stub to create a new object reference for a given context.
        /// </summary>
        private static WindowsRuntimeObjectReference? CreateForCurrentContext(nuint _, ContextAwareObjectReference state)
        {
            WindowsRuntimeObjectReference? agileReference = state.AgileReference;

            // We may fail to switch context and thereby not get an agile reference.
            // In these cases, fallback to using the current context.
            if (agileReference is null)
            {
                return null;
            }

            // Try to resolve an object reference for the current context, from the retrieved agile reference
            try
            {
                return agileReference.FromAgileUnsafe(in state._iid);
            }
            catch
            {
                // Fallback to using the current context in case of error
                return null;
            }
        }
    }
}

/// <summary>
/// A placeholder object for <see cref="ContextAwareObjectReference.AgileReference"/>.
/// </summary>
file static unsafe class PlaceholderNullAgileReference
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static object Instance = new();
}
