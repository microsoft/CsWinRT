// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="CustomAttribute"/>.
/// </summary>
internal static class CustomAttributeExtensions
{
    extension(CustomAttribute attr)
    {
        /// <summary>
        /// Attempts to read the boxed fixed-positional argument at <paramref name="index"/> from
        /// <see cref="CustomAttribute.Signature"/>, applying the standard <c>uint</c>↔<c>int</c>
        /// coercion when <typeparamref name="T"/> is <see cref="int"/> and the boxed value is
        /// <see cref="uint"/>. Returns <see langword="false"/> when the attribute has no signature,
        /// the index is out of range, or the boxed value cannot be converted to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected argument type (e.g. <see cref="int"/>, <see cref="uint"/>,
        /// <see cref="string"/>).</typeparam>
        /// <param name="index">The zero-based positional argument index.</param>
        /// <param name="value">The argument value when this returns <see langword="true"/>; otherwise default.</param>
        /// <returns><see langword="true"/> on successful conversion; otherwise <see langword="false"/>.</returns>
        public bool TryGetFixedArgument<T>(int index, [NotNullWhen(true)] out T? value)
        {
            if (attr.Signature is null || index < 0 || index >= attr.Signature.FixedArguments.Count)
            {
                value = default;

                return false;
            }

            object? element = attr.Signature.FixedArguments[index].Element;

            if (element is T t)
            {
                value = t;

                return true;
            }

            // Standard coercion: 'uint' -> 'int' (WinMD often stores numeric metadata values as 'uint'
            // in the fixed-args list, but most call sites consume them as 'int').
            if (typeof(T) == typeof(int) && element is uint u)
            {
                value = (T)(object)(int)u;

                return true;
            }

            value = default;

            return false;
        }
    }
}
