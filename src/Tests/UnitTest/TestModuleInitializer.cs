// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;

namespace UnitTest;

// CsWinRT makes use of the .NET typemap to register all the projected types.
// As part this, .NET makes use of TypeMapAssemblyTarget to discover the assemblies with the type map.
// But this needs to be on the launching executable for it to discover them by default.  Given with
// a test runner, they aren't, this does the alternative way of setting the assembly with that attribute
// as the entry assembly which then allows .NET to discover it.
internal static class ProjectionTypesInitializer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void InitializeProjectionTypes()
    {
        Assembly.SetEntryAssembly(typeof(ProjectionTypesInitializer).Assembly);
    }
}
