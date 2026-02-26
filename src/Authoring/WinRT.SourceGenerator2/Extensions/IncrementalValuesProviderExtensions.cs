// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extensions for <see cref="IncrementalValuesProvider{TValues}"/>.
/// </summary>
internal static class IncrementalValuesProviderExtensions
{
    /// <summary>
    /// Skips all <see langword="null"/> values from a given provider.
    /// </summary>
    /// <typeparam name="T">The type of values being produced.</typeparam>
    /// <param name="provider">The input <see cref="IncrementalValuesProvider{TValues}"/> instance.</param>
    /// <returns>The resulting <see cref="IncrementalValuesProvider{TValues}"/> instance.</returns>
    public static IncrementalValuesProvider<T> SkipNullValues<T>(this IncrementalValuesProvider<T?> provider)
        where T : class
    {
        return provider.Where(static value => value is not null)!;
    }
}
