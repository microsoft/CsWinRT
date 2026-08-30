// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Tracks the controlling outer object for authored Windows Runtime objects taking part in COM aggregation.
/// </summary>
/// <remarks>
/// <para>
/// A managed object is registered here when a composition factory for an authored (unsealed) Windows Runtime
/// class is invoked with a non-<see langword="null"/> <c>baseInterface</c> parameter. From that point on, the
/// identity of that managed object (as seen by native code) is the controlling outer object, as mandated by
/// the COM aggregation contract.
/// </para>
/// <para>
/// This lookup is used when an aggregated object is marshalled out to native code and by
/// <see cref="WindowsRuntimeComposition"/> when managed code explicitly resolves the controlling outer.
/// The <c>IUnknown</c> methods on the interfaces such an object exposes never come through here: they resolve
/// their controlling outer with a pair of pointer loads instead.
/// </para>
/// <para>
/// The controlling outer is deliberately not reference counted: an aggregated object must never keep its
/// controlling outer alive, or the two would keep each other alive forever. The outer is guaranteed to
/// outlive the inner, as it holds the only reference to the non-delegating inner object.
/// </para>
/// </remarks>
internal static unsafe class WindowsRuntimeAggregation
{
    /// <summary>
    /// The map of aggregated managed objects to their controlling outer object.
    /// </summary>
    private static readonly ConditionalWeakTable<object, ControllingOuterReference> AggregatedInstances = [];

    /// <summary>
    /// The number of managed objects currently taking part in COM aggregation.
    /// </summary>
    /// <remarks>
    /// This is only used as a fast path, so that applications that never aggregate an authored Windows Runtime
    /// object (which is the vast majority of them) never have to pay for a lookup in <see cref="AggregatedInstances"/>
    /// when marshalling a managed object out to native code.
    /// </remarks>
    private static int AggregatedInstanceCount;

    /// <summary>
    /// Gets whether there is at least one managed object currently taking part in COM aggregation.
    /// </summary>
    public static bool HasAggregatedInstances
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref AggregatedInstanceCount) != 0;
    }

    /// <summary>
    /// Registers a managed object as being aggregated by a given controlling outer object.
    /// </summary>
    /// <param name="instance">The managed object being aggregated.</param>
    /// <param name="controllingOuter">The controlling outer <c>IInspectable</c> object (not reference counted).</param>
    public static void Register(object instance, void* controllingOuter)
    {
        AggregatedInstances.AddOrUpdate(instance, new ControllingOuterReference { Value = (nint)controllingOuter });

        _ = Interlocked.Increment(ref AggregatedInstanceCount);
    }

    /// <summary>
    /// Unregisters a managed object previously registered with <see cref="Register"/>.
    /// </summary>
    /// <param name="instance">The managed object that is no longer aggregated.</param>
    public static void Unregister(object instance)
    {
        if (!AggregatedInstances.TryGetValue(instance, out ControllingOuterReference? reference))
        {
            return;
        }

        // Clear the outer before removing the entry, so that any 'IUnknown' call racing with this
        // one observes either the still valid outer, or no outer at all (ie. the standalone path).
        Volatile.Write(ref reference.Value, 0);

        _ = AggregatedInstances.Remove(instance);
        _ = Interlocked.Decrement(ref AggregatedInstanceCount);
    }

    /// <summary>
    /// Gets the controlling outer object for a given managed object, if it is being aggregated.
    /// </summary>
    /// <param name="instance">The managed object to look up.</param>
    /// <returns>The controlling outer object, or <see langword="null"/> if <paramref name="instance"/> is not aggregated.</returns>
    public static void* GetControllingOuter(object instance)
    {
        if (!HasAggregatedInstances)
        {
            return null;
        }

        if (!AggregatedInstances.TryGetValue(instance, out ControllingOuterReference? reference))
        {
            return null;
        }

        return (void*)Volatile.Read(ref reference.Value);
    }

    /// <summary>
    /// Tries to marshal a managed object taking part in COM aggregation for a given interface.
    /// </summary>
    /// <param name="instance">The managed object being marshalled.</param>
    /// <param name="iid">The IID of the interface being requested.</param>
    /// <param name="interfacePtr">The resulting interface pointer, if the aggregation path was taken and succeeded.</param>
    /// <param name="hresult">The <c>HRESULT</c> of the <c>QueryInterface</c> call, if the aggregation path was taken.</param>
    /// <returns>Whether <paramref name="instance"/> is aggregated, and this method handled the marshalling for it.</returns>
    /// <remarks>
    /// <para>
    /// The identity of an aggregate is its controlling outer object, so every interface pointer handed out for an
    /// aggregated managed object has to be produced by the controlling outer. That is also the only way to get the
    /// reference counting right: the interfaces the CCW of an aggregated object exposes delegate their <c>Release</c>
    /// calls to the controlling outer, so a <c>QueryInterface</c> call on the CCW itself would increment the wrong
    /// reference count (leaking the CCW, and over-releasing the controlling outer).
    /// </para>
    /// <para>
    /// This mirrors C++/WinRT, where converting <c>*this</c> to an interface on a composed object goes through
    /// <c>m_outer</c> as well (see <c>root_implements::QueryInterface</c>).
    /// </para>
    /// </remarks>
    public static bool TryQueryControllingOuter(object instance, in Guid iid, out void* interfacePtr, out HRESULT hresult)
    {
        // Fast path for the common case: no object in this application has ever been aggregated
        if (!HasAggregatedInstances)
        {
            interfacePtr = null;
            hresult = WellKnownErrorCodes.S_OK;

            return false;
        }

        void* controllingOuter = GetControllingOuter(instance);

        if (controllingOuter is null)
        {
            interfacePtr = null;
            hresult = WellKnownErrorCodes.S_OK;

            return false;
        }

        hresult = IUnknownVftbl.QueryInterfaceUnsafe(controllingOuter, in iid, out interfacePtr);

        return true;
    }

    /// <summary>
    /// A mutable holder for the controlling outer object of an aggregated managed object.
    /// </summary>
    private sealed class ControllingOuterReference
    {
        /// <summary>
        /// The controlling outer object (not reference counted), or <c>0</c> if it is no longer valid.
        /// </summary>
        public nint Value;
    }
}
