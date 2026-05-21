// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Generation.WorkItems;

/// <summary>
/// A single unit of work dispatched from <see cref="ProjectionGenerator.Run"/> to the parallel
/// orchestrator. Each implementation owns the inputs it needs to execute (closure-style) so the
/// orchestrator can simply iterate the work-item enumerable and call <see cref="Execute"/> on
/// each item without knowing the per-item shape.
/// </summary>
internal interface IProjectionWorkItem
{
    /// <summary>
    /// Executes the work item. Must be safe to invoke concurrently with sibling work items
    /// produced by the same enumeration.
    /// </summary>
    void Execute();
}
