// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Tests for the Windows Runtime metadata attributes the projection writer replaces with a .NET
/// counterpart, rather than carrying over as-is.
/// </summary>
/// <remarks>
/// See <c>docs/attribute-projections.md</c> for the full mapping table. These tests pin down the
/// <c>[Experimental]</c> mapping, which is the one the Windows SDK exercises.
/// </remarks>
[TestClass]
public class Test_MappedAttributes
{
    /// <summary>
    /// The <c>[Experimental]</c> attribute the projection writer emits, in full.
    /// </summary>
    /// <remarks>
    /// The Windows Runtime attribute carries no arguments, so all of these are synthesized. Asserting
    /// the exact text keeps the diagnostic id, url and message in sync with <c>docs/diagnostics/cswinrt3005.md</c>:
    /// user code suppresses the id, so changing it silently would be a breaking change.
    /// </remarks>
    private const string ExperimentalAttributeText =
        """[global::System.Diagnostics.CodeAnalysis.Experimental("CSWINRT3005", UrlFormat = "https://aka.ms/cswinrt/errors/{0}", Message = "This Windows Runtime API is marked as experimental in its Windows Runtime metadata")]""";

    /// <summary>
    /// The Windows Runtime <c>[Experimental]</c> attribute is projected as the .NET one.
    /// </summary>
    [TestMethod]
    public void Experimental_IsProjectedAsTheDotNetAttribute()
    {
        int referenceCount = ProjectionWriterRunner.CountAttributeText(referenceProjection: true, ExperimentalAttributeText);

        Assert.AreNotEqual(0, referenceCount, "The Windows Runtime '[Experimental]' attribute should be projected as the .NET one.");
    }

    /// <summary>
    /// The projected <c>[Experimental]</c> attribute is reference-projection-only, like every other
    /// carried-over metadata attribute.
    /// </summary>
    /// <remarks>
    /// It is only consumed by compilers and analyzers, which always see the reference projection, and
    /// attribute blobs cannot be trimmed by ILLink or ILC (see <see cref="Test_CarriedOverAttributes"/>).
    /// </remarks>
    [TestMethod]
    public void Experimental_IsReferenceProjectionOnly()
    {
        int implementationCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: false, "System.Diagnostics.CodeAnalysis.Experimental");

        Assert.AreEqual(0, implementationCount, "'[Experimental]' should not be emitted into the implementation projection.");
    }

    /// <summary>
    /// The Windows Runtime <c>[Experimental]</c> attribute type is custom-mapped, so it is not projected
    /// itself, and no application of it survives under its Windows Runtime name.
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
}
