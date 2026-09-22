// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using WindowsRuntime.Xaml;

namespace CustomPropertyProviderLibrary;

[GeneratedCustomPropertyProvider]
public sealed partial class GenericDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> values)
    where TKey : notnull
{
    public TValue this[TKey key] => values[key];

    public int Count => values.Count;
}

[GeneratedCustomPropertyProvider]
public sealed partial class GenericList<T>(IReadOnlyList<T> values)
{
    public T this[int index] => values[index];

    public int Count => values.Count;
}

public static class ProviderExtensions
{
    public static GenericDictionary<TKey, TValue> AsBindable<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> values)
        where TKey : notnull => new(values);

    public static GenericList<T> AsBindable<T>(this IReadOnlyList<T> values) => new(values);
}
