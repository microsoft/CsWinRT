// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// A reference assembly loaded as part of a <see cref="ReferenceAssemblySet"/>.
/// </summary>
/// <param name="Path">The path to the reference assembly.</param>
/// <param name="Module">The module loaded from <paramref name="Path"/>.</param>
/// <param name="Name">The simple name of the assembly, if it has one.</param>
/// <param name="IsComponent">Whether the assembly is annotated with <c>[WindowsRuntimeComponentAssembly]</c>.</param>
internal readonly record struct ReferenceAssembly(string Path, ModuleDefinition Module, string? Name, bool IsComponent);
