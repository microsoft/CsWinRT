// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Source-generated metadata for AOT-compatible interop log serialization.
/// </summary>
[JsonSerializable(typeof(InteropGeneratorLog))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
internal sealed partial class InteropGeneratorLogJsonSerializerContext : JsonSerializerContext;
