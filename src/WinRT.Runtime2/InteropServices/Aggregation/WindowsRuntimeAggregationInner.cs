// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The non-delegating inner <c>IInspectable</c> object handed back to a controlling outer object by the
/// composition factory of an authored (unsealed) Windows Runtime class.
/// </summary>
/// <remarks>
/// <para>
/// This is the CsWinRT counterpart of the <c>INonDelegatingInspectable</c> facet that C++/WinRT exposes from
/// <c>winrt::implements</c>. It is a small, hand-written COM object (it deliberately does not go through
/// <see cref="System.Runtime.InteropServices.ComWrappers"/>, as its identity and lifetime must be distinct
/// from the ones of the aggregate) which:
/// <list type="bullet">
///   <item>Answers <c>IUnknown</c> and <c>IInspectable</c> with itself, without delegating.</item>
///   <item>Owns the CCW of the aggregated object, created through <see cref="WindowsRuntimeAggregationComWrappers"/>.</item>
///   <item>Owns the per-aggregate delegating vtables that CCW uses, and hands out the matching interface
///     pointer for every interface the composable runtime class declares.</item>
/// </list>
/// </para>
/// <para>
/// The per-aggregate vtables are private copies of the normal CCW vtables of those interfaces, with their
/// <c>IUnknown</c> and <c>IInspectable</c> entries replaced by the delegating ones from
/// <see cref="WindowsRuntimeAggregatedIInspectableImpl"/>. Each copy is preceded by a single slot holding the
/// controlling outer object, so those entries can resolve it with a pair of pointer loads. All other entries
/// are the original CCW stubs, receiving the very same <see cref="ComInterfaceDispatch"/> pointer they would
/// receive for a non aggregated object, so interface calls behave identically (and CCW-s for objects that are
/// not aggregated are not affected in any way).
/// </para>
/// <para>
/// The controlling outer is not reference counted here, matching the COM aggregation contract (the outer
/// always outlives the inner, as it is the only object holding a reference to it).
/// </para>
/// </remarks>
internal static unsafe class WindowsRuntimeAggregationInner
{
    /// <summary>
    /// The native memory backing the interface entries (and the per-aggregate vtable copies they point to)
    /// of the CCW of each aggregated object, keyed by that object.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Runtime.InteropServices.ComWrappers"/> keeps the <c>ComInterfaceEntry</c> array
    /// returned by <c>ComputeVtables</c> (and, through it, the vtables it points to) for the entire lifetime of
    /// the wrapper, which can extend past the last COM reference on it. Tying that memory to the lifetime of the
    /// aggregated managed object is therefore the only safe bound: once the object is gone, no code can reach
    /// the wrapper anymore, so the memory can be released from the finalizer of the entry below.
    /// </remarks>
    private static readonly ConditionalWeakTable<object, NativeInterfaceEntries> InterfaceEntries = [];

    /// <summary>
    /// The <see cref="IInspectableVftbl"/> value for the non-delegating inner object.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IInspectableVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static WindowsRuntimeAggregationInner()
    {
        Vftbl.QueryInterface = &QueryInterface;
        Vftbl.AddRef = &AddRef;
        Vftbl.Release = &Release;
        Vftbl.GetIids = &GetIids;
        Vftbl.GetRuntimeClassName = &GetRuntimeClassName;
        Vftbl.GetTrustLevel = &GetTrustLevel;
    }

