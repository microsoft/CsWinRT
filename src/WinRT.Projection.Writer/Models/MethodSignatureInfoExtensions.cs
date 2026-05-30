// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Category-filtered iteration helpers for <see cref="MethodSignatureInfo"/>. Each
/// iterator pre-computes the <see cref="ParameterCategory"/> for the requested filter and
/// yields the (index, parameter) tuples. The index must remain available because several
/// callers use it to control comma emission or fixed-block nesting.
/// </summary>
internal static class MethodSignatureInfoExtensions
{
    extension(MethodSignatureInfo sig)
    {
        /// <summary>
        /// Yields each (index, parameter, resolved category) triple for the method's parameters
        /// in declaration order.
        /// </summary>
        public IEnumerable<(int Index, ParameterInfo Parameter, ParameterCategory Category)> EnumerateWithCategory()
        {
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                yield return (i, p, ParameterCategoryResolver.Resolve(p));
            }
        }

        /// <summary>
        /// Yields each (index, parameter) pair whose resolved category equals
        /// <paramref name="category"/>.
        /// </summary>
        public IEnumerable<(int Index, ParameterInfo Parameter)> ParametersByCategory(ParameterCategory category)
        {
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];

                if (ParameterCategoryResolver.Resolve(p) == category)
                {
                    yield return (i, p);
                }
            }
        }
    }
}
