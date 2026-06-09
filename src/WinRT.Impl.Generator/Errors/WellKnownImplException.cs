// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.GeneratorCli.Errors;

namespace WindowsRuntime.ImplGenerator.Errors;

/// <summary>
/// A well known exception for the impl generator.
/// </summary>
/// <inheritdoc cref="WellKnownGeneratorException(string, string, Exception?)"/>
internal sealed class WellKnownImplException(string id, string message, Exception? innerException)
    : WellKnownGeneratorException(id, message, innerException);
