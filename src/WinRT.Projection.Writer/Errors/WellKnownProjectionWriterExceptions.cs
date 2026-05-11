// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;

namespace WindowsRuntime.ProjectionWriter.Errors;

/// <summary>
/// Well-known exceptions produced by the projection writer.
/// </summary>
internal static class WellKnownProjectionWriterExceptions
{
    /// <summary>
    /// The prefix for all error IDs produced by the writer. Shared with
    /// <c>WinRT.Projection.Generator</c>; the writer uses the reserved 5000+ ID range so
    /// writer and host-generator error IDs never collide.
    /// </summary>
    public const string ErrorPrefix = "CSWINRTPROJECTIONGEN";

    /// <summary>
    /// Raised when an internal invariant about a referenced type fails (e.g. an
    /// expected type definition cannot be resolved). Replaces ad-hoc
    /// <see cref="InvalidOperationException"/> throws scattered through the writer.
    /// </summary>
    /// <param name="message">The message describing the failed invariant.</param>
    /// <returns>The constructed exception (callers are expected to <c>throw</c> the result).</returns>
    public static WellKnownProjectionWriterException InternalInvariantFailed(string message)
    {
        return Exception(5001, message);
    }

    /// <summary>
    /// Raised when a metadata type referenced from an emission helper cannot be resolved.
    /// </summary>
    /// <param name="typeName">The fully-qualified name of the unresolved type.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException CannotResolveType(string typeName)
    {
        return Exception(5002, $"The type '{typeName}' could not be resolved against the metadata cache.");
    }

    /// <summary>
    /// Raised when a switch over the well-known <c>TypeCategory</c> enum encounters an unrecognized member.
    /// </summary>
    /// <param name="category">The unknown category value.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException UnknownTypeCategory(object category)
    {
        return Exception(5003, $"Unknown TypeCategory: {category}.");
    }

    /// <summary>
    /// Raised when a type signature passed to <c>TypeSemanticsFactory</c> cannot be classified.
    /// </summary>
    /// <param name="signature">The unsupported signature (rendered via <c>ToString()</c>).</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException UnsupportedTypeSignature(string signature)
    {
        return Exception(5004, $"Unsupported signature: '{signature}'.");
    }

    /// <summary>
    /// Raised when a corlib element type passed to <c>TypeSemanticsFactory.GetFundamental</c> is not in the
    /// supported set.
    /// </summary>
    /// <param name="elementType">The unsupported element type.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException UnsupportedCorLibElementType(object elementType)
    {
        return Exception(5005, $"Unsupported corlib element type: {elementType}.");
    }

    /// <summary>
    /// Raised when a fundamental type passed to <c>IidExpressionGenerator</c> is not in the supported set.
    /// </summary>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException UnknownFundamentalType()
    {
        return Exception(5006, "Unknown fundamental type.");
    }

    /// <summary>
    /// Raised when a type referenced from <c>IidExpressionGenerator</c> is missing the expected
    /// <c>[Guid]</c> attribute or has malformed <c>Guid</c> fields.
    /// </summary>
    /// <param name="typeName">The fully-qualified type name that lacks usable GUID metadata.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException MissingGuidAttribute(string typeName)
    {
        return Exception(5007, $"Type '{typeName}' is missing a usable [Guid] attribute or has malformed Guid fields.");
    }

    /// <summary>
    /// Raised when the orchestrator cannot locate the Windows SDK install root in the registry.
    /// </summary>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException WindowsSdkNotFound()
    {
        return Exception(5008, "Could not find the Windows SDK in the registry.");
    }

    /// <summary>
    /// Raised when the orchestrator cannot read a Windows SDK platform XML file.
    /// </summary>
    /// <param name="xmlPath">The path of the XML file that could not be read.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException CannotReadWindowsSdkXml(string xmlPath)
    {
        return Exception(5009, $"Could not read the Windows SDK's XML at '{xmlPath}'.");
    }

    /// <summary>
    /// Raised when an emission helper detects a programming error (e.g. an unexpected null state)
    /// that should never occur at runtime.
    /// </summary>
    /// <param name="message">The message describing the unreachable state.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException UnreachableEmissionState(string message)
    {
        return Exception(5010, message);
    }

