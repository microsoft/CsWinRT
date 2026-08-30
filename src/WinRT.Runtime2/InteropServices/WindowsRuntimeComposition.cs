// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
#if !WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using WindowsRuntime.InteropServices.Marshalling;
#endif

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides support for authored Windows Runtime classes taking part in COM aggregation, i.e. composable
/// (unsealed) runtime classes that have been derived from by a Windows Runtime object implemented outside .NET.
/// </summary>
/// <remarks>
/// <para>
/// When a composable class authored in C# is derived from by, for instance, a C++/WinRT type, the managed instance
/// becomes the <em>inner</em> object of a COM aggregate, and the derived native object becomes its
/// <em>controlling outer</em> object. From that point on, the identity of the aggregate (as seen by native code)
/// is the controlling outer, and the members a derived type is allowed to override resolve on it.
/// </para>
/// <para>
/// Plain virtual dispatch in C# cannot see that native override: the managed instance is the base object of the
/// aggregate, so calling one of its own overridable members always runs the managed implementation. This mirrors
/// C++/WinRT, where calling a member directly also runs the base implementation, and dispatching to the most derived
/// one requires going through the controlling outer explicitly (the <c>overridable()</c> helper). The APIs on this
/// type are the C#/WinRT equivalent of that helper.
/// </para>
/// </remarks>
public static unsafe class WindowsRuntimeComposition
{
    /// <summary>
    /// Gets whether a given authored object is currently taking part in COM aggregation.
    /// </summary>
    /// <param name="instance">The authored object to check.</param>
    /// <returns>Whether <paramref name="instance"/> is the inner object of a COM aggregate.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is <see langword="null"/>.</exception>
    public static bool IsAggregated(object instance)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
        ArgumentNullException.ThrowIfNull(instance);

        return WindowsRuntimeAggregation.GetControllingOuter(instance) is not null;
#endif
    }

    /// <summary>
    /// Gets the controlling outer object of a given authored object taking part in COM aggregation.
    /// </summary>
    /// <param name="instance">The authored object to get the controlling outer object for.</param>
    /// <returns>
    /// The controlling outer object of <paramref name="instance"/>, or <see langword="null"/> if
    /// <paramref name="instance"/> is not taking part in COM aggregation.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This is the C#/WinRT equivalent of <c>overridable()</c> in C++/WinRT: casting the returned object to an
    /// <c>[Overridable]</c> interface of the composable class (see <see cref="WindowsRuntimeOverridableAttribute"/>)
    /// resolves the most derived implementation of its members, so calling one of them runs the override supplied by
    /// the derived Windows Runtime object rather than the managed base implementation.
    /// </para>
    /// <para>
    /// The returned object must not be stored on <paramref name="instance"/> (nor on anything it keeps alive): the
    /// controlling outer holds the only reference to the aggregated object, so a strong reference in the opposite
    /// direction would keep the whole aggregate alive forever. Resolve it for the duration of the call instead,
    /// exactly like <c>overridable()</c> does.
    /// </para>
    /// </remarks>
    public static object? GetControllingOuterObject(object instance)
    {
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
        throw null;
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
        ArgumentNullException.ThrowIfNull(instance);

        void* controllingOuter = WindowsRuntimeAggregation.GetControllingOuter(instance);

        if (controllingOuter is null)
        {
            return null;
        }

        // The controlling outer is not reference counted by the aggregate (see 'WindowsRuntimeAggregation'), so the
        // pointer is only borrowed here. Marshalling it does not transfer ownership: the resulting RCW takes its own
        // reference on it, which is exactly what makes it safe to use past the lifetime of this call.
        return WindowsRuntimeObjectMarshaller.ConvertToManaged(controllingOuter);
#endif
    }
}
