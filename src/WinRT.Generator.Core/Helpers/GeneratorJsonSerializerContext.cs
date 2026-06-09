// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WindowsRuntime.Generator.Helpers;

/// <summary>
/// A <see cref="JsonSerializerContext"/> for types used across the CsWinRT CLI generators.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class GeneratorJsonSerializerContext : JsonSerializerContext;
