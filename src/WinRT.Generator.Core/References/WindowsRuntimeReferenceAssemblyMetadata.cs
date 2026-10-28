// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Helpers;

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
    /// The key prefix for the default interface signature of one Windows Runtime class.
    /// </summary>
    /// <remarks>
    /// The key is completed with the fully qualified class name, and the value is the Windows Runtime signature of the
    /// class's default interface (see <see cref="TryGetDefaultInterfaceIid"/>). This lets consumers compute the class's
    /// IID and signature even when its default interface is exclusive and therefore absent from the reference projection.
    /// </remarks>
    public const string DefaultInterfaceSignaturePrefix = "CsWinRT.DefaultInterfaceSignature.v1:";

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

    /// <summary>
    /// Gets the IID of a Windows Runtime class's default interface, from its <see cref="DefaultInterfaceSignaturePrefix"/> value.
    /// </summary>
    /// <param name="signature">The signature of the default interface.</param>
    /// <param name="iid">The resulting IID, if <paramref name="signature"/> is valid.</param>
    /// <returns>Whether <paramref name="iid"/> was retrieved successfully.</returns>
    public static bool TryGetDefaultInterfaceIid(string signature, out Guid iid)
    {
        // Parameterized interfaces are identified by their signature hash, all other interfaces by their braced IID
        if (signature.StartsWith("pinterface(", StringComparison.Ordinal) && signature.EndsWith(')'))
        {
            iid = GuidGenerator.Generate(signature);

            return true;
        }

        return Guid.TryParseExact(signature, "B", out iid);
    }
}