    /// <summary>
    /// Raised when an input metadata path passed to the loader does not point at an existing
    /// file or directory.
    /// </summary>
    /// <param name="path">The path that could not be resolved.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException InvalidInputPath(string path)
    {
        return Exception(5011, $"The input metadata path '{path}' does not exist (must be a <c>.winmd</c> file or a directory containing one).");
    }

    /// <summary>
    /// Raised when an input <c>.winmd</c> file is malformed or contains an unexpected number of
    /// modules (every <c>.winmd</c> must contain exactly one module).
    /// </summary>
    /// <param name="path">The malformed input path.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException MalformedWinmd(string path)
    {
        return Exception(5012, $"The input metadata file '{path}' is malformed: expected exactly one module per .winmd file.");
    }

    /// <summary>
    /// Raised when the parallel work-item loop terminates without enumerating every work item
    /// (typically indicates an unhandled control-flow signal coming back from a worker).
    /// </summary>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException WorkItemLoopDidNotComplete()
    {
        return Exception(5013, "The parallel projection work-item loop did not complete; one or more work items were not dispatched.");
    }

    /// <summary>
    /// Raised when one of the projection work items dispatched by <c>Run</c> failed with an
    /// unexpected (non well-known) exception. Mirrors
    /// <c>WellKnownInteropExceptions.LoadAndDiscoverModulesLoopError</c>.
    /// </summary>
    /// <param name="exception">The first inner exception extracted from the <see cref="AggregateException"/>.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException WorkItemLoopError(Exception exception)
    {
        return Exception(5014, "The parallel projection work-item loop reported one or more failures.", exception);
    }

    /// <summary>
    /// Raised when emission of a single namespace's projection file fails. Used inside
    /// <c>NamespaceWorkItem.Execute</c> via <see cref="WellKnownProjectionWriterException.ThrowOrAttach"/>
    /// so the original failure surfaces with the namespace context attached.
    /// </summary>
    /// <param name="namespaceName">The namespace whose projection file was being emitted.</param>
    /// <param name="exception">The inner exception that caused the failure.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException NamespaceEmissionFailed(string namespaceName, Exception exception)
    {
        return Exception(5015, $"Failed to emit the projection file for namespace '{namespaceName}'.", exception);
    }

    /// <summary>
    /// Raised when emission of the global <c>GeneratedInterfaceIIDs.cs</c> file fails. Used
    /// inside <c>IidsWorkItem.Execute</c> via <see cref="WellKnownProjectionWriterException.ThrowOrAttach"/>.
    /// </summary>
    /// <param name="exception">The inner exception that caused the failure.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException GeneratedInterfaceIidsEmissionFailed(Exception exception)
    {
        return Exception(5016, "Failed to emit the global 'GeneratedInterfaceIIDs.cs' file.", exception);
    }

    /// <summary>
    /// Raised when emission of the component-module activation-factory aggregator
    /// <c>WinRT_Module.cs</c> fails. Used inside <c>ComponentModuleWorkItem.Execute</c> via
    /// <see cref="WellKnownProjectionWriterException.ThrowOrAttach"/>.
    /// </summary>
    /// <param name="exception">The inner exception that caused the failure.</param>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException ComponentModuleEmissionFailed(Exception exception)
    {
        return Exception(5017, "Failed to emit the component-mode 'WinRT_Module.cs' activation-factory file.", exception);
    }

    /// <summary>
    /// Raised when the input projection options do not include any <c>.winmd</c> path. Used by
    /// <see cref="ProjectionWriter.Run"/> as a guard before any generation work.
    /// </summary>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException MissingInputPaths()
    {
        return Exception(5018, "At least one input metadata path must be provided.");
    }

    /// <summary>
    /// Raised when the input projection options do not include an output folder. Used by
    /// <see cref="ProjectionWriter.Run"/> as a guard before any generation work.
    /// </summary>
    /// <returns>The constructed exception.</returns>
    public static WellKnownProjectionWriterException MissingOutputFolder()
    {
        return Exception(5019, "An output folder must be provided.");
    }

    private static WellKnownProjectionWriterException Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownProjectionWriterException(
            ErrorPrefix + id.ToString("D4", CultureInfo.InvariantCulture),
            message,
            innerException);
    }
}
