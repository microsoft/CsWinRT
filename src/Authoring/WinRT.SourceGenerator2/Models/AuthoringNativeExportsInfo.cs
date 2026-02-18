// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// Options for <see cref="AuthoringExportTypesGenerator"/> specific to native exports.
/// </summary>
/// <param name="AssemblyName">The assembly name for the current assembly.</param>
/// <param name="Options">The options for the generator.</param>
internal record AuthoringNativeExportsInfo(string AssemblyName, AuthoringExportTypesOptions Options);