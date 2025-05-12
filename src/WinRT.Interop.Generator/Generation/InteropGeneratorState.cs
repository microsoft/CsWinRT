// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Global state tracking type for <see cref="InteropGenerator"/>.
/// </summary>
internal sealed class InteropGeneratorState
{
    public readonly SortedDictionary<string, string> _typeHierarchyEntries = [];

    public void TrackTypeHierarchyEntry(string runtimeClassName, string baseRuntimeClassName)
    {
        _typeHierarchyEntries.Add(runtimeClassName, baseRuntimeClassName);
    }
}
