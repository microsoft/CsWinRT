// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Generation.WorkItems;

/// <summary>
/// Work item that emits the <c>WinRT_Module.cs</c> file containing the per-module activation-
/// factory entry points. Only enumerated when <see cref="Settings.Component"/> is set.
/// </summary>
/// <param name="owner">The owning generator (provides access to settings + the component-module entry point).</param>
/// <param name="state">The shared run state that this work item writes into (component module always counts as a written projection file).</param>
internal sealed class ComponentModuleWorkItem(
    ProjectionGenerator owner,
    ProjectionGeneratorRunState state) : IProjectionWorkItem
{
    /// <inheritdoc/>
    public void Execute()
    {
        owner.WriteComponentModuleFile(state.ComponentByModule);
        state.MarkProjectionFileWritten();
    }
}
