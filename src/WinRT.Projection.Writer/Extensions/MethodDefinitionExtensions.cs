// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="MethodDefinition"/>.
/// </summary>
internal static class MethodDefinitionExtensions
{
    extension(MethodDefinition method)
    {
        /// <summary>
        /// Returns whether the method is an instance constructor (i.e. its name is
        /// <c>.ctor</c> and it is marked as runtime-special).
        /// </summary>
        /// <returns><see langword="true"/> if the method is an instance constructor; otherwise <see langword="false"/>.</returns>
        public bool IsConstructor()
            => method.IsRuntimeSpecialName && method.Name == ".ctor";

        /// <summary>
        /// Returns whether the method is special (has either <c>SpecialName</c> or
        /// <c>RuntimeSpecialName</c> set in its method attributes -- e.g. property accessors,
        /// event accessors, constructors).
        /// </summary>
        /// <returns><see langword="true"/> if the method is marked special; otherwise <see langword="false"/>.</returns>
        public bool IsSpecial()
            => method.IsSpecialName || method.IsRuntimeSpecialName;

        /// <summary>
        /// Returns whether the method is the special <c>remove_xxx</c> event remover overload.
        /// </summary>
        /// <returns><see langword="true"/> if the method is an event remover; otherwise <see langword="false"/>.</returns>
        public bool IsRemoveOverload()
            => method.IsSpecialName && (method.Name?.Value?.StartsWith("remove_", System.StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method carries the <c>[NoExceptionAttribute]</c> or is a
        /// <see cref="IsRemoveOverload"/> (event removers are implicitly no-throw).
        /// </summary>
        /// <returns><see langword="true"/> if the method is documented to never throw; otherwise <see langword="false"/>.</returns>
        public bool IsNoExcept()
            => method.IsRemoveOverload() || method.HasAttribute("Windows.Foundation.Metadata", "NoExceptionAttribute");
    }
}