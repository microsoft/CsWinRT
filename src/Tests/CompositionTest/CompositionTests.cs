// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AuthoringTest;
using CompositionTestComponent;

namespace CompositionTest;

/// <summary>
/// End to end tests for a Windows Runtime composition chain spanning three layers and two languages.
/// </summary>
/// <remarks>
/// <para>The chain under test is:</para>
/// <list type="number">
///   <item><c>AuthoringTest.ComposableBase</c>: a public unsealed runtime class authored in C#.</item>
///   <item>
///     <c>CompositionTestComponent.NativeComposableMiddle</c>: a metadata exposed, unsealed
///     C++/WinRT runtime class deriving from it, implemented in a real C++ component.
///   </item>
///   <item>
///     <see cref="SealedCSharpOuter"/> and <see cref="UnsealedCSharpOuter"/>: C# classes deriving
///     from the projection of the middle class.
///   </item>
/// </list>
/// <para>
/// A fully constructed instance of the third layer is a three level COM aggregate: the managed outer
/// object aggregates the native middle object, which in turn aggregates the managed inner object
/// living in <c>AuthoringTest.dll</c>. The two managed halves run on two different CsWinRT instances
/// (the test app runs on CoreCLR, the authored component is published with NativeAOT), so every call
/// that crosses a layer is a real cross ABI call.
/// </para>
/// </remarks>
internal static class CompositionTests
{
    private static readonly List<string> Failures = [];

    private static int _checks;

    /// <summary>
    /// Runs every test and reports the failures.
    /// </summary>
    /// <returns>Whether all the checks passed.</returns>
    public static bool RunAll()
    {
        Run(NativeMiddleStandalone);
        Run(NativeMiddleParameterlessActivation);
        Run(NativeMiddleProperties);
        Run(NativeLeaf);
        Run(SealedOuterOrdinaryMembers);
        Run(SealedOuterProtectedMembers);
        Run(SealedOuterOverridableDispatch);
        Run(UnsealedOuterOverridableDispatch);
        Run(UnsealedOuterEveryLayerRunsExactlyOnce);
        Run(DeepOuterOverridableDispatch);
        Run(OuterComIdentity);
        Run(ProjectedCSharpDerivedComposable);
        Run(Statics);
        Run(NativeMiddleLifetime);

        Console.WriteLine();
        Console.WriteLine($"{_checks} checks, {Failures.Count} failure(s).");

        foreach (string failure in Failures)
        {
            Console.WriteLine($"  FAILED: {failure}");
        }

        return Failures.Count == 0;
    }

    private static void Run(Action test, [CallerArgumentExpression(nameof(test))] string name = "")
    {
        Console.WriteLine($"[ RUN ] {name}");

        int failuresBefore = Failures.Count;

        try
        {
            test();
        }
        catch (Exception e)
        {
            Failures.Add($"{name}: threw {e.GetType().Name}: {e.Message}");
        }

        Console.WriteLine(Failures.Count == failuresBefore ? $"[  OK ] {name}" : $"[FAIL ] {name}");
    }