    /// <summary>
    /// Creates a new non-delegating inner object with an initial reference count of <c>1</c>.
    /// </summary>
    /// <param name="instance">The aggregated managed object.</param>
    /// <param name="controllingOuter">The controlling outer <c>IInspectable</c> object (not reference counted).</param>
    /// <param name="aggregationEntries">The interfaces the composable runtime class of <paramref name="instance"/> can expose.</param>
    /// <returns>The resulting non-delegating inner <c>IInspectable</c> object.</returns>
    public static void* Create(object instance, void* controllingOuter, ReadOnlySpan<WindowsRuntimeAggregationEntry> aggregationEntries)
    {
        int count = aggregationEntries.Length;

        // Total size of the block holding all per-aggregate vtable copies. Each copy is preceded by one
        // pointer sized slot holding the controlling outer object, which is what the delegating entries
        // read to resolve it (the vtable structs are made of function pointers, so all offsets stay aligned).
        nuint vtableBlockSize = 0;

        for (int i = 0; i < count; i++)
        {
            vtableBlockSize += (nuint)sizeof(void*) + (nuint)aggregationEntries[i].VtableSize;
        }

        // The extra interface entry is the non-delegating 'IInspectable' the inner object itself forwards to,
        // so that 'GetIids', 'GetRuntimeClassName', and 'GetTrustLevel' on the inner report the composed base
        // rather than the aggregate (this is what 'NonDelegatingInspectable' does in C++/WinRT).
        NativeInterfaceEntries interfaceEntries = NativeInterfaceEntries.Allocate(instance, count + 1, vtableBlockSize);

        ComInterfaceEntry* entries = interfaceEntries.Entries;
        State* state = (State*)NativeMemory.AllocZeroed((nuint)sizeof(State));

        state->ReferenceCount = 1;
        state->Vtable = (void*)Vtable;
        state->ControllingOuter = controllingOuter;
        state->InterfaceEntries = entries;
        state->InterfacePointers = (void**)NativeMemory.AllocZeroed((nuint)count + 1, (nuint)sizeof(void*));
        state->InterfaceCount = count;
        state->InstanceHandle = GCHandle.ToIntPtr(GCHandle.Alloc(instance));

        try
        {
            byte* currentVtable = (byte*)interfaceEntries.VtableBlock;

            for (int i = 0; i < count; i++)
            {
                WindowsRuntimeAggregationEntry entry = aggregationEntries[i];

                // The controlling outer object goes right before the vtable copy
                *(void**)currentVtable = controllingOuter;

                void* vtableCopy = currentVtable + sizeof(void*);

                NativeMemory.Copy((void*)entry.Vtable, vtableCopy, (nuint)entry.VtableSize);

                // Replace all six 'IInspectable' (and therefore 'IUnknown') entries with the delegating ones.
                // Every remaining entry is left as is, so interface methods keep running the normal CCW stubs.
                *(IInspectableVftbl*)vtableCopy = *WindowsRuntimeAggregatedIInspectableImpl.Vtable;

                entries[i].IID = entry.IID;
                entries[i].Vtable = (nint)vtableCopy;

                currentVtable += sizeof(void*) + entry.VtableSize;
            }

            entries[count].IID = WellKnownWindowsInterfaceIIDs.IID_IInspectable;
            entries[count].Vtable = IInspectableImpl.Vtable;

            state->Unknown = WindowsRuntimeAggregationComWrappers.CreateComInterfaceForObject(instance, entries, count + 1);

            // Resolve (and cache) every interface pointer the inner object can hand out. Each 'QueryInterface'
            // call increments the reference count of the CCW, but the resulting pointers delegate their
            // 'Release' calls to the controlling outer, so that increment is undone right away: the single
            // reference this inner object owns is what keeps the CCW (and its vtables) alive.
            for (int i = 0; i <= count; i++)
            {
                IUnknownVftbl.QueryInterfaceUnsafe(state->Unknown, in entries[i].IID, out state->InterfacePointers[i]).Assert();

                _ = IUnknownVftbl.ReleaseUnsafe(state->Unknown);
            }

            // The last entry is the non-delegating 'IInspectable' the inner object forwards to
            state->Inspectable = state->InterfacePointers[count];

            return state;
        }
        catch
        {
            Destroy(state);

            throw;
        }
    }

    /// <summary>
    /// Gets a pointer to the non-delegating inner <c>IInspectable</c> implementation.
    /// </summary>
    private static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// Releases every native resource owned by a given inner object, and the object itself.
    /// </summary>
    /// <param name="state">The state of the inner object to destroy.</param>
    /// <remarks>
    /// The interface entries and the vtable copies are deliberately not freed here: they are owned by
    /// <see cref="InterfaceEntries"/>, and are released only once the aggregated object itself is collected
    /// (see the remarks on that field).
    /// </remarks>
    private static void Destroy(State* state)
    {
        // Stop treating the managed object as aggregated before dropping the last reference on its CCW:
        // from this point on there is no controlling outer left to delegate to.
        if (state->InstanceHandle != 0)
        {
            GCHandle instanceHandle = GCHandle.FromIntPtr(state->InstanceHandle);

            if (instanceHandle.Target is { } instance)
            {
                WindowsRuntimeAggregation.Unregister(instance);
            }

            instanceHandle.Free();
        }

        if (state->Unknown is not null)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(state->Unknown);
        }

