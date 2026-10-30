// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Foundation;

namespace UnitTest;

[TestClass]
public class FoundationStructTests
{
    // Use bit patterns for signed zeros and NaN variants, avoiding floating-point data row generation.
    private const uint PositiveZeroBits = 0x00000000;
    private const uint NegativeZeroBits = 0x80000000;
    private const uint PositiveEpsilonBits = 0x00000001;
    private const uint NegativeEpsilonBits = 0x80000001;
    private const uint OneBits = 0x3F800000;
    private const uint NegativeOneBits = 0xBF800000;
    private const uint NegativeTwoBits = 0xC0000000;
    private const uint MaxValueBits = 0x7F7FFFFF;
    private const uint MinValueBits = 0xFF7FFFFF;
    private const uint PositiveInfinityBits = 0x7F800000;
    private const uint NegativeInfinityBits = 0xFF800000;
    private const uint PositiveQuietNaNBits = 0x7FC00000;
    private const uint NegativeQuietNaNBits = 0xFFC00000;
    private const uint PositiveSignalingNaNBits = 0x7F800001;
    private const uint NegativeSignalingNaNBits = 0xFF800001;

    [TestMethod]
    [DataRow(PositiveZeroBits)]
    [DataRow(NegativeZeroBits)]
    [DataRow(PositiveEpsilonBits)]
    [DataRow(OneBits)]
    [DataRow(MaxValueBits)]
    [DataRow(PositiveInfinityBits)]
    [DataRow(PositiveQuietNaNBits)]
    [DataRow(NegativeQuietNaNBits)]
    [DataRow(PositiveSignalingNaNBits)]
    [DataRow(NegativeSignalingNaNBits)]
    public void SizeConstructor_NonNegativeOrNaNDimensions_ArePreserved(uint valueBits)
    {
        float value = BitConverter.UInt32BitsToSingle(valueBits);

        Size size = new(value, value);

        AssertValuePreserved(valueBits, size.Width);
        AssertValuePreserved(valueBits, size.Height);
        Assert.IsFalse(size.IsEmpty);
    }

    [TestMethod]
    [DataRow(PositiveZeroBits)]
    [DataRow(NegativeZeroBits)]
    [DataRow(PositiveEpsilonBits)]
    [DataRow(OneBits)]
    [DataRow(MaxValueBits)]
    [DataRow(PositiveInfinityBits)]
    [DataRow(PositiveQuietNaNBits)]
    [DataRow(NegativeQuietNaNBits)]
    [DataRow(PositiveSignalingNaNBits)]
    [DataRow(NegativeSignalingNaNBits)]
    public void RectConstructor_NonNegativeOrNaNDimensions_ArePreserved(uint valueBits)
    {
        float value = BitConverter.UInt32BitsToSingle(valueBits);

        Rect rect = new(-1, -2, value, value);

        Assert.AreEqual(-1f, rect.X);
        Assert.AreEqual(-2f, rect.Y);
        AssertValuePreserved(valueBits, rect.Width);
        AssertValuePreserved(valueBits, rect.Height);
        Assert.IsFalse(rect.IsEmpty);
    }

    [TestMethod]
    [DataRow(PositiveZeroBits)]
    [DataRow(NegativeZeroBits)]
    [DataRow(PositiveEpsilonBits)]
    [DataRow(NegativeEpsilonBits)]
    [DataRow(OneBits)]
    [DataRow(NegativeOneBits)]
    [DataRow(MaxValueBits)]
    [DataRow(MinValueBits)]
    [DataRow(PositiveInfinityBits)]
    [DataRow(NegativeInfinityBits)]
    [DataRow(PositiveQuietNaNBits)]
    [DataRow(NegativeQuietNaNBits)]
    [DataRow(PositiveSignalingNaNBits)]
    [DataRow(NegativeSignalingNaNBits)]
    public void PointAndRectConstructors_Coordinates_ArePreserved(uint valueBits)
    {
        float value = BitConverter.UInt32BitsToSingle(valueBits);

        Point point = new(value, value);
        Rect rect = new(value, value, 1, 2);

        AssertValuePreserved(valueBits, point.X);
        AssertValuePreserved(valueBits, point.Y);
        AssertValuePreserved(valueBits, rect.X);
        AssertValuePreserved(valueBits, rect.Y);
    }

