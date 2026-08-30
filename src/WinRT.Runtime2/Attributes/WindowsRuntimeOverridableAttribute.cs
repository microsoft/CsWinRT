// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates that a Windows Runtime interface authored in a component is an <c>[Overridable]</c> interface of the
/// composable runtime classes implementing it, i.e. that a derived type is allowed to replace its implementation.
/// </summary>
/// <remarks>
/// <para>
/// This is the C# equivalent of declaring an <c>[overridable] interface</c> member on a runtime class in MIDL, which
/// is the shape XAML uses for members such as <c>IControlOverrides</c>. Windows Runtime has no notion of a public
/// overridable member, so the members of an overridable interface are surfaced as <c>protected</c> by the language
/// projections consuming the class, and only the derived (composing) type can replace them.
/// </para>
/// <para>
/// Applying this attribute to an interface only has an effect on classes that are projected as composable runtime
/// classes (i.e. public unsealed classes with at least one public constructor). On any other class, the interface is
/// projected as an ordinary required interface.
/// </para>
/// <para>
/// On a composable class, the interface is emitted as <c>[ExclusiveTo]</c> that class in the generated <c>.winmd</c>,
/// which is what Windows Runtime metadata requires of every <c>[Overridable]</c> interface. It is still nameable from
/// the authoring component and from consumers (exactly like <c>IControlOverrides</c>), it just stops being part of the
/// general purpose public surface of the component: language projections surface its members on the class itself.
/// </para>
/// <para>
/// A composable class implementing an overridable interface can dispatch to the most derived implementation of its
/// members by resolving the controlling outer object with
/// <see cref="InteropServices.WindowsRuntimeComposition.GetControllingOuterObject"/>, which is the C#/WinRT
/// equivalent of <c>overridable()</c> in C++/WinRT.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class WindowsRuntimeOverridableAttribute : Attribute;