        NativeMemory.Free(state->InterfacePointers);
        NativeMemory.Free(state);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-queryinterface(refiid_void)"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT QueryInterface(void* thisPtr, Guid* riid, void** ppvObject)
    {
        if (ppvObject is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *ppvObject = null;

        if (riid is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        State* state = (State*)thisPtr;

        // The inner object answers 'IUnknown' and 'IInspectable' with itself, so that the aggregate keeps a
        // stable, non-delegating identity for the controlling outer to hold on to. This is exactly what
        // 'root_implements::NonDelegatingQueryInterface' does in C++/WinRT.
        if (*riid == WellKnownWindowsInterfaceIIDs.IID_IUnknown ||
            *riid == WellKnownWindowsInterfaceIIDs.IID_IInspectable)
        {
            _ = Interlocked.Increment(ref state->ReferenceCount);

            *ppvObject = thisPtr;

            return WellKnownErrorCodes.S_OK;
        }

        // Every other interface the composable runtime class declares is answered with the matching
        // per-aggregate delegating interface pointer. Interfaces that are not in this set (e.g. the ones
        // every CCW carries, such as 'IStringable', 'IAgileObject', 'IMarshal', and 'IWeakReferenceSource')
        // are deliberately not exposed: their vtables are shared across the whole application, so handing
        // them out would give the aggregate a second COM identity, and a second reference count. Just like
        // in C++/WinRT, the controlling outer object is the one responsible for implementing them.
        for (int i = 0; i < state->InterfaceCount; i++)
        {
            if (*riid == state->InterfaceEntries[i].IID)
            {
                // The returned pointer delegates its 'IUnknown' methods to the controlling outer, so the
                // reference the caller is being handed has to be taken on the controlling outer as well.
                _ = IUnknownVftbl.AddRefUnsafe(state->ControllingOuter);

                *ppvObject = state->InterfacePointers[i];

                return WellKnownErrorCodes.S_OK;
            }
        }

        return WellKnownErrorCodes.E_NOINTERFACE;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-addref"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* thisPtr)
    {
        State* state = (State*)thisPtr;

        return (uint)Interlocked.Increment(ref state->ReferenceCount);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nf-unknwn-iunknown-release"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(void* thisPtr)
    {
        State* state = (State*)thisPtr;

        long referenceCount = Interlocked.Decrement(ref state->ReferenceCount);

        if (referenceCount == 0)
        {
            Destroy(state);
        }

        return (uint)referenceCount;
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getiids"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetIids(void* thisPtr, uint* iidCount, Guid** iids)
    {
        return IInspectableVftbl.GetIidsUnsafe(((State*)thisPtr)->Inspectable, iidCount, iids);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-getruntimeclassname"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetRuntimeClassName(void* thisPtr, HSTRING* className)
    {
        return IInspectableVftbl.GetRuntimeClassNameUnsafe(((State*)thisPtr)->Inspectable, className);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nf-inspectable-iinspectable-gettrustlevel"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetTrustLevel(void* thisPtr, TrustLevel* trustLevel)
    {
        return IInspectableVftbl.GetTrustLevelUnsafe(((State*)thisPtr)->Inspectable, trustLevel);
    }

    /// <summary>
    /// The native state of a non-delegating inner object.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct State
    {
        /// <summary>The <c>IInspectable</c> vtable (this field makes the state a valid COM object).</summary>
        public void* Vtable;

        /// <summary>The reference count of the inner object itself.</summary>
        public long ReferenceCount;

        /// <summary>The controlling outer object (not reference counted).</summary>
        public void* ControllingOuter;

        /// <summary>The <c>IUnknown</c> pointer for the CCW of the aggregated object (owned).</summary>
        public void* Unknown;

        /// <summary>The non-delegating <c>IInspectable</c> pointer for that CCW (borrowed from <see cref="InterfacePointers"/>).</summary>
        public void* Inspectable;

        /// <summary>The <see cref="GCHandle"/> keeping the aggregated managed object alive.</summary>
        public nint InstanceHandle;

        /// <summary>The interface entries of the CCW (owned by <see cref="InterfaceEntries"/>).</summary>
        public ComInterfaceEntry* InterfaceEntries;

        /// <summary>The cached interface pointers, one per entry in <see cref="InterfaceEntries"/> (owned).</summary>
        public void** InterfacePointers;

        /// <summary>The number of delegating interfaces (i.e. all entries except the trailing <c>IInspectable</c> one).</summary>
        public int InterfaceCount;
    }

    /// <summary>
    /// The native memory holding the interface entries of the CCW of an aggregated object, along with the
    /// per-aggregate vtable copies those entries point to.
    /// </summary>
    /// <remarks>
    /// This is attached to the aggregated managed object through <see cref="InterfaceEntries"/>, so the memory
    /// is only released once that object is collected, which is strictly after any code could still reach the
    /// CCW referring to it.
    /// </remarks>
    private sealed class NativeInterfaceEntries
    {
        /// <summary>
        /// Allocates the native memory for a given aggregated object.
        /// </summary>
        /// <param name="instance">The aggregated managed object.</param>
        /// <param name="interfaceEntryCount">The number of interface entries to allocate.</param>
        /// <param name="vtableBlockSize">The size in bytes of the block holding all per-aggregate vtable copies.</param>
        /// <returns>The resulting <see cref="NativeInterfaceEntries"/> instance, owned by <paramref name="instance"/>.</returns>
        public static NativeInterfaceEntries Allocate(object instance, int interfaceEntryCount, nuint vtableBlockSize)
        {
            NativeInterfaceEntries entries = new()
            {
                Entries = (ComInterfaceEntry*)NativeMemory.AllocZeroed((nuint)interfaceEntryCount, (nuint)sizeof(ComInterfaceEntry)),
                VtableBlock = vtableBlockSize == 0 ? null : NativeMemory.Alloc(vtableBlockSize)
            };

            // Each composition creates a brand new instance, so there can never be an existing entry here
            InterfaceEntries.Add(instance, entries);

            return entries;
        }

        /// <summary>
        /// Releases the native memory.
        /// </summary>
        ~NativeInterfaceEntries()
        {
            NativeMemory.Free(Entries);
            NativeMemory.Free(VtableBlock);
        }

        /// <summary>The interface entries.</summary>
        public ComInterfaceEntry* Entries { get; private init; }

        /// <summary>The block holding all per-aggregate vtable copies.</summary>
        public void* VtableBlock { get; private init; }
    }
}
