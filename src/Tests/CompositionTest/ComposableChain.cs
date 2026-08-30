// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CompositionTestComponent;

namespace CompositionTest;

/// <summary>
/// An unsealed C# class deriving from the projected, metadata exposed C++/WinRT runtime class
/// <see cref="NativeComposableMiddle"/>, which in turn derives from the C# authored composable class
/// <c>AuthoringTest.ComposableBase</c>.
/// </summary>
/// <remarks>
/// <para>
/// Constructing one of these produces a three level COM aggregate spanning two languages and two
/// separate CsWinRT instances: this managed object is the controlling outer, the native middle class
/// is the object it aggregates, and the managed <c>ComposableBase</c> inside <c>AuthoringTest.dll</c>
/// is the innermost, non delegating inner object.
/// </para>
/// <para>
/// The overrides below cover both overridable surfaces the aggregate exposes: the ones declared by
/// the C++ middle class (<c>[overridable]</c> members in its IDL) and the ones declared by the C#
/// base (both the interface CsWinRT synthesizes out of its <c>virtual</c> members, and the
/// <c>[WindowsRuntimeOverridable]</c> interface it authors explicitly). Every override calls the base
/// implementation, so a test can tell all three layers apart in the result.
/// </para>
/// </remarks>
public class UnsealedCSharpOuter : NativeComposableMiddle
{
    public UnsealedCSharpOuter()
    {
    }

    public UnsealedCSharpOuter(int initialValue)
        : base(initialValue)
    {
    }

    /// <summary>
    /// The number of times each override declared by this class has run.
    /// </summary>
    public int OuterComputeMiddleValueCallCount { get; private set; }

    /// <inheritdoc cref="OuterComputeMiddleValueCallCount"/>
    public int OuterComputeCoreValueCallCount { get; private set; }

    /// <summary>
    /// Overrides an <c>[overridable]</c> member of the C++ middle class.
    /// </summary>
    protected override int ComputeMiddleValue()
    {
        OuterComputeMiddleValueCallCount++;

        return base.ComputeMiddleValue() + 100;
    }

    /// <inheritdoc cref="ComputeMiddleValue"/>
    protected override string DescribeMiddleCore()
    {
        return "CSharpOuter:" + base.DescribeMiddleCore();
    }

    /// <summary>
    /// Overrides a member of the interface CsWinRT synthesizes out of the <c>virtual</c> members of
    /// the C# base. The C++ middle class overrides it too, so this is a three level chain.
    /// </summary>
    protected override int ComputeValue()
    {
        return base.ComputeValue() + 1000;
    }

    /// <summary>
    /// Overrides another member of the same synthesized interface, which the C++ middle class does
    /// not override, so the base call reaches the C# base implementation directly.
    /// </summary>
    protected override string DescribeCore()
    {
        return "CSharpOuter:" + base.DescribeCore();
    }

    /// <inheritdoc cref="DescribeCore"/>
    protected override int OverridableValue => base.OverridableValue + 1;

    /// <summary>
    /// Overrides the member of the <c>[WindowsRuntimeOverridable]</c> interface the C# base authors
    /// itself. This is the one the C# base can name, and therefore the one it dispatches to through
    /// the controlling outer object (see <c>ComposableBase.CallComputeCoreValue</c>).
    /// </summary>
    protected override int ComputeCoreValue()
    {
        OuterComputeCoreValueCallCount++;

        return base.ComputeCoreValue() + 50;
    }

    /// <summary>
    /// Reaches the <c>[protected]</c> surface of the C++ middle class.
    /// </summary>
    public int CallMiddleSecretValue() => GetMiddleSecretValue();

    /// <inheritdoc cref="CallMiddleSecretValue"/>
    public string MiddleSecret
    {
        get => MiddleSecretTag;
        set => MiddleSecretTag = value;
    }

    /// <summary>
    /// Reaches the <c>[protected]</c> surface of the C# base, two layers down.
    /// </summary>
    public int CallBaseSecretValueFromOuter() => GetSecretValue();

    /// <inheritdoc cref="CallBaseSecretValueFromOuter"/>
    public string BaseSecret
    {
        get => SecretTag;
        set => SecretTag = value;
    }

    /// <summary>
    /// Invokes the overridable members through plain managed virtual dispatch, so a test can tell
    /// the managed dispatch apart from the dispatch that goes through the aggregate.
    /// </summary>
    public int CallOwnComputeValue() => ComputeValue();

    /// <inheritdoc cref="CallOwnComputeValue"/>
    public string CallOwnDescribeCore() => DescribeCore();

    /// <inheritdoc cref="CallOwnComputeValue"/>
    public int CallOwnOverridableValue() => OverridableValue;

    /// <inheritdoc cref="CallOwnComputeValue"/>
    public int CallOwnComputeCoreValue() => ComputeCoreValue();

    /// <inheritdoc cref="CallOwnComputeValue"/>
    public int CallOwnComputeMiddleValueFromOuter() => ComputeMiddleValue();
}

/// <summary>
/// A sealed C# class deriving from the projected C++/WinRT middle class. A sealed derived type takes
/// the same aggregation path as an unsealed one, it just cannot be composed any further itself.
/// </summary>
public sealed class SealedCSharpOuter : NativeComposableMiddle
{
    public SealedCSharpOuter()
    {
    }

    public SealedCSharpOuter(int initialValue)
        : base(initialValue)
    {
    }

    /// <inheritdoc cref="UnsealedCSharpOuter.ComputeMiddleValue"/>
    protected override int ComputeMiddleValue() => base.ComputeMiddleValue() + 200;

    /// <inheritdoc cref="UnsealedCSharpOuter.DescribeMiddleCore"/>
    protected override string DescribeMiddleCore() => "SealedOuter:" + base.DescribeMiddleCore();

    /// <inheritdoc cref="UnsealedCSharpOuter.ComputeCoreValue"/>
    protected override int ComputeCoreValue() => base.ComputeCoreValue() + 60;

    /// <inheritdoc cref="UnsealedCSharpOuter.CallMiddleSecretValue"/>
    public int CallMiddleSecretValue() => GetMiddleSecretValue();

    /// <inheritdoc cref="UnsealedCSharpOuter.CallBaseSecretValueFromOuter"/>
    public int CallBaseSecretValueFromOuter() => GetSecretValue();
}

/// <summary>
/// A sealed C# class deriving from the unsealed C# class above. Deriving from a managed class is
/// plain managed inheritance, so the aggregate stays three levels deep and this type shares the
/// single COM callable wrapper of its base.
/// </summary>
public sealed class DeepCSharpOuter : UnsealedCSharpOuter
{
    public DeepCSharpOuter(int initialValue)
        : base(initialValue)
    {
    }

    /// <inheritdoc cref="UnsealedCSharpOuter.ComputeMiddleValue"/>
    protected override int ComputeMiddleValue() => base.ComputeMiddleValue() + 10000;

    /// <inheritdoc cref="UnsealedCSharpOuter.ComputeCoreValue"/>
    protected override int ComputeCoreValue() => base.ComputeCoreValue() + 5000;
}
