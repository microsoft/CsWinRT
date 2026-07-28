// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates a projected Windows Runtime class type for which CCW (COM Callable Wrapper) marshalling code
/// should be generated, so that its instances can be marshalled to native code through a CCW.
/// </summary>
/// <remarks>
/// <para>
/// CCW marshalling code is normally generated automatically for all managed types that implement one or more
/// Windows Runtime interfaces. Projected Windows Runtime types are an exception, as they are backed by native
/// objects and are marshalled by unwrapping the underlying native object directly, which means they normally
/// never require a CCW. This attribute allows explicitly opting a projected class type into CCW marshalling
/// code generation, for the niche scenarios where such support is required.
/// </para>
/// <para>
/// One such scenario involves using a projected collection type as an items source for a XAML control.
/// Consider using a <c>DependencyObjectCollection</c> as the items source for an <c>ItemsControl</c>. The
/// control will query the object assigned to its items source (which is marshalled as an <c>IInspectable</c>
/// value) for the <c>IBindableIterable</c> and <c>IIterable&lt;IInspectable&gt;</c> interfaces. This normally
/// succeeds when assigning most collection types. However, <c>DependencyObjectCollection</c> implements neither
/// of those interfaces. It implements <c>IIterable&lt;DependencyObject&gt;</c>, and Windows Runtime generic
/// interfaces are not covariant on the native side, meaning each generic instantiation of a given generic
/// Windows Runtime interface has a completely different IID.
/// </para>
/// <para>
/// To handle this, XAML controls perform the following steps:
/// <list type="number">
///   <item>They query the source object for <c>IBindableIterable</c> and <c>IIterable&lt;IInspectable&gt;</c>.</item>
///   <item>If that fails, they obtain a CCW from the source object and query that CCW instead.</item>
/// </list>
/// </para>
/// <para>
/// This might seem surprising, given that a projected type is backed by a native object, and so it would just
/// be unwrapped when marshalled, with no CCW involved. The .NET runtime has dedicated logic for this case,
/// where it will perform the following steps:
/// <list type="number">
///   <item>It creates an RCW (Runtime Callable Wrapper) for the native object.</item>
///   <item>It uses <c>ComWrappers</c> to obtain a CCW for that RCW, to be used as a proxy.</item>
/// </list>
/// That is, in this scenario the reference from the control to the object is transformed from a direct
/// reference into a reference to a CCW that wraps an RCW over the original native object.
/// </para>
/// <para>
/// Because the RCW implements <c>IEnumerable&lt;DependencyObject&gt;</c>, and that interface is covariant in
/// C#, marshalling it to native makes the query for <c>IIterable&lt;IInspectable&gt;</c> succeed, by going
/// through this reference cycle with the proxy object. This works because CsWinRT computes all variant versions
/// of each implemented interface on managed objects, and generates vtables for all of them. As a consequence,
/// such native objects can only be used as items sources from .NET applications, and not from C++ applications.
/// </para>
/// <para>
/// Applying this attribute to a projected class type explicitly requests that a CCW vtable be generated for it,
/// which would not normally be present, since the type is just a projection.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class WindowsRuntimeNativeExposedTypeAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeNativeExposedTypeAttribute"/> instance with the specified parameters.
    /// </summary>
    /// <param name="type">The projected Windows Runtime class type to generate CCW marshalling code for.</param>
    public WindowsRuntimeNativeExposedTypeAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets the projected Windows Runtime class type to generate CCW marshalling code for.
    /// </summary>
    public Type Type { get; }
}
