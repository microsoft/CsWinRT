// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

[assembly: global::System.Runtime.Versioning.SupportedOSPlatform("Windows")]

namespace AuthoringImplementWinMDTypesTest;

/// <summary>
/// Implements 'TestComponent.Class', a runtime class declared in existing metadata. It can only be
/// activated through the parameterless 'ActivateInstance', so no factory is declared here: CsWinRT
/// generates one.
/// </summary>
public sealed class ImplementedClass : global::ABI.TestComponent.Class
{
    public override int One() => 1;
}

/// <summary>
/// Implements the composable 'TestComponent.Composable'. Activating it goes through factory methods,
/// so it needs the factory below.
/// </summary>
public class ImplementedComposable : global::ABI.TestComponent.Composable
{
    public override int Value { get; set; }

    public override int One() => 1;

    public override int Two() => 2;

    public override int Three() => 3;

    public override int Four() => 4;
}

/// <summary>
/// The activation factory for <see cref="ImplementedComposable"/>. The Windows Runtime factory methods
/// take an outer and an inner (raw COM aggregation); that is generated onto the base, leaving only the
/// creation hooks to implement.
/// </summary>
[global::WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(ImplementedComposable))]
public sealed class ImplementedComposableFactory : global::ABI.TestComponent.ComposableActivationFactory
{
    protected override global::ABI.TestComponent.Composable CreateInstance() => new ImplementedComposable();

    protected override global::ABI.TestComponent.Composable CreateWithValue(int init) => new ImplementedComposable { Value = init };

    public override int ExpectComposable(global::TestComponent.Composable t) => 0;

    public override int ExpectRequiredOne(global::TestComponent.IRequiredOne t) => 0;

    public override int ExpectRequiredTwo(global::TestComponent.IRequiredTwo t) => 0;

    public override int ExpectRequiredThree(global::TestComponent.IRequiredThree t) => 0;

    public override int ExpectRequiredFour(global::TestComponent.IRequiredFour t) => 0;
}
