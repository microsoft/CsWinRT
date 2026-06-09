// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// A well known exception for the projection generator.
/// </summary>
/// <inheritdoc cref="WellKnownGeneratorException(string, string, Exception?)"/>
internal sealed class WellKnownProjectionGeneratorException(string id, string message, Exception? innerException)
    : WellKnownGeneratorException(id, message, innerException);
