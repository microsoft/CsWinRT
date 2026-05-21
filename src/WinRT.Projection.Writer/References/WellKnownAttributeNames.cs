// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.References;

/// <summary>
/// Well-known <c>Windows.Foundation.Metadata</c> attribute type names recognized by the
/// projection writer.
/// </summary>
internal static class WellKnownAttributeNames
{
    /// <summary>
    /// The <c>[ActivatableAttribute]</c> attribute type name.
    /// </summary>
    public const string ActivatableAttribute = "ActivatableAttribute";

    /// <summary>
    /// The <c>[StaticAttribute]</c> attribute type name.
    /// </summary>
    public const string StaticAttribute = "StaticAttribute";

    /// <summary>
    /// The <c>[ComposableAttribute]</c> attribute type name.
    /// </summary>
    public const string ComposableAttribute = "ComposableAttribute";

    /// <summary>
    /// The <c>[DefaultAttribute]</c> attribute type name (marks the default interface of a runtime class).
    /// </summary>
    public const string DefaultAttribute = "DefaultAttribute";

    /// <summary>
    /// The <c>[OverridableAttribute]</c> attribute type name (marks an overridable interface).
    /// </summary>
    public const string OverridableAttribute = "OverridableAttribute";

    /// <summary>
    /// The <c>[ExclusiveToAttribute]</c> attribute type name (marks an interface as exclusive to a class).
    /// </summary>
    public const string ExclusiveToAttribute = "ExclusiveToAttribute";

    /// <summary>
    /// The <c>[FastAbiAttribute]</c> attribute type name (enables the merged fast-abi vtable layout).
    /// </summary>
    public const string FastAbiAttribute = "FastAbiAttribute";

    /// <summary>
    /// The <c>[NoExceptionAttribute]</c> attribute type name (marks a method/property as non-throwing).
    /// </summary>
    public const string NoExceptionAttribute = "NoExceptionAttribute";

    /// <summary>
    /// The <c>[ContractVersionAttribute]</c> attribute type name.
    /// </summary>
    public const string ContractVersionAttribute = "ContractVersionAttribute";

    /// <summary>
    /// The <c>[VersionAttribute]</c> attribute type name.
    /// </summary>
    public const string VersionAttribute = "VersionAttribute";
}
