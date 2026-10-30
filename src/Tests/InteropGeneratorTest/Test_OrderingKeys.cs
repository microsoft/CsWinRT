// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using WindowsRuntime.InteropGenerator.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_OrderingKeys
{
    [TestMethod]
    public void ByteKeys_PreserveHexOrdinalOrderForPrefixesAndEveryByte()
    {
        byte[][] values =
        [
            [],
            [0],
            [0, 0],
            [0, 255],
            [1],
            [15, 0],
            [16],
            [127],
            [128],
            [255],
            [255, 0],
            [255, 255],
            [255, 255]
        ];

        byte[][] exhaustive = [
            .. values,
            .. Enumerable.Range(0, 256).Select(static value => new byte[] { (byte)value }),
            .. Enumerable.Range(0, 256).Select(static value => new byte[] { 42, (byte)value })];

        foreach (byte[] first in exhaustive)
        {
            foreach (byte[] second in exhaustive)
            {
                Assert.AreEqual(
                    Math.Sign(StringComparer.Ordinal.Compare(Convert.ToHexString(first), Convert.ToHexString(second))),
                    Math.Sign(ByteSequenceComparer.Instance.Compare(first, second)),
                    $"Byte ordering changed for '{Convert.ToHexString(first)}' and '{Convert.ToHexString(second)}'.");
            }
        }
    }
}
