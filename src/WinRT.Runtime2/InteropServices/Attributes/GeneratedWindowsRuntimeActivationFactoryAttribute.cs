// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Indicates that CsWinRT should generate the activation factory for the Windows Runtime class implemented by the annotated type.
/// </summary>
/// <remarks>
/// <para>
/// This attribute applies to a type implementing a Windows Runtime class defined in existing metadata (i.e. deriving from a
/// generated implementable base class). CsWinRT then generates an activation factory constructing the annotated type, and
/// registers it for the runtime class name of the implemented class, so that activation requests for it are served.
/// </para>
/// <para>
/// This is only possible when the implemented class is activated through its parameterless constructor alone (that is, it has
/// no constructors with parameters and no statics), and the annotated type has a parameterless constructor accessible within
/// its assembly. Otherwise, the activation factory has to be declared explicitly with <see cref="WindowsRuntimeActivationFactoryAttribute"/>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeneratedWindowsRuntimeActivationFactoryAttribute : Attribute;
