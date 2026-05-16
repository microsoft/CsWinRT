// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="IdentifierEscaping.WriteEscapedIdentifier(string)"/>
internal readonly struct WriteEscapedIdentifierCallback(string identifier) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        IdentifierEscaping.WriteEscapedIdentifier(writer, identifier);
    }
}
