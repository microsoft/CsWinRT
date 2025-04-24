// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ConsoleAppFramework;

ConsoleApp.Run(args, InteropGenerator.Run);

internal sealed class InteropGeneratorState
{
}

internal static class InteropGenerator
{
    /// <summary>
    /// Runs the interop generator to produce the resulting <c>WinRT.Interop.dll</c> assembly.
    /// </summary>
    /// <param name="input">The input .dll paths.</param>
    /// <param name="output">The output path for the resulting assembly.</param>
    public static void Run(string[] input, string output)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfZero(input.Length, nameof(input));
        ArgumentException.ThrowIfNullOrEmpty(output);

        PathAssemblyResolver pathAssemblyResolver = new(input);
        MetadataLoadContext metadataLoadContext = new(pathAssemblyResolver);

        AssemblyName windowsSdkAssemblyName = new("Microsoft.Windows.SDK.NET");
        Assembly windowsSdkAssembly = metadataLoadContext.LoadFromAssemblyName(windowsSdkAssemblyName);

        foreach (var path in input)
        {
            try
            {
                Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(path);

                if (!assembly.GetReferencedAssemblies().Any(static name => name.Name == "Microsoft.Windows.SDK.NET"))
                {
                    Console.WriteLine($"SKIPPED {assembly.FullName}");

                    continue;
                }

                Console.WriteLine($"Loaded {assembly.FullName}");
            }
            catch
            {
                Console.WriteLine($"FAILED {Path.GetFileName(path)}");
            }
        }

        PersistedAssemblyBuilder persistedAssemblyBuilder = new(new AssemblyName("WinRT.Interop"), metadataLoadContext.CoreAssembly!);

        ModuleBuilder mob = persistedAssemblyBuilder.DefineDynamicModule("MyModule");
        TypeBuilder tb = mob.DefineType("ABI.MyType", TypeAttributes.Public | TypeAttributes.Class);
        MethodBuilder mb = tb.DefineMethod("SumMethod", MethodAttributes.Public | MethodAttributes.Static,
                                                                       typeof(int), [typeof(int), typeof(int)]);
        ILGenerator il = mb.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        _ = tb.CreateType();

        persistedAssemblyBuilder.Save(Path.Combine(output, "WinRT.Interop.dll"));
    }
}
