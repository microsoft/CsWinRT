// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Extension methods for <see cref="MethodDefinition"/>.
/// </summary>
internal static class MethodDefinitionExtensions
{
    extension(MethodDefinition method)
    {
        /// <summary>
        /// Gets whether the method is a property getter/setter or event adder/remover.
        /// </summary>
        public bool IsAccessorMethod => method.IsGetMethod || method.IsSetMethod || method.IsAddMethod || method.IsRemoveMethod;
    }
}
