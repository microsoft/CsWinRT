// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates a mapped type for a Windows Runtime type projection (ie. a metadata provider type).
/// </summary>
/// <remarks>
/// <para>
/// Using this attribute is only valid with supported projected generic type instantiations.
/// </para>
/// <para>
/// This is the full list of supported types that can be used for <see cref="GenericTypeInstantiation"/>:
/// <list type="bullet">
///   <item><see cref="Array"/></item>
///   <item><see cref="Nullable{T}"/></item>
///   <item><see cref="EventHandler{TEventArgs}"/></item>
///   <item><see cref="System.Collections.Generic.IEnumerable{T}"/></item>
///   <item><see cref="System.Collections.Generic.IList{T}"/></item>
///   <item><see cref="System.Collections.Generic.IReadOnlyList{T}"/></item>
///   <item><see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/></item>
///   <item><see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/></item>
///   <item><see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/></item>
///   <item><see href="https://learn.microsoft.com/uwp/api/windows.foundation.typedeventhandler-2"><c>Windows.Foundation.TypedEventHandler&lt;TSender, TResult&gt;</c></see></item>
///   <item><see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1"><c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c></see></item>
///   <item><see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncoperation-1"><c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;</c></see></item>
///   <item><see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncoperationwithprogress-2"><c>Windows.Foundation.IAsyncOperation&lt;TResult, TProgress&gt;</c></see></item>
/// </list>
/// </para>
/// <para>
/// All type arguments must be valid Windows Runtime type arguments as well.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class WindowsRuntimeGenericTypeInstantiationAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeGenericTypeInstantiationAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="genericTypeInstantiation">The generic type instantiation for this instance.</param>
    public WindowsRuntimeGenericTypeInstantiationAttribute(Type genericTypeInstantiation)
    {
        GenericTypeInstantiation = genericTypeInstantiation;
    }

    /// <summary>
    /// Gets the generic type instantiation for this instance.
    /// </summary>
    public Type GenericTypeInstantiation { get; }
}
