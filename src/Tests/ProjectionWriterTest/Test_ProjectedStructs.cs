// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Tests for how the projection writer projects the members of Windows Runtime struct types.
/// </summary>
/// <remarks>
/// Windows Runtime structs are plain data: their members are fields in metadata, and they must be
/// projected as C# fields. Projecting them as properties breaks authoring (the WinMD generator only
/// maps public instance fields back to Windows Runtime struct fields, so an authored struct using
/// properties would produce an empty struct in metadata), and it prevents callers from taking a
/// reference to a member.
/// </remarks>
[TestClass]
public class Test_ProjectedStructs
{
    /// <summary>
    /// Every field of a projected struct is emitted as a public C# field.
    /// </summary>
    /// <remarks>
    /// <c>Windows.Foundation.Numerics.Rational</c> is the only non-mapped struct in the projected
    /// namespace, so it is the anchor for both assertions.
    /// </remarks>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void StructFields_AreProjectedAsFields(bool referenceProjection)
    {
        string sources = ProjectionWriterRunner.GetSources(referenceProjection);

        StringAssert.Contains(sources, "public uint Numerator;", "'Rational.Numerator' should be projected as a field.");
        StringAssert.Contains(sources, "public uint Denominator;", "'Rational.Denominator' should be projected as a field.");
    }

    /// <summary>
    /// No projected struct member is emitted as an auto-property.
    /// </summary>
    /// <remarks>
    /// Guards against a regression to the <c>{ readonly get; set; }</c> form that projected struct
    /// members used to be emitted with.
    /// </remarks>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void StructFields_AreNotProjectedAsProperties(bool referenceProjection)
    {
        string sources = ProjectionWriterRunner.GetSources(referenceProjection);

        Assert.IsFalse(sources.Contains("readonly get; set;"), "Struct members should be projected as fields, not as auto-properties.");
    }
}
