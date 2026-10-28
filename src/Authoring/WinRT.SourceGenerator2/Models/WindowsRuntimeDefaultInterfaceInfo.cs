// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.SourceGenerator.Models;

/// <summary>
/// The default-interface identity recorded by a reference projection.
/// </summary>
/// <param name="Signature">The signature used when the runtime class is a generic argument.</param>
/// <param name="Iid">The IID used when the runtime class itself is queried.</param>
internal sealed record WindowsRuntimeDefaultInterfaceInfo(string Signature, Guid Iid);
