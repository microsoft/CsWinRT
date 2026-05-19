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
        public bool IsConstructor => method.IsRuntimeSpecialName && method.Name == ".ctor";

        /// <summary>
        /// Returns whether the method is special (has either <c>SpecialName</c> or
        /// <c>RuntimeSpecialName</c> set in its method attributes -- e.g. property accessors,
        /// event accessors, constructors).
        /// </summary>
        public bool IsSpecial => method.IsSpecialName || method.IsRuntimeSpecialName;

        /// <summary>
        /// Returns whether the method is a property getter (special method whose name starts with <c>get_</c>).
        /// </summary>
        public bool IsGetter => method.IsSpecialName && (method.Name?.Value?.StartsWith("get_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method is a property setter (special method whose name starts with <c>put_</c>).
        /// </summary>
        public bool IsSetter => method.IsSpecialName && (method.Name?.Value?.StartsWith("put_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method is an event adder (special method whose name starts with <c>add_</c>).
        /// </summary>
        public bool IsAdder => method.IsSpecialName && (method.Name?.Value?.StartsWith("add_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method is the special <c>remove_xxx</c> event remover overload.
        /// </summary>
        public bool IsRemover => method.IsSpecialName && (method.Name?.Value?.StartsWith("remove_", StringComparison.Ordinal) == true);

        /// <summary>
        /// Returns whether the method declares no parameters.
        /// </summary>
        public bool IsParameterless => method.Parameters.Count == 0;

        /// <summary>
        /// Returns whether the method is a parameterless instance constructor (i.e. a default
        /// constructor): runtime-special, name <c>.ctor</c>, no parameters.
        /// </summary>
        public bool IsDefaultConstructor => method.IsConstructor && method.IsParameterless;

        /// <summary>
        /// Returns whether the method carries the <c>[NoExceptionAttribute]</c> or is an
        /// event remover (event removers are implicitly no-throw).
        /// </summary>
        /// <returns><see langword="true"/> if the method is documented to never throw; otherwise <see langword="false"/>.</returns>
        public bool IsNoExcept => method.IsRemover || method.HasWindowsFoundationMetadataAttribute(NoExceptionAttribute);

        /// <summary>
        /// Returns whether the method is the special <c>Invoke</c> method (used for delegates).
        /// </summary>
        public bool IsInvoke => method.IsSpecialName && method.Name is { } name && name.AsSpan().SequenceEqual("Invoke"u8);

        /// <summary>
        /// Returns the method's raw metadata name, falling back to <see cref="string.Empty"/> when
        /// the metadata name is <see langword="null"/>. Convenience for the
        /// <c>method.Name?.Value ?? string.Empty</c> pattern that appears at many sites.
        /// </summary>
        public string GetRawName()
        {
            return method.Name?.Value ?? string.Empty;
        }
    }
}
