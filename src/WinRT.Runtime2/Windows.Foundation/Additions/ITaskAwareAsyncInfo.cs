// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Threading.Tasks;

internal interface ITaskAwareAsyncInfo
{
    Task Task { get; }
}