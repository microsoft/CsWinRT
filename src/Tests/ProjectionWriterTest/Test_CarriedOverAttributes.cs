// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

/// <summary>
/// Tests for which Windows Runtime metadata attributes the projection writer carries over into each
/// projection mode.
/// </summary>
/// <remarks>
/// Implementation projections are only ever loaded at runtime; user code, compilers, analyzers and
/// metadata tooling all see the reference projection instead. Attribute blobs cannot be trimmed by
/// ILLink or ILC, so any metadata attribute carried into an implementation projection is permanent,
/// unremovable metadata in the shipped application. These tests lock that down in both directions:
/// the metadata attributes must be absent from implementation projections, and still present in
/// reference projections (which also keeps the former assertions from silently becoming vacuous).
/// </remarks>
[TestClass]
public class Test_CarriedOverAttributes
{
    /// <summary>
    /// The Windows Runtime metadata attributes that are carried over into reference projections only.
    /// </summary>
    /// <remarks>
    /// <c>[Overload]</c> comes from both the type-level carry-over in <c>CustomAttributeFactory</c> and
    /// the interface member level one in <c>InterfaceFactory</c>, so it also covers the latter path.
    /// Only attributes that actually occur in the projected namespace are listed here; the remaining
    /// ones are covered by <see cref="ImplementationProjection_CarriesOverNoWindowsRuntimeMetadataAttribute"/>.
    /// </remarks>
    [TestMethod]
    [DataRow("Windows.Foundation.Metadata.Overload")]
    [DataRow("Windows.Foundation.Metadata.ContractVersion")]
    public void MetadataAttribute_IsReferenceProjectionOnly(string attributeName)
    {
        int referenceCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: true, attributeName);
        int implementationCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: false, attributeName);

        Assert.AreNotEqual(0, referenceCount, $"'[{attributeName}]' should be carried over into the reference projection.");
        Assert.AreEqual(0, implementationCount, $"'[{attributeName}]' should not be carried over into the implementation projection.");
    }

    /// <summary>
    /// The synthesized <c>[SupportedOSPlatform]</c> (derived from <c>[ContractVersion]</c>) is also
    /// reference-projection-only.
    /// </summary>
    [TestMethod]
    public void SupportedOSPlatform_IsReferenceProjectionOnly()
    {
        int referenceCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: true, "System.Runtime.Versioning.SupportedOSPlatform");
        int implementationCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: false, "System.Runtime.Versioning.SupportedOSPlatform");

        Assert.AreNotEqual(0, referenceCount, "'[SupportedOSPlatform]' should be synthesized for the reference projection.");
        Assert.AreEqual(0, implementationCount, "'[SupportedOSPlatform]' should not be synthesized for the implementation projection.");
    }

    /// <summary>
    /// No attribute is carried over into an implementation projection from a namespace whose only
    /// consumers are compilers, analyzers or metadata tooling.
    /// </summary>
    /// <remarks>
    /// This is the general guard: rather than listing individual attributes, it asserts that
    /// <em>nothing</em> from <c>Windows.Foundation.Metadata</c> survives. Any future metadata attribute
    /// that starts leaking into implementation projections fails here.
    /// </remarks>
    [TestMethod]
    public void ImplementationProjection_CarriesOverNoWindowsRuntimeMetadataAttribute()
    {
        int referenceCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: true, "Windows.Foundation.Metadata.");
        int implementationCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: false, "Windows.Foundation.Metadata.");

        Assert.AreNotEqual(0, referenceCount, "'Windows.Foundation.Metadata' attributes should be carried over into the reference projection.");
        Assert.AreEqual(0, implementationCount, "No 'Windows.Foundation.Metadata' attribute should be carried over into the implementation projection.");
    }

    /// <summary>
    /// <c>[AttributeUsage]</c> is kept in both projection modes.
    /// </summary>
    /// <remarks>
    /// It is not carried-over Windows Runtime metadata, but the .NET modeling of a projected attribute
    /// type's own usage contract: its <c>AllowMultiple</c> and <c>Inherited</c> arguments drive
    /// <c>Attribute.GetCustomAttributes(inherit: true)</c> semantics at runtime.
    /// </remarks>
    [TestMethod]
    public void AttributeUsage_IsKeptInBothProjections()
    {
        int referenceCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: true, "System.AttributeUsage");
        int implementationCount = ProjectionWriterRunner.CountGlobalAttribute(referenceProjection: false, "System.AttributeUsage");

        Assert.AreNotEqual(0, referenceCount, "'[AttributeUsage]' should be emitted for the reference projection.");
        Assert.AreEqual(referenceCount, implementationCount, "'[AttributeUsage]' should be emitted identically for both projection modes.");
    }

    /// <summary>
    /// The Windows Runtime <c>[allowmultiple]</c> metadata attribute is folded into the
    /// <c>AllowMultiple</c> argument of the projected <c>[AttributeUsage]</c> in both projection modes.
    /// </summary>
    /// <remarks>
    /// <c>[AllowMultiple]</c> is never carried over as an attribute of its own, so it has to be observed
    /// before the carry-over filter runs. This guards that observation in the implementation projection,
    /// where every other metadata attribute is dropped.
    /// </remarks>
    [TestMethod]
    [DataRow("AllowMultiple = true")]
    [DataRow("AllowMultiple = false")]
    public void AllowMultiple_IsFoldedIntoAttributeUsageInBothProjections(string argumentText)
    {
        int referenceCount = ProjectionWriterRunner.CountAttributeText(referenceProjection: true, argumentText);
        int implementationCount = ProjectionWriterRunner.CountAttributeText(referenceProjection: false, argumentText);

        Assert.AreNotEqual(0, referenceCount, $"'{argumentText}' should be emitted for the reference projection.");
        Assert.AreEqual(referenceCount, implementationCount, $"'{argumentText}' should be emitted identically for both projection modes.");
    }

    /// <summary>
    /// The attributes the implementation projection actually needs at runtime (or to compile) are kept.
    /// </summary>
    /// <remarks>
    /// <c>[WindowsRuntimeType]</c> and the other CsWinRT markers drive marshalling, <c>[Guid]</c> is the
    /// runtime IID lookup for delegates, <c>[Flags]</c> backs <c>Enum.ToString</c>, and
    /// <c>[IndexerName]</c> is required for the generated mapped interface stubs to compile.
    /// </remarks>
    [TestMethod]
    [DataRow("[WindowsRuntimeType]")]
    [DataRow("[WindowsRuntimeClassName(")]
    [DataRow("[Guid(")]
    [DataRow("[Flags]")]
    [DataRow("[global::System.Runtime.CompilerServices.IndexerName(")]
    public void RuntimeRelevantAttribute_IsKeptInImplementationProjection(string attributeText)
    {
        int implementationCount = ProjectionWriterRunner.CountAttributeText(referenceProjection: false, attributeText);

        Assert.AreNotEqual(0, implementationCount, $"'{attributeText}' should be emitted for the implementation projection.");
    }
}
