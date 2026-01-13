// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal class RuntimeClassNameGenerator
{
    public static string GetRuntimeClassName(TypeSignature type, InteropReferences interopReferences)
    {
        return type.FullName;
    }
}
