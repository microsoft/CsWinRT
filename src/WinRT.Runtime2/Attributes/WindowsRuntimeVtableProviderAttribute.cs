// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// An attribute that allows retrieving the vtable to marshal managed objects to native.
/// It can be applied directly to types, or to their associated proxy types.
/// </summary>
/// <remarks>
/// This attribute is primarily meant to be used by types provided by CsWinRT, or generated at compile time by its source generators.
/// However, it is possible to implement it manually as well, to support advanced scenarios where further customization was needed.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Interface |
    AttributeTargets.Enum |
    AttributeTargets.Struct |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
public abstract class WindowsRuntimeVtableProviderAttribute : Attribute
{
    /// <summary>
    /// Computes the vtables for the annotated type.
    /// </summary>
    /// <param name="bufferWriter">The input <see cref="IBufferWriter{T}"/> object to append the compute vtables to.</param>
    /// <remarks>
    /// The retrieved vtables do not represent the full set of vtables on the resulting CCW produced for the managed object
    /// after marshalling. Rather, they will be added to the default set of vtables provided automatically by CsWinRT. For
    /// instance, that will include vtables for fundamental interfaces such as <c>IUnknown</c> and <c>IInspectable</c>.
    /// </remarks>
    /// <seealso cref="ComWrappers.ComputeVtables"/>
    public abstract void ComputeVtables(IBufferWriter<ComWrappers.ComInterfaceEntry> bufferWriter);
}
