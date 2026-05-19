// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

namespace WindowsRuntime.ProjectionWriter;

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
            => method.IsSpecialName && (method.Name?.Value?.StartsWith("remove_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method is a property getter (special method whose name starts with <c>get_</c>).
        /// </summary>
        /// <returns><see langword="true"/> if the method is a property getter; otherwise <see langword="false"/>.</returns>
        public bool IsGetter()
            => method.IsSpecialName && (method.Name?.Value?.StartsWith("get_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method is a property setter (special method whose name starts with <c>put_</c>).
        /// </summary>
        /// <returns><see langword="true"/> if the method is a property setter; otherwise <see langword="false"/>.</returns>
        public bool IsSetter()
            => method.IsSpecialName && (method.Name?.Value?.StartsWith("put_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method is an event adder (special method whose name starts with <c>add_</c>).
        /// </summary>
        /// <returns><see langword="true"/> if the method is an event adder; otherwise <see langword="false"/>.</returns>
        public bool IsAdder()
            => method.IsSpecialName && (method.Name?.Value?.StartsWith("add_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method is an event remover (alias for <see cref="IsRemoveOverload"/>).
        /// </summary>
        /// <returns><see langword="true"/> if the method is an event remover; otherwise <see langword="false"/>.</returns>
        public bool IsRemover() => method.IsRemoveOverload();

        /// <summary>
        /// Returns whether the method carries the <c>[NoExceptionAttribute]</c> or is a
        /// <see cref="IsRemoveOverload"/> (event removers are implicitly no-throw).
        /// </summary>
        /// <returns><see langword="true"/> if the method is documented to never throw; otherwise <see langword="false"/>.</returns>
        public bool IsNoExcept()
            => method.IsRemoveOverload() || method.HasWindowsFoundationMetadataAttribute(NoExceptionAttribute);
    }
}
