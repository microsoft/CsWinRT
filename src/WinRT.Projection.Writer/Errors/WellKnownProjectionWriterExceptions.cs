// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionWriter.Models;

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
    /// A switch over the well-known <c>TypeKind</c> enum encountered an unrecognized member.
    /// </summary>
    public static WellKnownProjectionWriterException UnknownTypeKind(TypeKind kind)
    {
        return Exception(5003, $"Unknown type kind: '{kind}'.");
    }

    /// <summary>
    /// A type signature passed to <c>TypeSemanticsFactory</c> could not be classified.
    /// </summary>
    public static WellKnownProjectionWriterException UnsupportedTypeSignature(string signature)
    {
        return Exception(5004, $"Unsupported signature: '{signature}'.");
    }

    /// <summary>
    /// A corlib element type passed to <c>TypeSemanticsFactory.GetFundamental</c> is not in the supported set.
    /// </summary>
    public static WellKnownProjectionWriterException UnsupportedCorLibElementType(object elementType)
    {
        return Exception(5005, $"Unsupported corlib element type: {elementType}.");
    }

    /// <summary>
    /// A fundamental type passed to <c>IidExpressionGenerator</c> is not in the supported set.
    /// </summary>
    public static WellKnownProjectionWriterException UnknownFundamentalType()
    {
        return Exception(5006, "Unknown fundamental type.");
    }

    /// <summary>
    /// A type referenced from <c>IidExpressionGenerator</c> is missing the expected <c>[Guid]</c> attribute or has malformed <c>Guid</c> fields.
    /// </summary>
    public static WellKnownProjectionWriterException MissingGuidAttribute(string typeName)
    {
        return Exception(5007, $"Type '{typeName}' is missing a usable [Guid] attribute or has malformed Guid fields.");
    }

    /// <summary>
    /// The orchestrator could not locate the Windows SDK install root in the registry.
    /// </summary>
    public static WellKnownProjectionWriterException WindowsSdkNotFound()
    {
        return Exception(5008, "Could not find the Windows SDK in the registry.");
    }

    /// <summary>
    /// The orchestrator could not read a Windows SDK platform XML file.
    /// </summary>
    public static WellKnownProjectionWriterException CannotReadWindowsSdkXml(string xmlPath)
    {
        return Exception(5009, $"Could not read the Windows SDK's XML at '{xmlPath}'.");
    }

    /// <summary>
    /// An emission helper detected a programming error (e.g. an unexpected null state).
    /// </summary>
    public static WellKnownProjectionWriterException UnreachableEmissionState(string message)
    {
        return Exception(5010, message);
    }

    /// <summary>
    /// An input metadata path passed to the loader does not point at an existing file or directory.
    /// </summary>
    public static WellKnownProjectionWriterException InvalidInputPath(string path)
    {
        return Exception(5011, $"The input metadata path '{path}' does not exist (must be a <c>.winmd</c> file or a directory containing one).");
    }

    /// <summary>
    /// An input <c>.winmd</c> file is malformed or contains an unexpected number of modules.
    /// </summary>
    public static WellKnownProjectionWriterException MalformedWinmd(string path)
    {
        return Exception(5012, $"The input metadata file '{path}' is malformed: expected exactly one module per .winmd file.");
    }

    /// <summary>
    /// The parallel projection work-item loop terminated without enumerating every work item.
    /// </summary>
    public static WellKnownProjectionWriterException WorkItemLoopDidNotComplete()
    {
        return Exception(5013, "The parallel projection work-item loop did not complete; one or more work items were not dispatched.");
    }

    /// <summary>
    /// One of the projection work items dispatched by <c>Run</c> failed with an unexpected (non well-known) exception.
    /// </summary>
    public static WellKnownProjectionWriterException WorkItemLoopError(Exception exception)
    {
        return Exception(5014, "The parallel projection work-item loop reported one or more failures.", exception);
    }

    /// <summary>
    /// Emission of a single namespace's projection file failed.
    /// </summary>
    public static WellKnownProjectionWriterException NamespaceEmissionFailed(string namespaceName, Exception exception)
    {
        return Exception(5015, $"Failed to emit the projection file for namespace '{namespaceName}'.", exception);
    }

    /// <summary>
    /// Emission of the global <c>GeneratedInterfaceIIDs.cs</c> file failed.
    /// </summary>
    public static WellKnownProjectionWriterException GeneratedInterfaceIidsEmissionFailed(Exception exception)
    {
        return Exception(5016, "Failed to emit the global 'GeneratedInterfaceIIDs.cs' file.", exception);
    }

    /// <summary>
    /// Emission of the component-module activation-factory aggregator <c>WinRT_Module.cs</c> failed.
    /// </summary>
    public static WellKnownProjectionWriterException ComponentModuleEmissionFailed(Exception exception)
    {
        return Exception(5017, "Failed to emit the component-mode 'WinRT_Module.cs' activation-factory file.", exception);
    }

    /// <summary>
    /// The input projection options do not include any <c>.winmd</c> path.
    /// </summary>
    public static WellKnownProjectionWriterException MissingInputPaths()
    {
        return Exception(5018, "At least one input metadata path must be provided.");
    }

    /// <summary>
    /// The input projection options do not include an output folder.
    /// </summary>
    public static WellKnownProjectionWriterException MissingOutputFolder()
    {
        return Exception(5019, "An output folder must be provided.");
    }

    /// <summary>
    /// <c>Settings.MakeReadOnly</c> was called more than once on the same <c>Settings</c> instance.
    /// </summary>
    public static WellKnownProjectionWriterException SettingsAlreadyReadOnly()
    {
        return Exception(5020, "Settings have already been finalized via MakeReadOnly and cannot be finalized again.");
    }

    /// <summary>
    /// A derived <c>Settings</c> property was accessed before <c>MakeReadOnly</c> had been called.
    /// </summary>
    public static WellKnownProjectionWriterException SettingsNotReadOnly()
    {
        return Exception(5021, "Settings have not been finalized via MakeReadOnly; the derived state is not yet available.");
    }

    private static WellKnownProjectionWriterException Exception(int id, string message, Exception? innerException = null)
    {
        return new WellKnownProjectionWriterException($"{ErrorPrefix}{id:0000}", message, innerException);
    }
}
