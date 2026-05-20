// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="ParameterCategory"/>.
/// </summary>
internal static class ParameterCategoryExtensions
{
    /// <param name="category">The input parameter category.</param>
    extension(ParameterCategory category)
    {
        /// <summary>
        /// Returns whether the input category is an input-side array category
        /// (<see cref="ParameterCategory.PassArray"/> or <see cref="ParameterCategory.FillArray"/>).
        /// </summary>
        public bool IsArrayInput()
        {
            return category is ParameterCategory.PassArray or ParameterCategory.FillArray;
        }

        /// <summary>
        /// Returns whether the input category is a non-array input parameter that the callee
        /// reads (<see cref="ParameterCategory.In"/> or <see cref="ParameterCategory.Ref"/>) —
        /// i.e. not an array, not an output parameter, and not a receive-array. This is the
        /// natural complement of <see cref="IsArrayInput"/> on the input side of the boundary.
        /// </summary>
        public bool IsScalarInput()
        {
            return category is ParameterCategory.In or ParameterCategory.Ref;
        }

        /// <summary>
        /// Returns whether the input category is any of the array-shaped categories
        /// (<see cref="ParameterCategory.PassArray"/>, <see cref="ParameterCategory.FillArray"/>,
        /// or <see cref="ParameterCategory.ReceiveArray"/>).
        /// </summary>
        public bool IsAnyArray()
        {
            return category is ParameterCategory.PassArray or ParameterCategory.FillArray or ParameterCategory.ReceiveArray;
        }
    }
}
