// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.Generator.References;

/// <summary>
/// The generator-owned metadata contract carried by Windows Runtime reference projections.
/// </summary>
internal static class WindowsRuntimeReferenceAssemblyMetadata
{
    /// <summary>
    /// The fully qualified name of the key/value metadata attribute.
    /// </summary>
    public const string AttributeTypeName = "WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyMetadataAttribute";

    /// <summary>
    /// The key for one fully qualified exclusive-to interface name requiring dynamic interface casting.
    /// Repeated entries form the effective selection; no entries means no exclusive-to IDIC.
    /// </summary>
    public const string IdicExclusiveTo = "CsWinRT.IdicExclusiveTo.v1";

    /// <summary>
    /// The key for one fully qualified Windows Runtime class name whose instance base (see
    /// <see cref="GetImplementableClassBaseTypeName"/>) the reference projection declares, so the class
    /// can be implemented in C# (<c>CsWinRTImplementWinMDTypes</c>).
    /// </summary>
    public const string ImplementableClass = "CsWinRT.ImplementableClass.v1";

    /// <summary>
    /// The key for one fully qualified Windows Runtime class name whose activation factory base (see
    /// <see cref="GetImplementableClassFactoryBaseTypeName"/>) the reference projection declares.
    /// </summary>
    public const string ImplementableClassFactory = "CsWinRT.ImplementableClassFactory.v1";

    /// <summary>
    /// The key for one fully qualified Windows Runtime class name whose activation factory base only declares
    /// <c>ActivateInstance</c> (no constructors taking arguments, and no statics), which CsWinRT can implement
    /// itself. Only ever recorded alongside <see cref="ImplementableClassFactory"/>.
    /// </summary>
    public const string ImplementableClassDefaultActivationOnly = "CsWinRT.ImplementableClassDefaultActivationOnly.v1";

    /// <summary>
    /// Gets the full name of the instance base generated for an implementable Windows Runtime class.
    /// </summary>
    /// <param name="runtimeClassName">The fully qualified Windows Runtime class name.</param>
    /// <returns>The full name of the base, <c>ABI.&lt;Namespace&gt;.&lt;Class&gt;</c>.</returns>
    public static string GetImplementableClassBaseTypeName(string runtimeClassName)
    {
        return $"ABI.{runtimeClassName}";
    }

    /// <summary>
    /// Gets the full name of the activation factory base generated for an implementable Windows Runtime class.
    /// </summary>
    /// <param name="runtimeClassName">The fully qualified Windows Runtime class name.</param>
    /// <returns>The full name of the factory base, <c>ABI.&lt;Namespace&gt;.&lt;Class&gt;ActivationFactory</c>.</returns>
    public static string GetImplementableClassFactoryBaseTypeName(string runtimeClassName)
    {
        return $"ABI.{runtimeClassName}ActivationFactory";
    }
}
