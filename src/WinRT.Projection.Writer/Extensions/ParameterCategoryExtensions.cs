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
