// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Win32;

namespace Microsoft.NET.Build.Tasks;

/// <summary>Chooses GC startup settings without changing the MSBuild process environment.</summary>
internal static class InteropGeneratorGCPolicy
{
    /// <summary>The minimum processor count for automatic Server GC selection.</summary>
    private const int MinimumProcessorCount = 4;

    /// <summary>The minimum system memory in megabytes for automatic Server GC selection.</summary>
    private const ulong MinimumSystemMemoryInMegabytes = 16 * 1024;

    /// <summary>Determines whether this system should use Server GC based on processor count and installed physical memory.</summary>
    public static bool ShouldUseServerGC()
    {
        // Server GC trades dedicated threads and larger heaps for throughput
        int processors = Environment.ProcessorCount;

        // Check for enough system RAM to accommodate the generator's higher peak memory usage with Server GC
        if (!PInvoke.GetPhysicallyInstalledSystemMemory(out ulong memoryInKilobytes))
        {
            // Without a memory estimate, keep the conservative workstation GC default
            return false;
        }

        ulong memoryInMegabytes = memoryInKilobytes / 1024;

        return processors >= MinimumProcessorCount && memoryInMegabytes >= MinimumSystemMemoryInMegabytes;
    }
}
