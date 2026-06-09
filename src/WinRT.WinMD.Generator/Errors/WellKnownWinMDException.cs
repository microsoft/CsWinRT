// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.GeneratorCli.Errors;

namespace WindowsRuntime.WinMDGenerator.Errors;

/// <summary>
/// A well known exception for the WinMD generator.
/// </summary>
/// <inheritdoc cref="WellKnownGeneratorException(string, string, Exception?)"/>
internal sealed class WellKnownWinMDException(string id, string message, Exception? innerException)
    : WellKnownGeneratorException(id, message, innerException);
