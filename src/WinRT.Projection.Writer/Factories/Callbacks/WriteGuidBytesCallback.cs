// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories.Callbacks;

/// <see cref="IidExpressionGenerator.WriteGuidBytes(Guid)"/>
internal readonly struct WriteGuidBytesCallback(Guid guid) : IIndentedTextWriterCallback
{
    /// <inheritdoc/>
    public void Write(IndentedTextWriter writer)
    {
        IidExpressionGenerator.WriteGuidBytes(writer, guid);
    }
}
