// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_InterfaceIIDLookup
{
    [TestMethod]
    public async Task GeneratedIidGetters_HaveUniqueNamesAndDeterministicOrder()
    {
        using InteropGeneratorRunner runner = new(
            useFrameworkImplementations: true,
            forwardedEnumerableAliases: true,
            ccwGroupingCases: true);
        string output = await runner.GenerateAsync("serial");
        (_, ModuleDefinition module) = runner.LoadOutput(output);
        TypeDefinition iids = module.TopLevelTypes.Single(type => type.FullName == "ABI.InterfaceIIDs");
        Assert.AreEqual(iids.Methods.Count, iids.Methods.Select(method => method.Name!).Distinct().Count(),
            "Generated IID getters must have unique names, including when inputs contain forwarded aliases.");

        int calls = 0;

        foreach (MethodDefinition method in module.TopLevelTypes
            .Where(type => type.Namespace == "WindowsRuntime.Interop.UserDefinedTypes" &&
                type.Name!.ToString().EndsWith("InterfaceEntriesImpl", StringComparison.Ordinal))
            .SelectMany(type => type.Methods))
        {
            foreach (object? operand in method.CilMethodBody!.Instructions.Select(instruction => instruction.Operand))
            {
                if (operand is MethodDefinition getter && getter.DeclaringType == iids)
                {
                    Assert.AreSame(iids.Methods.Single(candidate => candidate.Name == getter.Name), getter,
                        $"CCW lookup must reference the generated getter named '{getter.Name}'.");
                    calls++;
                }
            }
        }

        Assert.IsGreaterThan(0, calls, "The test must exercise generated generic interface IID lookups.");

        byte[] expected = File.ReadAllBytes(output);

        foreach ((bool reverse, int parallelism) in new[] { (true, 1), (false, -1), (true, 2) })
        {
            byte[] actual = File.ReadAllBytes(await runner.GenerateAsync($"order-{reverse}-{parallelism}", reverse, parallelism));
            CollectionAssert.AreEqual(expected, actual, "IID indexing must not change emission order.");
        }
    }

    [TestMethod]
    [DataRow(false, 1)]
    [DataRow(true, 1)]
    [DataRow(false, -1)]
    [DataRow(true, 2)]
    public async Task CollidingGeneratedIidNames_FailWithDiagnostic(bool reverseInputs, int parallelism)
    {
        using InteropGeneratorRunner runner = new(iidNameCollisionCases: true);
        string log = await runner.AssertFailureAsync("collision", "CSWINRTINTEROPGEN0112", reverseInputs, parallelism);

        StringAssert.Contains(log, "generated IID getter name 'get_IID_");
        StringAssert.Contains(log, "IidNames-A-Value");
    }
}
