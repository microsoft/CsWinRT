// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A dummy placeholder object to represent <see langword="null"/> values being marshalled.
/// </summary>
/// <remarks>
/// <para>
/// This type can be used to represent <see langword="null"/> values for projected Windows Runtime types being marshalled to
/// managed. The reason why this exists is that
/// <see cref="System.Runtime.InteropServices.ComWrappers.CreateObject(nint, System.Runtime.InteropServices.CreateObjectFlags, object?, out System.Runtime.InteropServices.CreatedWrapperFlags)"/>
/// doesn't allow returning <see langword="null"/> values, and it will throw an exception if that happens. To avoid that, this type
/// can be used instead (ie. by returning <see cref="Instance"/>). Callers are expected to then check for this instance, and
/// convert to an actual <see langword="null"/> value. 3rd-party code should never see or receive instances of this type.
/// </para>
/// <para>
/// An example of where this is used is by <see cref="ABI.System.Exception"/>. Because exceptions are marshalled to <see langword="null"/>
/// when the native <c>HRESULT</c> is <c>0</c>, the object marshaller for them could potentially return <see langword="null"/> if the
/// input object were some <c>IReference`1</c> wrapping such an error code. Instead, that marshaller can use <see cref="NullPlaceholder"/>.
/// </para>
/// </remarks>
internal sealed unsafe class NullPlaceholder
{
    /// <summary>
    /// The shared instance of <see cref="NullPlaceholder"/>.
    /// </summary>
    public static readonly NullPlaceholder Instance = new();

    /// <summary>
    /// Creates a new <see cref="NullPlaceholder"/> instance.
    /// </summary>
    private NullPlaceholder()
    {
    }
}