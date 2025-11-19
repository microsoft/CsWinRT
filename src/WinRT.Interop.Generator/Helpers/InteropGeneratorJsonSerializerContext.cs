// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A <see cref="JsonSerializerContext"/> for types used in the interop generator.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class InteropGeneratorJsonSerializerContext : JsonSerializerContext;