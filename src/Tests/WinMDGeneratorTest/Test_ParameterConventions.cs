// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// End-to-end tests for the WinMD generator's validation of unsupported method parameter conventions.
/// </summary>
/// <remarks>
/// These tests run the actual <c>cswinrtwinmdgen</c> tool and assert that unsupported array/span
/// parameter shapes are rejected with the expected error. Keeping the failure cases here lets us
/// exercise them without breaking the build. The supported conventions that produce a valid
/// <c>.winmd</c> (PassArray/FillArray/ReceiveArray) are covered by the authoring tests instead.
/// </remarks>
[TestClass]
public class Test_ParameterConventions
{
    [TestMethod]
    public void ValidComponent_GeneratesSuccessfully()
    {
        // Smoke test: a valid component generates a '.winmd', validating the end-to-end harness
        WinMDGeneratorRunner.AssertSuccess("""
            public interface IComponent
            {
                int Method(int value);
            }
            """);
    }

    [TestMethod]
    public void RefArrayParameter_IsRejected()
    {
        WinMDGeneratorRunner.AssertFailure("""
            public interface IComponent
            {
                void Method(ref int[] values);
            }
            """, error: "CSWINRTWINMDGEN0011");
    }

    [TestMethod]
    public void InArrayParameter_IsRejected()
    {
        // 'in int[]' carries a 'modreq(InAttribute)' on interface members; the generator must still
        // see through it to detect the by-reference array.
        WinMDGeneratorRunner.AssertFailure("""
            public interface IComponent
            {
                void Method(in int[] values);
            }
            """, error: "CSWINRTWINMDGEN0011");
    }

    [TestMethod]
    public void OutSpanParameter_IsRejected()
    {
        WinMDGeneratorRunner.AssertFailure("""
            using System;
            public interface IComponent
            {
                void Method(out Span<int> values);
            }
            """, error: "CSWINRTWINMDGEN0012");
    }

    [TestMethod]
    public void OutReadOnlySpanParameter_IsRejected()
    {
        WinMDGeneratorRunner.AssertFailure("""
            using System;
            public interface IComponent
            {
                void Method(out ReadOnlySpan<int> values);
            }
            """, error: "CSWINRTWINMDGEN0012");
    }
}
