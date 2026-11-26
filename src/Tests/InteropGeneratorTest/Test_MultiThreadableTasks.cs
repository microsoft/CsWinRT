// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Reflection;
using AsmResolver.DotNet;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_MultiThreadableTasks
{
    [TestMethod]
    [DataRow("GeneratorTasksAssemblyPath")]
    [DataRow("GeneratorTasksNetFrameworkAssemblyPath")]
    public void AllTasks_AreMarkedAsMultiThreadable(string metadataKey)
    {
        string path = typeof(Test_MultiThreadableTasks).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == metadataKey).Value!;

        ModuleDefinition module = ModuleDefinition.FromFile(System.IO.Path.GetFullPath(path));

        // Every public type in the task assembly is an MSBuild task (the attribute is not inherited, so each one needs it)
        TypeDefinition[] tasks = [.. module.TopLevelTypes.Where(static type => type.IsPublic && !type.IsAbstract)];

        Assert.IsTrue(tasks.Length >= 6, "Expected to find all the CsWinRT build tasks.");

        foreach (TypeDefinition task in tasks)
        {
            // The attribute must be the one from MSBuild, not a polyfill defined in the task assembly
            Assert.IsTrue(
                task.CustomAttributes.Any(static attribute =>
                    attribute.Constructor?.DeclaringType is { Namespace.Value: "Microsoft.Build.Framework", Name.Value: "MSBuildMultiThreadableTaskAttribute" } type &&
                    type.Scope?.GetAssembly()?.Name?.Value == "Microsoft.Build.Framework"),
                $"Task '{task.FullName}' is missing '[MSBuildMultiThreadableTask]'.");

            // Tool tasks inherit 'IMultiThreadableTask' from 'ToolTask', all other tasks need to implement it directly
            bool isToolTask = task.BaseType?.Name?.Value == "ToolTask";

            Assert.IsTrue(
                isToolTask || task.Interfaces.Any(static i => i.Interface?.Name?.Value == "IMultiThreadableTask"),
                $"Task '{task.FullName}' does not implement 'IMultiThreadableTask'.");

            // 'ToolTask' doesn't make a relative tool path absolute, so each tool task has to resolve it ('ResolveFileName')
            Assert.IsTrue(
                !isToolTask || task.Methods.Any(static method => method.Name?.Value == "GetProcessStartInfo"),
                $"Tool task '{task.FullName}' does not override 'GetProcessStartInfo'.");
        }
    }
}
