// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using WindowsRuntime.InteropGenerator.Generation;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_MetadataCollectionOrdering
{
    [TestMethod]
    public void HeterogeneousOrderings_PreserveSourcesUntilApply()
    {
        List<int> numbers = [3, 1, 2];
        List<string> names = ["C", "A", "B"];
        MetadataCollectionOrdering[] orderings =
        [
            MetadataCollectionOrdering.Create(numbers, static values => values.Order()),
            MetadataCollectionOrdering.Create(names, static values => values.Order(StringComparer.Ordinal))
        ];

        foreach (MetadataCollectionOrdering ordering in orderings)
        {
            ordering.Compute();
        }

        CollectionAssert.AreEqual(new[] { 3, 1, 2 }, numbers);
        CollectionAssert.AreEqual(new[] { "C", "A", "B" }, names);

        foreach (MetadataCollectionOrdering ordering in orderings)
        {
            ordering.Apply();
        }

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, numbers);
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, names);
    }

    [TestMethod]
    public void ApplyWithoutSnapshot_IsRejectedBeforeChangingSource()
    {
        List<int> values = [2, 1];
        MetadataCollectionOrdering ordering = MetadataCollectionOrdering.Create(values, static source => source.Order());

        Assert.ThrowsExactly<InvalidOperationException>(ordering.Apply);
        CollectionAssert.AreEqual(new[] { 2, 1 }, values);

        ordering.Compute();
        ordering.Apply();

        Assert.ThrowsExactly<InvalidOperationException>(ordering.Apply);
        CollectionAssert.AreEqual(new[] { 1, 2 }, values);
    }

    [TestMethod]
    public void FailedCompute_PreservesSourceAndDoesNotPublishSnapshot()
    {
        List<int> values = [2, 1];
        InvalidOperationException expected = new("ordering failure");
        MetadataCollectionOrdering ordering = MetadataCollectionOrdering.Create(values, _ => throw expected);

        InvalidOperationException actual = Assert.ThrowsExactly<InvalidOperationException>(ordering.Compute);

        Assert.AreSame(expected, actual);
        Assert.ThrowsExactly<InvalidOperationException>(ordering.Apply);
        CollectionAssert.AreEqual(new[] { 2, 1 }, values);
    }
}
