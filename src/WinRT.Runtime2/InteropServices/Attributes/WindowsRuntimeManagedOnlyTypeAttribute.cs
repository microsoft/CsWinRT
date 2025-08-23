// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates a type that is only meant to be used as a managed type, and should be excluded
/// from processing for generated marshalling code for interop with Windows Runtime.
/// </summary>
/// <remarks>
/// <para>
/// Instances of types annotated with <see cref="WindowsRuntimeManagedOnlyTypeAttribute"/> can still
/// be marshalled to native code as a CCW (COM Callable Wrapper), however they will always be treated
/// as opaque objects (i.e. marshalled as <c>IInspectable</c> values), with no additional interfaces.
/// </para>
/// <para>
/// This attribute will apply to all types derived from annotated types as well. It will not apply to
/// types used as generic type arguments (e.g. <see cref="System.Collections.Generic.List{T}"/> with
/// the element type being some type annotated with <see cref="WindowsRuntimeManagedOnlyTypeAttribute"/>).
/// </para>
/// <para>
/// This attribute is meant to be used in advanced scenarios, specifically to minimize binary size.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class WindowsRuntimeManagedOnlyTypeAttribute : Attribute;
