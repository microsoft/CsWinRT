// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CompositionTest;

internal static class Program
{
    /// <summary>
    /// Runs the composition tests, following the exit code convention of the other functional tests
    /// in this repository (100 on success).
    /// </summary>
    private static int Main()
    {
        return CompositionTests.RunAll() ? 100 : 101;
    }
}
