// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace ForwardedTypeIdentitiesNetStandard;

public sealed class DisposableProbe : IDisposable
{
    public int DisposeCount { get; private set; }

    public void Dispose()
    {
        DisposeCount++;
    }
}
