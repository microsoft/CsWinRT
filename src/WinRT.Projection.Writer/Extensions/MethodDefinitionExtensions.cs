// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="MethodDefinition"/>.
/// </summary>
internal static class MethodDefinitionExtensions
{
    /// <summary>
    /// Returns whether <paramref name="method"/> is an instance constructor (i.e. its name is
    /// <c>.ctor</c> and it is marked as runtime-special).
    /// </summary>
    /// <param name="method">The method definition to inspect.</param>
    /// <returns><see langword="true"/> if the method is an instance constructor; otherwise <see langword="false"/>.</returns>
    public static bool IsConstructor(this MethodDefinition method)
        => method.IsRuntimeSpecialName && method.Name == ".ctor";

    /// <summary>
    /// Returns whether <paramref name="method"/> is special (has either <c>SpecialName</c> or
    /// <c>RuntimeSpecialName</c> set in its method attributes -- e.g. property accessors,
    /// event accessors, constructors).
    /// </summary>
    /// <param name="method">The method definition to inspect.</param>
    /// <returns><see langword="true"/> if the method is marked special; otherwise <see langword="false"/>.</returns>
    public static bool IsSpecial(this MethodDefinition method)
        => method.IsSpecialName || method.IsRuntimeSpecialName;

    /// <summary>
    /// Returns whether <paramref name="method"/> is the special <c>remove_xxx</c> event remover
    /// overload (mirrors C++ <c>is_remove_overload</c>).
    /// </summary>
    /// <param name="method">The method definition to inspect.</param>
    /// <returns><see langword="true"/> if the method is an event remover; otherwise <see langword="false"/>.</returns>
    public static bool IsRemoveOverload(this MethodDefinition method)
        => method.IsSpecialName && (method.Name?.Value?.StartsWith("remove_", System.StringComparison.Ordinal) == true);

    /// <summary>
    /// Returns whether <paramref name="method"/> carries the <c>[NoExceptionAttribute]</c> or
    /// is a <see cref="IsRemoveOverload"/> (event removers are implicitly no-throw).
    /// </summary>
    /// <param name="method">The method definition to inspect.</param>
    /// <returns><see langword="true"/> if the method is documented to never throw; otherwise <see langword="false"/>.</returns>
    public static bool IsNoExcept(this MethodDefinition method)
        => method.IsRemoveOverload() || method.HasAttribute("Windows.Foundation.Metadata", "NoExceptionAttribute");
}
