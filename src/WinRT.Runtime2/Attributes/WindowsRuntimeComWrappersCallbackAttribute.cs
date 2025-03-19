// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// An attribute for callbacks for <see cref="System.Runtime.InteropServices.ComWrappers.CreateObject"/>, for Windows Runtime objects.
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
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
public abstract unsafe class WindowsRuntimeComWrappersCallbackAttribute : Attribute
{
    /// <summary>
    /// Creates a managed Windows Runtime object for a given native object.
    /// </summary>
    /// <param name="value">The input native object to marshal.</param>
    /// <returns>The resulting managed Windows Runtime object.</returns>
    /// <remarks>
    /// The <paramref name="value"/> parameter will be some <c>IInspectable</c> interface pointer, and
    /// implementations of this attribute are required to call <c>QueryInterface</c> for the right IID.
    /// </remarks>
    public abstract object CreateObject(void* value);
}
