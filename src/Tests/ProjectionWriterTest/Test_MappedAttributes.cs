// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Tests for the Windows Runtime metadata attributes the projection writer replaces with a .NET
/// counterpart, rather than carrying over as-is.
/// </summary>
/// <remarks>
/// <para>
/// See <c>docs/attribute-projections.md</c> for the full mapping table. These tests pin down the
/// <c>[Experimental]</c> mapping, which is the only one that removes a metadata attribute type from
/// the projection outright.
/// </para>
/// <para>
/// They assert the shape of the projection, never the contents of the Windows SDK: the input metadata
/// is whichever SDK is installed on the machine, so which APIs happen to be experimental is not stable
/// across agents. The emitted <c>[Experimental]</c> attribute itself is covered where it is
/// deterministic instead — <c>TestComponentCSharp</c> declares an <c>[experimental]</c> runtime class,
/// and the <c>#pragma warning disable CSWINRT3005</c> around its use in <c>UnitTest</c> only compiles
/// while the writer emits exactly that diagnostic id.
/// </para>
/// </remarks>
[TestClass]
public class Test_MappedAttributes
{
    /// <summary>
    /// The Windows Runtime <c>[Experimental]</c> attribute type is custom-mapped to the .NET one, so it
    /// is not projected itself, and no application of it survives under its Windows Runtime name.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WindowsRuntimeExperimentalAttribute_IsNotProjected(bool referenceProjection)
    {
        int typeCount = ProjectionWriterRunner.CountAttributeText(referenceProjection, "class ExperimentalAttribute");
        int applicationCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection, "Windows.Foundation.Metadata.Experimental");

        Assert.AreEqual(0, typeCount, "The Windows Runtime 'ExperimentalAttribute' type should not be projected.");
        Assert.AreEqual(0, applicationCount, "No application of the Windows Runtime '[Experimental]' attribute should survive.");
    }

    /// <summary>
    /// The metadata attribute types that are <em>not</em> custom-mapped are still projected.
    /// </summary>
    /// <remarks>
    /// This is what keeps <see cref="WindowsRuntimeExperimentalAttribute_IsNotProjected"/> honest: both
    /// types live in <c>Windows.Foundation.Metadata</c> and are emitted by the same code path, so if
    /// that namespace ever stopped being projected at all, the assertion there would pass for the wrong
    /// reason. <c>[Deprecated]</c> in particular has to stay projected, as component authors apply it
    /// directly (see <c>docs/attribute-projections.md</c>).
    /// </remarks>
    [TestMethod]
    [DataRow("class DeprecatedAttribute")]
    [DataRow("class OverloadAttribute")]
    [DataRow("class VersionAttribute")]
    public void UnmappedMetadataAttribute_IsStillProjected(string typeDeclaration)
    {
        int typeCount = ProjectionWriterRunner.CountAttributeText(referenceProjection: true, typeDeclaration);

        Assert.AreNotEqual(0, typeCount, $"'{typeDeclaration}' should still be projected.");
    }
}
