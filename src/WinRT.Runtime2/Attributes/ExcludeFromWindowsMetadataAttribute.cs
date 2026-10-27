// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime;

/// <summary>
/// Excludes a type or member from the Windows Runtime metadata generated for a component.
/// </summary>
/// <remarks>
/// <para>
/// The declaration remains available in the managed assembly. This attribute does not change its
/// managed accessibility or suppress COM interface exposure.
/// </para>
/// <para>
/// Excluding a type also excludes its members. Excluding a base class does not automatically exclude
/// classes that inherit from it. An exported derived class uses <see cref="object"/> as its base in
/// the generated metadata; inherited members are omitted from metadata unless re-exposed by the derived class
/// or a retained interface. Windows Runtime and custom-mapped interfaces inherited only through the
/// excluded base are omitted too; redeclare an interface on the derived class to publish its contract.
/// Inherited public implementations can satisfy that declaration. Omitting an interface from metadata
/// does not suppress native <c>QueryInterface</c> support.
/// Exported signatures cannot reference excluded types, and a member required by an exported
/// interface cannot be excluded independently. Apply the attribute to a whole property or event,
/// not an accessor. Instance fields of exported structs cannot be excluded because they define
/// the Windows Runtime layout.
/// </para>
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface |
    AttributeTargets.Enum |
    AttributeTargets.Delegate |
    AttributeTargets.Constructor |
    AttributeTargets.Method |
    AttributeTargets.Property |
    AttributeTargets.Event |
    AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ExcludeFromWindowsMetadataAttribute : Attribute;