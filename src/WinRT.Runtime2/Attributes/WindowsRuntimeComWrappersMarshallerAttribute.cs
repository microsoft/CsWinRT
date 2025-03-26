// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// An attribute that allows marshalling managed objects to native via the <see cref="ComWrappers"/> infrastructure.
/// It can be applied directly to types, or to their associated proxy types, to provide custom marshalling behavior.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is used in either of the following scenarios:
/// <list type="bullet">
///   <item>Projected types (either RCWs, or boxed value types).</item>
///   <item>Managed types (in which case, the attribute is on their associated proxy types).</item>
/// </list>
/// </para>
/// <para>
/// This attribute is only meant to be used to marshal objects which implement the <c>IInspectable</c> interface.
/// </para>
/// <para>
/// This attribute is primarily meant to be used by types provided by CsWinRT, or generated at compile time by its source generators.
/// However, it is possible to implement it manually as well, to support advanced scenarios where further customization was needed.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
public abstract unsafe class WindowsRuntimeComWrappersMarshallerAttribute : Attribute
{
    /// <summary>
    /// Marshals a given object as a COM pointer that can be passed to native code through the Windows Runtime ABI.
    /// </summary>
    /// <param name="value">The managed object to marshal to native callers.</param>
    /// <returns>The resulting COM pointer for <paramref name="value"/> to pass to native callers.</returns>
    /// <remarks>
    /// <para>
    /// The returned pointer is allowed to be an <c>IUnknown</c> interface pointer. However, the underlying object must
    /// implement <c>IInspectable</c> as well, as that is how it will actually be marshalled via the Windows Runtime ABI.
    /// </para>
    /// <para>
    /// Implementations of this method are free to customize how the native object is created (eg. by suppressing the reference
    /// tracking support, where applicable). They can also return new "equivalent values" when needed (eg. for custom mapped types).
    /// </para>
    /// <para>
    /// The default implementation will use the <see cref="ComWrappers"/> implementation in CsWinRT, with reference tracking support.
    /// </para>
    /// <para>
    /// The <paramref name="value"/> argument will never be <see langword="null"/>, and implementations don't have to validate that.
    /// </para>
    /// </remarks>
    /// <seealso cref="ComWrappers.GetOrCreateComInterfaceForObject"/>"/>
    public virtual void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

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

    /// <summary>
    /// Creates a managed Windows Runtime object for a given native object.
    /// </summary>
    /// <param name="value">The input native object to marshal.</param>
    /// <returns>The resulting managed Windows Runtime object.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="value"/> parameter will be some <c>IInspectable</c> interface pointer, and
    /// implementations of this attribute are required to call <c>QueryInterface</c> for the right IID.
    /// </para>
    /// <para>
    /// The <paramref name="value"/> argument will never be <see langword="null"/>, and implementations don't have to validate that.
    /// </para>
    /// </remarks>
    /// <seealso cref="ComWrappers.CreateObject"/>
    public abstract object CreateObject(void* value);
}
