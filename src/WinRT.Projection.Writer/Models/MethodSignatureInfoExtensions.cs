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
                yield return (i, p, ParameterCategoryResolver.GetParamCategory(p));
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

                if (ParameterCategoryResolver.GetParamCategory(p) == category)
                {
                    yield return (i, p);
                }
            }
        }

        /// <summary>
        /// Yields each (index, parameter, category) triple whose resolved category is one of
        /// the array shapes (<see cref="ParameterCategory.PassArray"/>,
        /// <see cref="ParameterCategory.FillArray"/>, <see cref="ParameterCategory.ReceiveArray"/>).
        /// </summary>
        public IEnumerable<(int Index, ParameterInfo Parameter, ParameterCategory Category)> ArrayParameters()
        {
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                ParameterInfo p = sig.Parameters[i];
                ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);

                if (cat.IsAnyArray())
                {
                    yield return (i, p, cat);
                }
            }
        }

        /// <summary>
        /// Returns whether any parameter has the given <paramref name="category"/>.
        /// </summary>
        public bool HasParameterOfCategory(ParameterCategory category)
        {
            for (int i = 0; i < sig.Parameters.Count; i++)
            {
                if (ParameterCategoryResolver.GetParamCategory(sig.Parameters[i]) == category)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
