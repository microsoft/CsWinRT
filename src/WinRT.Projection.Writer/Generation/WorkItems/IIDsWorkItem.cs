// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Generation.WorkItems;

/// <summary>
/// Work item that emits the per-projection <c>GeneratedInterfaceIIDs.cs</c> file (the global
/// IID GUID property table for every projected interface, delegate, enum, struct, and runtime
/// class). Decoupled from per-namespace work items because it produces a single distinct output
/// file that does not contend with any other work item.
/// </summary>
/// <param name="owner">The owning generator (provides access to settings, cache, and the IID-emission entry point).</param>
internal sealed class IIDsWorkItem(ProjectionGenerator owner) : IProjectionWorkItem
{
    /// <inheritdoc/>
    public void Execute()
    {
        owner.WriteGeneratedInterfaceIIDsFile();
    }
}
