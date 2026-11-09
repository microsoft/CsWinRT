// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using ProjectionWriterTest.Helpers;

namespace ProjectionWriterTest;

public partial class Test_InteropGenericDiscovery
{
    [TestMethod]
    public void AnalysisExclusion_AnnotatedTypeMarshalling_IsPreserved()
    {
        AssertGeneration("""
            using Windows.Foundation;
            using WindowsRuntime.InteropServices;
            namespace Recursion;
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Node<T> : IStringable
            {
                private readonly Peer<long> hidden = new();
                public override string ToString() => "node";
            }
            public sealed class Peer<T> : IStringable
            {
                public override string ToString() => "peer";
            }
            public static class Program
            {
                public static void Main() => _ = new Node<int>();
            }
            """, 1);
    }

    [TestMethod]
    public void AnalysisExclusion_ConstructorAndCacheTypeSpecs_DoNotContribute()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache
            {
                private readonly List<Node<long>> items = [new()];
                public object Get() => items[0];
            }
            """, "_ = new Cache().Get();", 1);
    }

    [TestMethod]
    public void AnalysisExclusion_GenericCaller_DoesNotExpandExcludedMethods()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public static class Cache
            {
                public static object Create<T>() => new Node<T>();
            }
            """, "_ = Cache.Create<long>();", 1);
    }

    [TestMethod]
    public void AnalysisExclusion_GenericHelper_DoesNotExpandMembers()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache<T>
            {
                public object Get() => new Node<T>();
            }
            """, "_ = new Cache<long>().Get();", 1);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AnalysisExclusion_IndependentSameTypeUse_IsPreserved(bool exclude)
    {
        AssertExclusion($$"""
            {{(exclude ? "[WindowsRuntimeExcludeFromInteropAnalysis]" : "")}}
            public sealed class Cache
            {
                private readonly List<Node<long>> items = [new()];
            }
            """, "_ = new Cache(); _ = new List<Node<long>>();", 3);
    }

    [TestMethod]
    public void AnalysisExclusion_CallerVisibleReturnSignature_IsPreservedWithoutTypeSpec()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public static class Cache
            {
                public static Node<long> Get() => throw new NotSupportedException();
            }
            """, "_ = Cache.Get();", 2);
    }

    [TestMethod]
    public void AnalysisExclusion_UnexcludedFieldAccess_IsPreserved()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache
            {
                public Node<long> Item;
            }
            """, "_ = new Cache().Item;", 2);
    }

    [TestMethod]
    public void AnalysisExclusion_NestedCompilerHelpers_AreExcluded()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache
            {
                public object Get()
                {
                    Func<object> factory = () => new Node<long>();
                    return factory();
                }
                public async Task<object> GetAsync()
                {
                    await Task.Yield();
                    return new Node<double>();
                }
            }
            """, "_ = new Cache().Get(); _ = new Cache().GetAsync();", 1);
    }

    [TestMethod]
    public void AnalysisExclusion_DerivedUnexcludedImplementation_IsPreserved()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public class Cache
            {
                private readonly Node<long> hidden = new();
            }
            public sealed class Derived : Cache
            {
                public object Get() => new Node<double>();
            }
            """, "_ = new Derived().Get();", 2);
    }

    [TestMethod]
    public void AnalysisExclusion_VirtualGenericDispatch_DoesNotExpandExcludedOverride()
    {
        AssertExclusion("""
            public abstract class Factory
            {
                public abstract object Get<T>();
            }
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache : Factory
            {
                public override object Get<T>() => new Node<T>();
            }
            """, "Factory factory = new Cache(); _ = factory.Get<long>();", 1);
    }

    [TestMethod]
    public void AnalysisExclusion_ExcludedMethodSpec_DoesNotExpandUnexcludedCallee()
    {
        AssertExclusion("""
            public static class Factory
            {
                public static object Get<T>() => new Node<T>();
            }
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public static class Cache
            {
                public static object Get() => Factory.Get<long>();
            }
            """, "_ = Cache.Get();", 1);
    }

    [TestMethod]
    public void AnalysisExclusion_SharedMethodSpec_FromUnexcludedCallerIsPreserved()
    {
        AssertExclusion("""
            public static class Factory
            {
                public static object Get<T>() => new Node<T>();
            }
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public static class Cache
            {
                public static object Get() => Factory.Get<long>();
            }
            """, "_ = Cache.Get(); _ = Factory.Get<long>();", 2);
    }

    [TestMethod]
    public void AnalysisExclusion_GenericArrayBody_IsExcluded()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public static class Cache
            {
                public static object Get<T>() => new Node<T>[1];
            }
            """, "_ = Cache.Get<long>();", 1);
    }

    [TestMethod]
    public void AnalysisExclusion_CallerVisibleArraySignature_IsPreserved()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public static class Cache
            {
                public static Node<long>[] Get() => throw new NotSupportedException();
            }
            """, "_ = Cache.Get();", 3);
    }

    [TestMethod]
    public void AnalysisExclusion_GenericReceiverArraySignature_IsPreserved()
    {
        AssertExclusion("""
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache<T>
            {
                public T[] Get() => throw new NotSupportedException();
            }
            """, "_ = new Cache<Node<long>>().Get();", 3);
    }

    [TestMethod]
    public void AnalysisExclusion_InheritedMemberReferenceSignature_IsPreserved()
    {
        string directory = Directory.CreateTempSubdirectory("InteropInheritedExclusionTest_").FullName;
        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                    using Windows.Foundation;
                    using WindowsRuntime.InteropServices;
                    namespace Recursion;
                    public sealed class Node<T> : IStringable
                    {
                        public override string ToString() => "node";
                    }
                    [WindowsRuntimeExcludeFromInteropAnalysis]
                    public class Cache
                    {
                        public Node<long> Get() => throw new System.NotSupportedException();
                    }
                    public sealed class Derived : Cache;
                    public static class Program
                    {
                        public static void Main()
                        {
                            _ = new Node<int>();
                            _ = new Derived().Get();
                        }
                    }
                    """],
                Path.Combine(directory, "App.dll"),
                outputKind: OutputKind.ConsoleApplication);
            var module = AsmResolver.DotNet.ModuleDefinition.FromFile(app);
            var derived = module.GetAllTypes().Single(type => type.Name == "Derived");
            var main = module.GetAllTypes().Single(type => type.Name == "Program").Methods.Single(method => method.Name == "Main");
            var call = main.CilMethodBody!.Instructions.Single(instruction =>
                instruction.Operand is AsmResolver.DotNet.IMethodDescriptor member && member.Name == "Get");
            var reference = (AsmResolver.DotNet.IMethodDescriptor)call.Operand!;
            call.Operand = new AsmResolver.DotNet.MemberReference(
                parent: derived,
                name: reference.Name,
                signature: (AsmResolver.DotNet.Signatures.MethodSignature)reference.Signature!);
            module.Write(app);
            (int exitCode, string log) = RunGenerator(directory, app);
            Assert.AreEqual(0, exitCode, log);
            var types = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            Assert.HasCount(2, types.Where(name => name.StartsWith("Recursion.Node`1[")), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void AnalysisExclusion_IndependentAttributeTypeSpec_IsPreserved()
    {
        AssertExclusion("""
            public sealed class KeepAttribute<T> : Attribute;
            [Keep<Node<long>>]
            public sealed class Marker;
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache
            {
                private readonly Node<long> hidden = new();
            }
            """, "_ = new Cache();", 2);
    }

    [TestMethod]
    public void AnalysisExclusion_AssemblyReferenceAttributeRoot_IsPreserved()
    {
        string directory = Directory.CreateTempSubdirectory("InteropAttributeExclusionTest_").FullName;
        try
        {
            string app = ProjectionWriterRunner.CompileSources(
                ["""
                    using System;
                    using Windows.Foundation;
                    using WindowsRuntime.InteropServices;
                    namespace Recursion;
                    public sealed class Node<T> : IStringable
                    {
                        public override string ToString() => "node";
                    }
                    public sealed class KeepAttribute<T> : Attribute;
                    [Keep<Node<long>>]
                    public sealed class Marker;
                    [WindowsRuntimeExcludeFromInteropAnalysis]
                    public sealed class Cache
                    {
                        private readonly object hidden = new KeepAttribute<Node<long>>();
                    }
                    public static class Program
                    {
                        public static void Main()
                        {
                            _ = new Node<int>();
                            _ = new Cache();
                        }
                    }
                    """],
                Path.Combine(directory, "App.dll"),
                outputKind: OutputKind.ConsoleApplication);
            var module = AsmResolver.DotNet.ModuleDefinition.FromFile(app);
            var owner = module.GetAllTypes().Single(type => type.Name == "Marker");
            var attribute = owner.CustomAttributes.Single();
            owner.CustomAttributes.Remove(attribute);
            module.AssemblyReferences[0].CustomAttributes.Add(attribute);
            module.Write(app);
            (int exitCode, string log) = RunGenerator(directory, app);
            Assert.AreEqual(0, exitCode, log);
            var types = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            Assert.HasCount(2, types.Where(name => name.StartsWith("Recursion.Node`1[")), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void AnalysisExclusion_StaticInterfaceDispatch_DoesNotExpandExcludedImplementation()
    {
        AssertExclusion("""
            public interface IFactory<T>
            {
                static abstract object Create();
            }
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache<T> : IFactory<T>
            {
                public static object Create() => new Node<T>();
            }
            public static class Caller
            {
                public static object Get<TFactory, T>() where TFactory : IFactory<T> => TFactory.Create();
            }
            """, "_ = Caller.Get<Cache<long>, long>();", 1);
    }

    [TestMethod]
    public void AnalysisExclusion_UnexcludedInheritedDefaultBody_IsPreserved()
    {
        AssertExclusion("""
            public interface IFactory<T>
            {
                object Create() => new Node<T>();
            }
            [WindowsRuntimeExcludeFromInteropAnalysis]
            public sealed class Cache<T> : IFactory<T>;
            """, "IFactory<long> factory = new Cache<long>(); _ = factory.Create();", 2);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AnalysisExclusion_CrossModuleCalls_RespectExclusionAndIndependentUse(bool independentUse)
    {
        string directory = Directory.CreateTempSubdirectory("InteropAnalysisExclusionTest_").FullName;
        try
        {
            string library = ProjectionWriterRunner.CompileSources(
                ["""
                    using Windows.Foundation;
                    using WindowsRuntime.InteropServices;
                    namespace Recursion;
                    public sealed class Node<T> : IStringable
                    {
                        public override string ToString() => "node";
                    }
                    public static class Factory
                    {
                        public static object Create<T>() => new Node<T>();
                    }
                    [WindowsRuntimeExcludeFromInteropAnalysis]
                    public static class Cache
                    {
                        public static object Get() => Factory.Create<long>();
                    }
                    """],
                Path.Combine(directory, "CacheLibrary.dll"),
                outputKind: OutputKind.DynamicallyLinkedLibrary);
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                    using Recursion;
                    public static class Program
                    {
                        public static void Main()
                        {
                            _ = new Node<int>();
                            _ = Cache.Get();
                            {{(independentUse ? "_ = Factory.Create<long>();" : "")}}
                        }
                    }
                    """],
                Path.Combine(directory, "App.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [library]);
            (int exitCode, string log) = RunGenerator(directory, app, additionalReferences: [library], maxDegreesOfParallelism: 4);
            Assert.AreEqual(0, exitCode, log);
            var types = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            string[] nodes = [.. types.Where(name => name.StartsWith("Recursion.Node`1["))];
            Assert.HasCount(independentUse ? 2 : 1, nodes, log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("Node<long>", false, 2)]
    [DataRow("Node<long>[]", false, 3)]
    [DataRow("Node<T>", true, 2)]
    public void AnalysisExclusion_CrossModuleCallerVisibleSignatures_ArePreserved(string returnType, bool generic, int expectedTypes)
    {
        string directory = Directory.CreateTempSubdirectory("InteropSignatureExclusionTest_").FullName;
        try
        {
            string library = ProjectionWriterRunner.CompileSources(
                [$$"""
                    using Windows.Foundation;
                    using WindowsRuntime.InteropServices;
                    namespace Recursion;
                    public sealed class Node<T> : IStringable
                    {
                        public override string ToString() => "node";
                    }
                    [WindowsRuntimeExcludeFromInteropAnalysis]
                    public static class Cache{{(generic ? "<T>" : "")}}
                    {
                        private static readonly Node<double> hidden = new();
                        public static {{returnType}} Get() => throw new System.NotSupportedException();
                    }
                    """],
                Path.Combine(directory, "CacheLibrary.dll"),
                outputKind: OutputKind.DynamicallyLinkedLibrary);
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                    using Recursion;
                    public static class Program
                    {
                        public static void Main()
                        {
                            _ = new Node<int>();
                            _ = Cache{{(generic ? "<long>" : "")}}.Get();
                        }
                    }
                    """],
                Path.Combine(directory, "App.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [library]);
            (int exitCode, string log) = RunGenerator(directory, app, additionalReferences: [library], maxDegreesOfParallelism: 4);
            Assert.AreEqual(0, exitCode, log);
            var types = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            Assert.HasCount(expectedTypes, types.Where(name => name.StartsWith("Recursion.Node`1[")), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AnalysisExclusion_UnrelatedMissingAssemblySignature_DoesNotContribute(bool exclude)
    {
        string directory = Directory.CreateTempSubdirectory("InteropMissingAssetExclusionTest_").FullName;
        try
        {
            string nodes = ProjectionWriterRunner.CompileSources(
                ["""
                    using Windows.Foundation;
                    namespace Recursion;
                    public sealed class Node<T> : IStringable
                    {
                        public override string ToString() => "node";
                    }
                    """],
                Path.Combine(directory, "Nodes.dll"),
                outputKind: OutputKind.DynamicallyLinkedLibrary);
            string missing = ProjectionWriterRunner.CompileSources(
                ["""
                    namespace MissingAsset;
                    public static class Factory
                    {
                        public static Recursion.Node<long> Get() => throw new System.NotSupportedException();
                    }
                    """],
                Path.Combine(directory, "MissingAsset.dll"),
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                additionalReferences: [nodes]);
            string app = ProjectionWriterRunner.CompileSources(
                [$$"""
                    using WindowsRuntime.InteropServices;
                    {{(exclude ? "[WindowsRuntimeExcludeFromInteropAnalysis]" : "")}}
                    public sealed class Cache;
                    public static class Program
                    {
                        public static void Main()
                        {
                            _ = new Recursion.Node<int>();
                            _ = new Cache();
                            _ = MissingAsset.Factory.Get();
                        }
                    }
                    """],
                Path.Combine(directory, "App.dll"),
                outputKind: OutputKind.ConsoleApplication,
                additionalReferences: [nodes, missing]);
            File.Delete(missing);
            (int exitCode, string log) = RunGenerator(directory, app, additionalReferences: [nodes]);
            Assert.AreEqual(0, exitCode, log);
            var types = GetComWrappersTypeAssociations(Path.Combine(directory, "WinRT.Interop.dll"));
            Assert.HasCount(1, types.Where(name => name.StartsWith("Recursion.Node`1[")), log);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void AssertExclusion(string helpers, string body, int expectedTypes)
    {
        AssertGeneration($$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Windows.Foundation;
            using WindowsRuntime.InteropServices;
            namespace Recursion;
            public sealed class Node<T> : IStringable
            {
                public override string ToString() => "node";
            }
            {{helpers}}
            public static class Program
            {
                public static void Main()
                {
                    _ = new Node<int>();
                    {{body}}
                }
            }
            """, expectedTypes);
    }
}
