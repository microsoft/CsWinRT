// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.Generator.Errors;

/// <summary>
/// Routes the Windows metadata expansion errors through the per-tool well-known exception factory.
/// </summary>
/// <remarks>
/// This is implemented only by the generators that expand Windows metadata tokens (i.e. those that use
/// <see cref="Helpers.WindowsMetadataExpander"/>). Like <see cref="IGeneratorErrorFactory"/>, it lets the
/// shared expander preserve per-tool exception identity: each factory assigns its own numeric error ID,
/// formats its own message, and constructs its own concrete <see cref="WellKnownGeneratorException"/> subtype.
/// </remarks>
internal interface IWindowsMetadataErrorFactory
{
    /// <summary>The Windows SDK install root could not be located in the registry.</summary>
    static abstract Exception WindowsSdkNotFound();

    /// <summary>A Windows SDK platform XML file could not be read.</summary>
    static abstract Exception CannotReadWindowsSdkXml(string path);
}
