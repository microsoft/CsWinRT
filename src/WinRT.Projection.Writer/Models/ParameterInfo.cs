// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Information about a single method parameter, pairing the metadata <see cref="Parameter"/>
/// definition (which carries its name, in/out flags, default value, and custom attributes)
/// with the resolved <see cref="TypeSignature"/> after any active generic-context substitutions.
/// </summary>
/// <param name="Parameter">The metadata parameter definition.</param>
/// <param name="Type">The signature type (with generic substitutions already applied).</param>
internal sealed record ParameterInfo(Parameter Parameter, TypeSignature Type);
