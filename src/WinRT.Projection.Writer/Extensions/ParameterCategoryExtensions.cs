// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="ParameterCategory"/>.
/// </summary>
internal static class ParameterCategoryExtensions
{
    extension(ParameterCategory cat)
    {
        /// <summary>
        /// Returns whether <paramref name="cat"/> is an input-side array category
        /// (<see cref="ParameterCategory.PassArray"/> or <see cref="ParameterCategory.FillArray"/>).
        /// </summary>
        public bool IsArrayInput()
            => cat is ParameterCategory.PassArray or ParameterCategory.FillArray;

        /// <summary>
        /// Returns whether <paramref name="cat"/> is any of the array-shaped categories
        /// (<see cref="ParameterCategory.PassArray"/>, <see cref="ParameterCategory.FillArray"/>,
        /// or <see cref="ParameterCategory.ReceiveArray"/>).
        /// </summary>
        public bool IsAnyArray()
            => cat is ParameterCategory.PassArray
                or ParameterCategory.FillArray
                or ParameterCategory.ReceiveArray;
    }
}
