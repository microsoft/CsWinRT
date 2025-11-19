// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

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
/// <para>
/// Note that all methods in each attribute instance may be called concurrently by the CsWinRT infrastructure. If they require
/// any kind of synchronization, they should handle it internally. For instance, <see cref="ComputeVtables"/> should make sure
/// not to perform additional allocations that are not cleaned up correctly in case it's called concurrently by multiple threads.
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
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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
    /// Implementations are allowed to use <see cref="WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject"/>
    /// if they need to directly marshal a managed object via the built-in <see cref="ComWrappers"/> implementation in CsWinRT. If instead
    /// they need to use custom logic to marshal types, they are also allowed to create a native object to return in whichever manner is
    /// required for the scenario.
    /// </para>
    /// <para>
    /// The <paramref name="value"/> argument will never be <see langword="null"/>, and implementations don't have to validate that.
    /// </para>
    /// </remarks>
    public virtual void* GetOrCreateComInterfaceForObject(object value)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static NotSupportedException GetNotSupportedException()
            => new($"The current '{nameof(WindowsRuntimeComWrappersMarshallerAttribute)}' implementation does not support '{nameof(GetOrCreateComInterfaceForObject)}'.");

        throw GetNotSupportedException();
    }

    /// <summary>
    /// Computes the vtables for the annotated type.
    /// </summary>
    /// <param name="count">The number of elements contained in the returned memory.</param>
    /// <returns>A <see cref="ComWrappers.ComInterfaceEntry"/> pointer containing memory for all COM interface entries for the annotated type.</returns>
    /// <remarks>
    /// <para>
    /// The retrieved vtables represent the full set of vtables on the resulting CCW produced for the managed object after marshalling.
    /// This means that implementing types are responsible for also adding all fundamental WinRT interfaces to the set, as applicable.
    /// </para>
    /// <para>
    /// These interfaces are as follows:
    /// <list type="bullet">
    ///   <item><see href="https://learn.microsoft.com/uwp/api/windows.foundation.istringable"><c>IStringable</c></see></item>
    ///   <item><see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.data.icustompropertyprovider"><c>ICustomPropertyProvider</c></see></item>
    ///   <item><see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreferencesource"><c>IWeakReferenceSource</c></see></item>
    ///   <item><see href="https://learn.microsoft.com/windows/win32/api/objidl/nn-objidl-imarshal"><c>IMarshal</c></see></item>
    ///   <item><see href="https://learn.microsoft.com/windows/win32/api/objidlbase/nn-objidlbase-iagileobject"><c>IAgileObject</c></see></item>
    ///   <item><see href="https://learn.microsoft.com/windows/win32/api/inspectable/nn-inspectable-iinspectable"><c>IInspectable</c></see></item>
    ///   <item><see href="https://learn.microsoft.com/windows/win32/api/unknwn/nn-unknwn-iunknown"><c>IUnknown</c></see></item>
    /// </list>
    /// Note that the <c>IUnknown</c> interface must always be the last one in the returned memory. Additionally, the <c>IStringable</c>,
    /// <c>IWeakReferenceSource</c>, <c>IAgileObject</c>, and <c>IInspectable</c> interfaces must also always be present in the set.
    /// </para>
    /// </remarks>
    /// <seealso cref="ComWrappers.ComputeVtables"/>
    public virtual ComWrappers.ComInterfaceEntry* ComputeVtables(out int count)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static NotSupportedException GetNotSupportedException()
            => new($"The current '{nameof(WindowsRuntimeComWrappersMarshallerAttribute)}' implementation does not support '{nameof(ComputeVtables)}'.");

        throw GetNotSupportedException();
    }

    /// <summary>
    /// Creates a managed Windows Runtime object for a given native object.
    /// </summary>
    /// <param name="value">The input native object to marshal.</param>
    /// <param name="wrapperFlags">Flags used to describe the created wrapper object.</param>
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
    /// <seealso cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>
    public virtual object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static NotSupportedException GetNotSupportedException()
            => new($"The current '{nameof(WindowsRuntimeComWrappersMarshallerAttribute)}' implementation does not support '{nameof(CreateObject)}'.");

        throw GetNotSupportedException();
    }
}