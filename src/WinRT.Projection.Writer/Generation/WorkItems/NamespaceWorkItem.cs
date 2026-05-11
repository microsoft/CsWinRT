// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter.Generation.WorkItems;

/// <summary>
/// Work item that emits the projection <c>.cs</c> file for a single namespace. Each work item
/// owns its target namespace + its <see cref="NamespaceMembers"/> bag and contributes to the
/// shared <see cref="ProjectionGeneratorRunState"/> through its concurrent collections.
/// </summary>
/// <param name="owner">The owning generator (provides access to settings, cache, and the per-namespace emission entry point).</param>
/// <param name="ns">The namespace being processed.</param>
/// <param name="members">The types in the target namespace.</param>
/// <param name="state">The shared run state that this work item writes into.</param>
internal sealed class NamespaceWorkItem(
    ProjectionGenerator owner,
    string ns,
    NamespaceMembers members,
    ProjectionGeneratorRunState state) : IProjectionWorkItem
{
    /// <inheritdoc/>
    public void Execute()
    {
        try
        {
            if (owner.ProcessNamespace(ns, members, state))
            {
                state.MarkProjectionFileWritten();
            }
        }
        catch (Exception e)
        {
            WellKnownProjectionWriterExceptions.NamespaceEmissionFailed(ns, e).ThrowOrAttach(e);
        }
    }
}
