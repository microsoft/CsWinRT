// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

[assembly: global::System.Runtime.Versioning.SupportedOSPlatform("Windows")]

namespace AuthoringMixedWinMDTypesTest;

/// <summary>
/// A runtime class authored by this component, so it goes into the generated '.winmd' and is activated
/// through the factory the component pipeline generates for it.
/// </summary>
public sealed class Greeter
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    public IList<int> GetNumbers()
    {
        return new List<int> { 1, 2, 3 };
    }
}

/// <summary>
/// Implements 'TestComponent.Class', a runtime class declared in existing metadata, so it does *not* go
/// into this component's '.winmd'. It can only be activated through the parameterless
/// 'ActivateInstance', so no factory is declared here: CsWinRT generates one.
/// </summary>
public sealed class ImplementedClass : global::ABI.TestComponent.Class
{
    public override int One() => 1;
}
