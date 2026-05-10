// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// One param: links the parameter definition to its signature type.
/// </summary>
internal sealed record ParameterInfo(Parameter Parameter, TypeSignature Type);