    [TestMethod]
    [DataRow(NegativeEpsilonBits, OneBits, "width")]
    [DataRow(OneBits, NegativeEpsilonBits, "height")]
    [DataRow(NegativeOneBits, OneBits, "width")]
    [DataRow(OneBits, NegativeOneBits, "height")]
    [DataRow(MinValueBits, OneBits, "width")]
    [DataRow(OneBits, MinValueBits, "height")]
    [DataRow(NegativeInfinityBits, OneBits, "width")]
    [DataRow(OneBits, NegativeInfinityBits, "height")]
    [DataRow(NegativeOneBits, NegativeTwoBits, "width")]
    [DataRow(NegativeQuietNaNBits, NegativeOneBits, "height")]
    [DataRow(NegativeOneBits, NegativeQuietNaNBits, "width")]
    public void SizeConstructor_NegativeDimension_Throws(uint widthBits, uint heightBits, string paramName)
    {
        float width = BitConverter.UInt32BitsToSingle(widthBits);
        float height = BitConverter.UInt32BitsToSingle(heightBits);

        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Size(width, height));

        Assert.AreEqual(paramName, exception.ParamName);
        Assert.AreEqual(paramName == "width" ? width : height, exception.ActualValue);
    }

    [TestMethod]
    [DataRow(NegativeEpsilonBits, OneBits, "width")]
    [DataRow(OneBits, NegativeEpsilonBits, "height")]
    [DataRow(NegativeOneBits, OneBits, "width")]
    [DataRow(OneBits, NegativeOneBits, "height")]
    [DataRow(MinValueBits, OneBits, "width")]
    [DataRow(OneBits, MinValueBits, "height")]
    [DataRow(NegativeInfinityBits, OneBits, "width")]
    [DataRow(OneBits, NegativeInfinityBits, "height")]
    [DataRow(NegativeOneBits, NegativeTwoBits, "width")]
    [DataRow(NegativeQuietNaNBits, NegativeOneBits, "height")]
    [DataRow(NegativeOneBits, NegativeQuietNaNBits, "width")]
    public void RectConstructor_NegativeDimension_Throws(uint widthBits, uint heightBits, string paramName)
    {
        float width = BitConverter.UInt32BitsToSingle(widthBits);
        float height = BitConverter.UInt32BitsToSingle(heightBits);

        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rect(0, 0, width, height));

        Assert.AreEqual(paramName, exception.ParamName);
        Assert.AreEqual(paramName == "width" ? width : height, exception.ActualValue);
    }

    [TestMethod]
    public void RectConstructor_FromLocationAndSize_PreservesNaN()
    {
        Rect rect = new(new Point(float.NaN, float.NaN), new Size(float.NaN, float.NaN));

        Assert.IsTrue(float.IsNaN(rect.X));
        Assert.IsTrue(float.IsNaN(rect.Y));
        Assert.IsTrue(float.IsNaN(rect.Width));
        Assert.IsTrue(float.IsNaN(rect.Height));
        Assert.IsFalse(rect.IsEmpty);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RectConstructor_FromPoints_AcceptsNaN(bool firstPointIsNaN)
    {
        Point nanPoint = new(float.NaN, float.NaN);
        Rect rect = firstPointIsNaN ? new Rect(nanPoint, default(Point)) : new Rect(default(Point), nanPoint);

        Assert.IsTrue(float.IsNaN(rect.X));
        Assert.IsTrue(float.IsNaN(rect.Y));
        Assert.IsTrue(float.IsNaN(rect.Width));
        Assert.IsTrue(float.IsNaN(rect.Height));
        Assert.IsFalse(rect.IsEmpty);
    }

    [TestMethod]
    public void RectConstructor_FromEmptySize_RemainsEmpty()
    {
        Rect rect = new(new Point(1, 2), Size.Empty);

        Assert.IsTrue(rect.IsEmpty);
        Assert.AreEqual(Rect.Empty, rect);
    }

    private static void AssertValuePreserved(uint expectedBits, float actual)
    {
        // Floating-point loads and returns can quiet signaling NaNs, notably on x86.
        if (float.IsNaN(BitConverter.UInt32BitsToSingle(expectedBits)))
        {
            Assert.IsTrue(float.IsNaN(actual));
        }
        else
        {
            Assert.AreEqual(expectedBits, BitConverter.SingleToUInt32Bits(actual));
        }
    }
}
