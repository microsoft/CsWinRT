// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WindowsRuntime.ReferenceProjectionGenerator.Helpers;

/// <summary>
/// A <see cref="JsonSerializerContext"/> for types used in the reference projection generator.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class ReferenceProjectionGeneratorJsonSerializerContext : JsonSerializerContext;
