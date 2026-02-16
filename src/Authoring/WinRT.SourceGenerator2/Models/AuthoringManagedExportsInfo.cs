// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// Options for <see cref="AuthoringExportTypesGenerator"/> specific to managed exports.
/// </summary>
/// <param name="AssemblyName">The assembly name for the current assembly.</param>
/// <param name="MergedManagedExportsTypeNames">The names of the merged managed exports types.</param>
/// <param name="Options">The options for the generator.</param>
internal record AuthoringManagedExportsInfo(
    string AssemblyName,
    EquatableArray<string> MergedManagedExportsTypeNames,
    AuthoringExportTypesOptions Options);