    private static void Check<T>(T expected, T actual, [CallerArgumentExpression(nameof(actual))] string expression = "")
    {
        _checks++;

        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Failures.Add($"{expression}: expected <{expected}>, got <{actual}>");
        }
    }

    private static void CheckTrue(bool actual, [CallerArgumentExpression(nameof(actual))] string expression = "")
    {
        Check(true, actual, expression);
    }

    /// <summary>
    /// The native middle class works standalone (no derived type, so it is the controlling outer of
    /// the aggregate it forms with the C# base).
    /// </summary>
    private static void NativeMiddleStandalone()
    {
        NativeComposableMiddle middle = new(7);

        // Ordinary members of the C# base, reached through the delegating interfaces of the aggregate
        Check(7, middle.GetValue());
        Check("ComposableBase", middle.GetName());
        Check("ComposableBase(7)", middle.DescribeSelf());
        Check(8, middle.GetThingValue());
        Check(9, middle.GetBaseThingValue());

        // Ordinary members of the C++ middle class
        Check(700, middle.GetMiddleValue());
        Check("NativeComposableMiddle(7)", middle.DescribeMiddle());

        // The C++ middle class reaching the members of the C# base through the inner object
        Check(7, middle.CallBaseGetValue());
        Check("ComposableBase(7)", middle.CallBaseDescribeSelf());
        Check(21, middle.CallBaseSecretValue());
        Check("secret", middle.GetBaseSecretTag());
        Check(14, middle.CallBaseComputeValue());
        Check("ComposableBase:7", middle.CallBaseDescribeCore());
        Check(1007, middle.CallBaseOverridableValue());
        Check(70, middle.CallBaseComputeCoreValue());

        // Overridable dispatch with no derived type: it lands on the implementation of the
        // C++ middle class for its own members, and on the C# base for the inherited ones
        Check(21, middle.CallComputeMiddleValue());
        Check("NativeMiddle:7", middle.CallDescribeMiddleCore());
        Check(21, middle.CallOwnComputeMiddleValue());
        Check("NativeMiddle:7", middle.CallOwnDescribeMiddleCore());

        // 'ComputeCoreValue' is overridden by the C++ middle class, so the most derived implementation
        // is the C++ one, which adds 3 to the result of the C# base implementation
        Check(73, middle.CallComputeCoreValue());
        Check(73, middle.CallBaseCallComputeCoreValue());

        // 'CallComputeValue' and friends dispatch with plain managed virtual dispatch inside the C#
        // base, which only ever sees the inner object, so they run the C# base implementation
        Check(14, middle.CallComputeValue());
        Check("ComposableBase:7", middle.CallDescribeCore());
        Check(1007, middle.CallOverridableValue());

        // The C# base sees itself as the inner object of an aggregate
        Check("ComposableBase(7)|73|1", ComposableBase.ProbeAggregate(middle));
    }

    /// <summary>
    /// The parameterless constructor of the C++ middle class chains to the parameterless constructor
    /// of the composable factory of the C# base (which defaults its value to 42), while the C++ layer
    /// keeps its own default (5).
    /// </summary>
    private static void NativeMiddleParameterlessActivation()
    {
        NativeComposableMiddle middle = new();

        Check(42, middle.GetValue());
        Check(500, middle.GetMiddleValue());
        Check(15, middle.CallOwnComputeMiddleValue());
        Check(420, middle.CallBaseComputeCoreValue());
    }

    /// <summary>
    /// Read/write properties at both layers round-trip through the aggregate.
    /// </summary>
    private static void NativeMiddleProperties()
    {
        NativeComposableMiddle middle = new(2);

        Check("base", middle.Tag);
        Check("middle", middle.MiddleTag);

        middle.Tag = "base-updated";
        middle.MiddleTag = "middle-updated";

        Check("base-updated", middle.Tag);
        Check("middle-updated", middle.MiddleTag);

        // The '[protected]' property of the C# base, set from the C++ middle class
        Check("secret", middle.GetBaseSecretTag());

        middle.SetBaseSecretTag("secret-updated");

        Check("secret-updated", middle.GetBaseSecretTag());
    }

    /// <summary>
    /// A sealed native class deriving from the middle class in the same component. C++/WinRT
    /// implements that as plain C++ inheritance, so the aggregate stays two levels deep and the
    /// overrides are reached through C++ virtual dispatch.
    /// </summary>
    private static void NativeLeaf()
    {
        NativeComposableLeaf leaf = new(9);

        Check(9000, leaf.GetLeafValue());
        Check(9, leaf.GetValue());
        Check(900, leaf.GetMiddleValue());

        // The most derived implementation is the one of the leaf
        Check(38, leaf.CallComputeMiddleValue());
        Check("NativeLeaf:NativeMiddle:9", leaf.CallDescribeMiddleCore());

        // ...while the implementation of the middle class itself stays reachable
        Check(27, leaf.CallOwnComputeMiddleValue());

        Check(90, leaf.CallBaseComputeCoreValue());
        Check(93, leaf.CallBaseCallComputeCoreValue());
        Check("ComposableBase(9)|93|1", ComposableBase.ProbeAggregate(leaf));
    }

    /// <summary>
    /// A sealed C# class deriving from the projected C++ middle class inherits the ordinary members
    /// of both layers below it.
    /// </summary>
    private static void SealedOuterOrdinaryMembers()
    {
        SealedCSharpOuter outer = new(3);

        // From the C# base, two layers down
        Check(3, outer.GetValue());
        Check("ComposableBase(3)", outer.DescribeSelf());
        Check(4, outer.GetThingValue());
        Check("base", outer.Tag);

        // From the C++ middle class
        Check(300, outer.GetMiddleValue());
        Check("NativeComposableMiddle(3)", outer.DescribeMiddle());
        Check("middle", outer.MiddleTag);

        outer.Tag = "outer-tag";
        outer.MiddleTag = "outer-middle-tag";

        Check("outer-tag", outer.Tag);
        Check("outer-middle-tag", outer.MiddleTag);

        // 'GetName' returns 'GetType().Name' of the managed inner object, which is the C# base
        Check("ComposableBase", outer.GetName());
    }

    /// <summary>
    /// A derived C# class can reach the <c>[protected]</c> surface of both layers below it.
    /// </summary>
    private static void SealedOuterProtectedMembers()
    {
        SealedCSharpOuter outer = new(3);

        Check(21, outer.CallMiddleSecretValue());
        Check(9, outer.CallBaseSecretValueFromOuter());

        UnsealedCSharpOuter unsealedOuter = new(4);

        Check(28, unsealedOuter.CallMiddleSecretValue());
        Check(12, unsealedOuter.CallBaseSecretValueFromOuter());
        Check("middle-secret", unsealedOuter.MiddleSecret);
        Check("secret", unsealedOuter.BaseSecret);

        unsealedOuter.MiddleSecret = "outer-middle-secret";
        unsealedOuter.BaseSecret = "outer-base-secret";

        Check("outer-middle-secret", unsealedOuter.MiddleSecret);
        Check("outer-base-secret", unsealedOuter.BaseSecret);
    }

    /// <summary>
    /// Overridable dispatch reaches the C# outer override from both layers below it.
    /// </summary>
    private static void SealedOuterOverridableDispatch()
    {
        SealedCSharpOuter outer = new(3);

        // Dispatch from the C++ middle class ('overridable()' in C++/WinRT) reaches the C# override,
        // which calls back into the implementation of the C++ middle class (3 * 3 = 9, + 200)
        Check(209, outer.CallComputeMiddleValue());
        Check("SealedOuter:NativeMiddle:3", outer.CallDescribeMiddleCore());

        // ...while the implementation of the C++ middle class itself stays reachable
        Check(9, outer.CallOwnComputeMiddleValue());
        Check("NativeMiddle:3", outer.CallOwnDescribeMiddleCore());

        // Dispatch from the C# base ('WindowsRuntimeComposition.GetControllingOuterObject') walks the
        // full chain: C# base (3 * 10 = 30) -> C++ middle (+3) -> C# outer (+60)
        Check(93, outer.CallComputeCoreValue());
        Check(93, outer.CallBaseCallComputeCoreValue());

        // ...and each base implementation in that chain stays reachable on its own
        Check(30, outer.CallBaseComputeCoreValue());

        // The members the C# outer does not override keep running the implementation they had
        Check(6, outer.CallComputeValue());
        Check(6, outer.CallBaseComputeValue());
        Check("ComposableBase:3", outer.CallDescribeCore());
        Check(1003, outer.CallOverridableValue());

        Check("ComposableBase(3)|93|1", ComposableBase.ProbeAggregate(outer));
    }

    /// <summary>
    /// Same as above for an unsealed C# class, which also overrides the members of the interface
    /// CsWinRT synthesizes out of the <c>virtual</c> members of the C# base.
    /// </summary>
    private static void UnsealedOuterOverridableDispatch()
    {
        UnsealedCSharpOuter outer = new(4);

        Check(112, outer.CallComputeMiddleValue());
        Check("CSharpOuter:NativeMiddle:4", outer.CallDescribeMiddleCore());
        Check(12, outer.CallOwnComputeMiddleValue());

        // Managed virtual dispatch on the outer object walks the same three layers, because every
        // override calls its base implementation, which routes back through the aggregate
        Check(112, outer.CallOwnComputeMiddleValueFromOuter());

        // C# base (4 * 2 = 8) -> C++ middle (+5) -> C# outer (+1000)
        Check(1013, outer.CallOwnComputeValue());

        // The C++ middle class does not override 'DescribeCore' or 'OverridableValue', so those
        // reach the C# base implementation directly
        Check("CSharpOuter:ComposableBase:4", outer.CallOwnDescribeCore());
        Check(1005, outer.CallOwnOverridableValue());

        // C# base (4 * 10 = 40) -> C++ middle (+3) -> C# outer (+50)
        Check(93, outer.CallOwnComputeCoreValue());
        Check(93, outer.CallComputeCoreValue());
        Check(93, outer.CallBaseCallComputeCoreValue());
        Check(40, outer.CallBaseComputeCoreValue());

        Check("ComposableBase(4)|93|1", ComposableBase.ProbeAggregate(outer));
    }

    /// <summary>
    /// A single dispatch through the aggregate runs exactly one implementation per layer.
    /// </summary>
    private static void UnsealedOuterEveryLayerRunsExactlyOnce()
    {
        UnsealedCSharpOuter outer = new(4);

        ComposableBase.ResetCallCounts();
        NativeComposableMiddle.ResetMiddleCallCounts();

        Check(93, outer.CallComputeCoreValue());

        Check(1, outer.OuterComputeCoreValueCallCount);
        Check(1, NativeComposableMiddle.MiddleComputeCoreValueCallCount);
        Check(1, ComposableBase.ComputeCoreValueCallCount);

        // 'DescribeSelf' is not overridable, so it always runs the C# base implementation
        ComposableBase.ResetCallCounts();

        Check("ComposableBase(4)", outer.DescribeSelf());
        Check(1, ComposableBase.DescribeSelfCallCount);
    }

    /// <summary>
    /// A sealed C# class deriving from the unsealed C# class. Deriving from a managed class is plain
    /// managed inheritance, so the aggregate stays three levels deep and the extra managed layer is
    /// simply part of the controlling outer object.
    /// </summary>
    private static void DeepOuterOverridableDispatch()
    {
        DeepCSharpOuter outer = new(6);

        Check(6, outer.GetValue());

        // C++ middle (6 * 3 = 18) -> unsealed C# outer (+100) -> sealed C# outer (+10000)
        Check(10118, outer.CallComputeMiddleValue());

        // Only the unsealed C# outer overrides 'DescribeMiddleCore'
        Check("CSharpOuter:NativeMiddle:6", outer.CallDescribeMiddleCore());

        // C# base (60) -> C++ middle (+3) -> unsealed C# outer (+50) -> sealed C# outer (+5000)
        Check(5113, outer.CallComputeCoreValue());
        Check("ComposableBase(6)|5113|1", ComposableBase.ProbeAggregate(outer));
    }

    /// <summary>
    /// The aggregate has a single COM identity: every layer that hands itself back to managed code
    /// produces the controlling outer object, which unwraps to the very same managed instance.
    /// </summary>
    private static void OuterComIdentity()
    {
        SealedCSharpOuter outer = new(3);

        // From the C++ middle class ('*this' in C++/WinRT is the controlling outer)
        CheckTrue(ReferenceEquals(outer, outer.GetSelfAsObject()));
        CheckTrue(ReferenceEquals(outer, outer.GetSelfAsMiddle()));
        CheckTrue(ReferenceEquals(outer, outer.GetSelfAsBaseClass()));

        // From the C# base, two layers down
        CheckTrue(ReferenceEquals(outer, outer.GetSelf()));
        CheckTrue(ReferenceEquals(outer, outer.GetSelfAsBase()));
        CheckTrue(ReferenceEquals(outer, outer.GetSelfAsThing()));
        CheckTrue(ReferenceEquals(outer, outer.GetSelfAsThingBase()));

        // Round-tripping the managed instance back into native code preserves the identity of the
        // aggregate on both sides of the chain
        CheckTrue(outer.IsSameMiddle(outer));
        CheckTrue(outer.IsSameInstance(outer));
        CheckTrue(outer.IsSameThing(outer));

        SealedCSharpOuter other = new(3);

        Check(false, outer.IsSameMiddle(other));
        Check(false, outer.IsSameInstance(other));

        // A native derived type is a distinct aggregate, and marshals as such
        NativeComposableMiddle middle = new(3);

        Check(false, outer.IsSameMiddle(middle));
    }

    /// <summary>
    /// A sealed C# class in the authored component deriving from the same composable base is
    /// projected as a plain sealed class, and is not an aggregate.
    /// </summary>
    private static void ProjectedCSharpDerivedComposable()
    {
        CSharpDerivedComposable derived = new();

        Check(84, derived.GetValue());
        Check(168, derived.GetDerivedValue());
        Check(252, derived.GetDerivedSecretValue());

        // Managed virtual dispatch inside the authored component picks the derived implementation
        Check(169, derived.CallComputeValue());
        Check(840, derived.CallComputeCoreValue());
        Check("ComposableBase(84)|840|0", ComposableBase.ProbeAggregate(derived));
    }

    /// <summary>
    /// Statics live on the activation factory of each layer, so they are reachable without an
    /// instance and are not affected by the composition.
    /// </summary>
    private static void Statics()
    {
        Check("default", ComposableBase.DefaultTag);
        Check("ComposableBase(5)", ComposableBase.DescribeValue(5));
        Check("NativeComposableMiddle(5)", NativeComposableMiddle.DescribeMiddleValue(5));
    }

    /// <summary>
    /// The native middle object of the aggregate is kept alive by the managed outer object, and is
    /// destroyed once that is collected.
    /// </summary>
    private static void NativeMiddleLifetime()
    {
        // Every aggregate created by the tests above is unreachable by now, but tearing one down
        // takes a collection on both sides: the managed inner object holds a reference on the
        // aggregate (so the authored component has to collect it), and the managed outer object is
        // what keeps the native middle object alive (so this app has to collect it). Alternating the
        // two until the counters settle gives a stable baseline.
        CollectBothRuntimes();

        int liveBefore = NativeComposableMiddle.LiveInstanceCount;
        int destroyedBefore = NativeComposableMiddle.DestroyedInstanceCount;

        int live = CreateScopedOuter();

        // While the managed outer object is alive, so is the native middle object it aggregates
        Check(liveBefore + 1, live);
        Check(destroyedBefore, NativeComposableMiddle.DestroyedInstanceCount);

        CollectBothRuntimes();

        Check(liveBefore, NativeComposableMiddle.LiveInstanceCount);
        Check(destroyedBefore + 1, NativeComposableMiddle.DestroyedInstanceCount);
    }

    /// <summary>
    /// Runs a full collection on both this application and the authored component.
    /// </summary>
    private static void CollectBothRuntimes()
    {
        for (int i = 0; i < 4; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            ComposableBase.CollectManagedObjects();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CreateScopedOuter()
    {
        SealedCSharpOuter outer = new(11);

        Check(1100, outer.GetMiddleValue());

        int live = NativeComposableMiddle.LiveInstanceCount;

        GC.KeepAlive(outer);

        return live;
    }
}
