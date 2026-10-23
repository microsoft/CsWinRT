// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// The source metadata shared by a projected type's contract and IID registrations.
/// </summary>
/// <param name="Type">The original Windows Runtime metadata type.</param>
/// <param name="SourceStem">The source Windows Runtime metadata module's filename stem.</param>
internal readonly record struct WindowsRuntimeMetadataTypeEntry(TypeDefinition Type, string SourceStem);
