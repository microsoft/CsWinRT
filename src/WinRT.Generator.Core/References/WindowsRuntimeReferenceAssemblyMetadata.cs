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
    /// The key for one fully qualified Windows Runtime class name that the reference projection declares
    /// generated bases for, so the class can be implemented in C# (<c>CsWinRTImplementWinMDTypes</c>).
    /// </summary>
    /// <remarks>
    /// The bases are found by name (see <see cref="GetImplementableClassBaseTypeName"/> and
    /// <see cref="GetImplementableClassFactoryBaseTypeName"/>). A class can have either or both: a static class
    /// only has a factory base, and a class with no activation and no statics only has an instance base.
    /// </remarks>
    public const string ImplementableClass = "CsWinRT.ImplementableClass.v1";

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
