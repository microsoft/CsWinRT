// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.IO;
using AsmResolver.PE.DotNet.Cil;
using InteropGeneratorTest.Helpers;

namespace InteropGeneratorTest;

/// <summary>
/// Round-trip tests for the runtime class name hierarchy lookup: the data is produced by the real interop generator,
/// and it is read back by the real runtime implementation (<c>WindowsRuntimeTypeHierarchy</c> in <c>WinRT.Runtime.dll</c>).
/// </summary>
[TestClass]
public sealed class Test_TypeHierarchyLookup
{
    /// <summary>
    /// The runtime diagnostic for malformed hierarchy data.
    /// </summary>
    private const string MalformedDataMessage = "The RVA data for the type hierarchy lookup is malformed.";

    /// <summary>
    /// Exercises narrow records, collisions, empty buckets, and ordinal name comparisons.
    /// </summary>
    [TestMethod]
    public async Task NarrowLookup_ResolvesEveryKeyAndRejectsNearMisses()
    {
        Dictionary<string, string> entries = CreateMixedCatalog();
        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(entries),
            sdkReferenceRewriter: RenameMixedCatalogTypes);

        // Apply the same renames to the expected entries
        entries = entries.ToDictionary(pair => RenameMixedCatalogName(pair.Key), pair => RenameMixedCatalogName(pair.Value));

        string interopPath = await runner.GenerateAsync("narrow");
        LookupData data = LookupData.Read(interopPath);

        Assert.IsTrue(data.UsesNarrowOffsets);
        AssertGeneratedCodeIsDataOnly(interopPath);

        HierarchyLookup lookup = HierarchyLookup.Load(interopPath);

        AssertAllEntriesResolve(lookup, entries);

        // The catalog must actually cover collisions, empty buckets, and negative hashes
        Dictionary<int, List<string>> chains = entries.Keys.GroupBy(key => data.GetBucket(key)).ToDictionary(group => group.Key, group => group.ToList());

        Assert.IsTrue(chains.Values.Any(chain => chain.Count >= 2), "No collisions in the catalog.");
        Assert.IsTrue(chains.Count < data.BucketCount, "No empty buckets in the catalog.");
        Assert.IsTrue(entries.Keys.Any(key => ComputeHash(key) < 0), "No negative hashes in the catalog.");

        // Names that are not keys (including values that are not themselves keys, and names out of the length range)
        string[] misses =
        [
            "",
            "A",
            "Hierarchy.Root",
            "Alpha.Butto",
            "Alpha.Buttonx",
            "alpha.Button",
            "Beta.Buttons",
            "XUnqualified",
            "Unqualifie",
            "Hierarchy.Trailing",
            "Hierarchy.Trailing..",
            new string('x', 4096)
        ];

        foreach (string miss in misses)
        {
            Assert.IsFalse(lookup.TryGetBase(miss, out _), $"Unexpected match for '{miss}'.");
        }

        // A name in an empty bucket must stop at the shared empty chain
        string emptyBucketMiss = FindName(data, static i => $"Empty.Miss{i}", bucket => !chains.ContainsKey(bucket));

        Assert.IsFalse(lookup.TryGetBase(emptyBucketMiss, out _));

        // Names in the same chain as a key, with the same length and suffix but a different prefix (and vice versa)
        string samePrefixBucket = FindName(data, static i => $"Alpha.{i:D6}", bucket => chains.TryGetValue(bucket, out List<string>? chain) && chain.Contains("Alpha.Button"));
        string sameSuffixBucket = FindName(data, static i => $"{i:D5}.Button", bucket => chains.TryGetValue(bucket, out List<string>? chain) && chain.Contains("Alpha.Button"));

        Assert.IsFalse(lookup.TryGetBase(samePrefixBucket, out _));
        Assert.IsFalse(lookup.TryGetBase(sameSuffixBucket, out _));
    }

    /// <summary>
    /// Checks that narrow addressing constrains chain starts rather than the total data size.
    /// </summary>
    [TestMethod]
    public async Task NarrowLookup_LastChainCanExtendPastNarrowOffsetRange()
    {
        // Two keys with very long names: every chain starts within the narrow range, but the data extends past it
        Dictionary<string, string> entries = new()
        {
            [$"Big.{new string('A', 40_000)}"] = "Hierarchy.Root",
            [$"Big.{new string('B', 40_000)}"] = "Hierarchy.Root"
        };

        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(new Dictionary<string, string> { ["Big.A"] = "Hierarchy.Root", ["Big.B"] = "Hierarchy.Root" }),
            sdkReferenceRewriter: module =>
            {
                foreach (TypeDefinition type in module.TopLevelTypes.Where(static type => type.Namespace == "Big"))
                {
                    type.Name = new string(type.Name!.Value[0], 40_000);
                }
            });

        string interopPath = await runner.GenerateAsync("narrow-large");
        LookupData data = LookupData.Read(interopPath);

        Assert.IsTrue(data.UsesNarrowOffsets);
        Assert.IsTrue(data.KeysSize > ushort.MaxValue * sizeof(char), $"Unexpected keys size: {data.KeysSize}.");

        AssertAllEntriesResolve(HierarchyLookup.Load(interopPath), entries);
    }

    /// <summary>
    /// Exercises the inclusive upper bound for narrow chain offsets.
    /// </summary>
    /// <param name="chainOffset">The exact chain offset, in 16-bit units.</param>
    /// <param name="expectedNarrowOffsets">The expected offset representation.</param>
    [TestMethod]
    [DataRow(65535, true)]
    [DataRow(65536, false)]
    public async Task Lookup_SelectsWidthAtExactChainStartBoundary(int chainOffset, bool expectedNarrowOffsets)
    {
        // One prefix occupies its length plus two headers (the shared zero and the prefix length)
        string ns = new('N', chainOffset - 3);
        string key = $"{ns}.D";
        Dictionary<string, string> entries = new() { [key] = "Hierarchy.Root" };

        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(new Dictionary<string, string> { ["Boundary.D"] = "Hierarchy.Root" }),
            sdkReferenceRewriter: module => module.TopLevelTypes.Single(static type => type.FullName == "Boundary.D").Namespace = ns);

        string interopPath = await runner.GenerateAsync("boundary");
        LookupData data = LookupData.Read(interopPath);

        Assert.AreEqual(expectedNarrowOffsets, data.UsesNarrowOffsets);
        Assert.AreEqual(chainOffset, LookupData.ReadChainOffset(interopPath, key));
        Assert.IsTrue(data.KeysSize > ushort.MaxValue * sizeof(char));

        AssertAllEntriesResolve(HierarchyLookup.Load(interopPath), entries);
    }

    /// <summary>
    /// Preserves the full ushort suffix-length range for unqualified names.
    /// </summary>
    [TestMethod]
    public async Task NarrowLookup_MaximumLengthUnqualifiedNameResolves()
    {
        string key = new('A', ushort.MaxValue);
        Dictionary<string, string> entries = new() { [key] = "Hierarchy.Root" };

        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(new Dictionary<string, string> { ["Derived"] = "Hierarchy.Root" }),
            sdkReferenceRewriter: module => module.TopLevelTypes.Single(static type => type.FullName == "Derived").Name = key);

        string interopPath = await runner.GenerateAsync("maximum-suffix");
        LookupData data = LookupData.Read(interopPath);

        Assert.IsTrue(data.UsesNarrowOffsets);
        Assert.AreEqual(1, LookupData.ReadChainOffset(interopPath, key));
        Assert.IsTrue(data.KeysSize > ushort.MaxValue * sizeof(char));

        AssertAllEntriesResolve(HierarchyLookup.Load(interopPath), entries);
    }

    /// <summary>
    /// Checks that width selection uses candidate narrow headers rather than wide headers.
    /// </summary>
    [TestMethod]
    public async Task NarrowLookup_UsesNarrowRecordSizesForWidthDecision()
    {
        // The long key is in bucket 1 and 'X' is in bucket 2 of the three-bucket table.
        // Its narrow record puts the next chain at 65535 units; a wide header would put it at 65536.
        string longKey = new('A', ushort.MaxValue - 5);
        Dictionary<string, string> entries = new()
        {
            [longKey] = "Hierarchy.Root",
            ["X"] = "Hierarchy.Root"
        };

        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(new Dictionary<string, string> { ["Long"] = "Hierarchy.Root", ["X"] = "Hierarchy.Root" }),
            sdkReferenceRewriter: module => module.TopLevelTypes.Single(static type => type.FullName == "Long").Name = longKey);

        string interopPath = await runner.GenerateAsync("narrow-prelayout");
        LookupData data = LookupData.Read(interopPath);

        Assert.AreEqual(3, data.BucketCount);
        Assert.IsTrue(data.UsesNarrowOffsets);
        Assert.AreEqual(65_535, LookupData.ReadChainOffset(interopPath, "X"));

        AssertAllEntriesResolve(HierarchyLookup.Load(interopPath), entries);
    }

    /// <summary>
    /// Exercises wide chain and prefix offsets against the real runtime.
    /// </summary>
    [TestMethod]
    public async Task WideLookup_ResolvesEveryKey()
    {
        // Enough long namespaces that the prefixes alone (which come before all chains) exceed the narrow offset range
        const int count = 80;

        Dictionary<string, string> sourceEntries = Enumerable.Range(0, count).ToDictionary(
            static i => $"Wide{i}.C{i}",
            static i => i % 2 == 0 ? "Hierarchy.Root" : "Hierarchy.Middle");

        sourceEntries.Add("Hierarchy.Middle", "Hierarchy.Root");

        Dictionary<string, string> entries = sourceEntries.ToDictionary(
            static pair => pair.Key.StartsWith("Wide", StringComparison.Ordinal) ? $"{GetWideNamespace(int.Parse(pair.Key.AsSpan(4, pair.Key.IndexOf('.') - 4), provider: null))}.{pair.Key[(pair.Key.IndexOf('.') + 1)..]}" : pair.Key,
            static pair => pair.Value);

        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(sourceEntries),
            sdkReferenceRewriter: RenameWideCatalogTypes);

        string interopPath = await runner.GenerateAsync("wide");
        LookupData data = LookupData.Read(interopPath);

        Assert.IsFalse(data.UsesNarrowOffsets);
        Assert.AreEqual(data.BucketCount * sizeof(int), data.BucketsSize);
        AssertGeneratedCodeIsDataOnly(interopPath);

        // Sanity check: the last prefix must start past the narrow offset range (in 16-bit units)
        int[] prefixSizes = entries.Keys
            .Select(static key => key[..(key.LastIndexOf('.') + 1)])
            .Distinct(StringComparer.Ordinal)
            .Select(static prefix => 1 + prefix.Length)
            .ToArray();

        Assert.IsTrue(1 + prefixSizes.Sum() - prefixSizes.Max() > ushort.MaxValue, "The prefixes don't exceed the narrow offset range.");

        HierarchyLookup lookup = HierarchyLookup.Load(interopPath);

        AssertAllEntriesResolve(lookup, entries);

        // Also check near misses against the long prefixes
        Assert.IsFalse(lookup.TryGetBase($"{GetWideNamespace(3)}.C4", out _));
        Assert.IsFalse(lookup.TryGetBase($"{GetWideNamespace(3)}X.C3", out _));
    }

    /// <summary>
    /// Exercises the bucket-search limit and catalogs beyond it.
    /// </summary>
    /// <param name="count">The number of derived types to generate.</param>
    [TestMethod]
    [DataRow(1931)]
    [DataRow(1932)]
    [DataRow(2400)]
    public async Task LargeCatalog_SelectsValidBucketCount(int count)
    {
        Dictionary<string, string> entries = Enumerable.Range(0, count).ToDictionary(
            static i => $"Large.Type{i}",
            static _ => "Hierarchy.Root");

        using InteropGeneratorRunner runner = new(sdkReferenceSource: CreateProjectionSource(entries));

        string interopPath = await runner.GenerateAsync("large");
        LookupData data = LookupData.Read(interopPath);

        Assert.IsTrue(data.BucketCount >= count, $"Unexpected bucket count: {data.BucketCount}.");

        AssertAllEntriesResolve(HierarchyLookup.Load(interopPath), entries);
    }

    /// <summary>
    /// Checks that input order and parallelism do not change the generated assembly.
    /// </summary>
    [TestMethod]
    public async Task Lookup_IsDeterministic()
    {
        Dictionary<string, string> entries = CreateMixedCatalog();
        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(entries),
            sdkReferenceRewriter: RenameMixedCatalogTypes);

        string first = await runner.GenerateAsync("first");
        string second = await runner.GenerateAsync("second", reverseInputs: true, parallelism: 4);

        CollectionAssert.AreEqual(File.ReadAllBytes(first), File.ReadAllBytes(second));
    }

    /// <summary>
    /// Exercises bucket seek bounds and checked unit scaling.
    /// </summary>
    [TestMethod]
    public async Task MalformedBuckets_ThrowBadImageFormatException()
    {
        Dictionary<string, string> narrowEntries = CreateMixedCatalog();
        using InteropGeneratorRunner narrowRunner = new(
            sdkReferenceSource: CreateProjectionSource(narrowEntries),
            sdkReferenceRewriter: RenameMixedCatalogTypes);

        string narrowPath = await narrowRunner.GenerateAsync("narrow");
        LookupData narrowData = LookupData.Read(narrowPath);

        // Offsets past the end of the keys data
        AssertMalformed(narrowRunner, narrowPath, "narrow-out-of-range", "Hierarchy.Middle", Enumerable.Repeat(ushort.MaxValue, narrowData.BucketCount).SelectMany(BitConverter.GetBytes).ToArray());

        // Offsets exactly at the end of the keys data (ie. a truncated chain)
        AssertMalformed(narrowRunner, narrowPath, "narrow-truncated", "Hierarchy.Middle", Enumerable.Repeat((ushort)(narrowData.KeysSize / sizeof(char)), narrowData.BucketCount).SelectMany(BitConverter.GetBytes).ToArray());

        Dictionary<string, string> wideEntries = Enumerable.Range(0, 80).ToDictionary(static i => $"Wide{i}.C{i}", static _ => "Hierarchy.Root");
        using InteropGeneratorRunner wideRunner = new(
            sdkReferenceSource: CreateProjectionSource(wideEntries),
            sdkReferenceRewriter: RenameWideCatalogTypes);

        string widePath = await wideRunner.GenerateAsync("wide");
        LookupData wideData = LookupData.Read(widePath);
        string wideKey = $"{GetWideNamespace(0)}.C0";

        Assert.IsFalse(wideData.UsesNarrowOffsets);

        // Offsets that would wrap around to a valid offset (2 bytes) if they were scaled before being validated
        AssertMalformed(wideRunner, widePath, "wide-overflow", wideKey, Enumerable.Repeat(unchecked((int)0x8000_0001), wideData.BucketCount).SelectMany(BitConverter.GetBytes).ToArray());

        // Negative offsets
        AssertMalformed(wideRunner, widePath, "wide-negative", wideKey, Enumerable.Repeat(-1, wideData.BucketCount).SelectMany(BitConverter.GetBytes).ToArray());

        static void AssertMalformed(InteropGeneratorRunner runner, string interopPath, string name, string runtimeClassName, byte[] buckets)
        {
            string patchedPath = LookupData.PatchBuckets(interopPath, Path.Combine(runner.Root, name), buckets);
            HierarchyLookup lookup = HierarchyLookup.Load(patchedPath);

            BadImageFormatException exception = Assert.ThrowsExactly<BadImageFormatException>(() => lookup.TryGetBase(runtimeClassName, out _));

            Assert.AreEqual(MalformedDataMessage, exception.Message);
        }
    }

    /// <summary>
    /// Exercises suffix bounds and prefix seek bounds in both offset formats.
    /// </summary>
    /// <param name="wide">Whether to generate wide offsets.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MalformedKeys_ThrowBadImageFormatException(bool wide)
    {
        string ns = wide ? new string('N', ushort.MaxValue - 2) : "Boundary";
        string key = $"{ns}.D";

        using InteropGeneratorRunner runner = new(
            sdkReferenceSource: CreateProjectionSource(new Dictionary<string, string> { ["Boundary.D"] = "Hierarchy.Root" }),
            sdkReferenceRewriter: module => module.TopLevelTypes.Single(static type => type.FullName == "Boundary.D").Namespace = ns);

        string interopPath = await runner.GenerateAsync("keys");
        LookupData data = LookupData.Read(interopPath);
        int recordOffset = LookupData.ReadChainOffset(interopPath, key) * sizeof(char);

        Assert.AreEqual(!wide, data.UsesNarrowOffsets);

        // The suffix is at the end of the data, so a maximal length cannot be read
        AssertMalformed("suffix-length", bytes => BitConverter.GetBytes(ushort.MaxValue).CopyTo(bytes, recordOffset));

        // The exact end is a valid seek position, but no prefix length can be read there
        byte[] truncatedPrefixOffset = wide
            ? BitConverter.GetBytes(data.KeysSize / sizeof(char))
            : BitConverter.GetBytes(checked((ushort)(data.KeysSize / sizeof(char))));

        AssertMalformed("prefix-truncated", bytes => truncatedPrefixOffset.CopyTo(bytes, recordOffset + 2 * sizeof(ushort)));

        // A wide prefix offset must be checked before multiplication, just like a bucket offset
        byte[] invalidPrefixOffset = wide
            ? BitConverter.GetBytes(unchecked((int)0x8000_0001))
            : BitConverter.GetBytes(ushort.MaxValue);

        AssertMalformed("prefix-out-of-range", bytes => invalidPrefixOffset.CopyTo(bytes, recordOffset + 2 * sizeof(ushort)));

        void AssertMalformed(string name, Action<byte[]> patch)
        {
            string patchedPath = LookupData.PatchKeys(interopPath, Path.Combine(runner.Root, name), patch);
            HierarchyLookup lookup = HierarchyLookup.Load(patchedPath);

            BadImageFormatException exception = Assert.ThrowsExactly<BadImageFormatException>(() => lookup.TryGetBase(key, out _));

            Assert.AreEqual(MalformedDataMessage, exception.Message);
        }
    }

    /// <summary>
    /// Renames the namespaces of types in the wide catalogs to very long namespaces.
    /// </summary>
    private static void RenameWideCatalogTypes(ModuleDefinition module)
    {
        foreach (TypeDefinition type in module.TopLevelTypes.Where(static type => type.Namespace?.Value.StartsWith("Wide", StringComparison.Ordinal) is true))
        {
            type.Namespace = GetWideNamespace(int.Parse(type.Namespace!.Value.AsSpan(4), provider: null));
        }
    }

    /// <summary>
    /// Creates a catalog with multi-level hierarchies, shared prefixes and suffixes, unqualified names, and enough keys to have collisions.
    /// </summary>
    private static Dictionary<string, string> CreateMixedCatalog()
    {
        Dictionary<string, string> entries = new(StringComparer.Ordinal)
        {
            ["Hierarchy.Middle"] = "Hierarchy.Root",
            ["Hierarchy.Leaf"] = "Hierarchy.Middle",
            ["Hierarchy.Deepest"] = "Hierarchy.Leaf",
            ["Alpha.Button"] = "Hierarchy.Root",
            ["Alpha.Buttons"] = "Hierarchy.Middle",
            ["Beta.Button"] = "Hierarchy.Leaf",
            ["Unqualified"] = "Hierarchy.Root",
            ["Hierarchy.Trailing"] = "Hierarchy.Root",
            ["Unicode.Surrogate"] = "Hierarchy.Middle",
            ["Odd.Abc"] = "Hierarchy.Root",
            ["Odd.Abcde"] = "Hierarchy.Middle"
        };

        for (int i = 0; i < 60; i++)
        {
            entries.Add($"Filler{i % 3}.Type{i}", i % 2 == 0 ? "Hierarchy.Root" : "Hierarchy.Leaf");
        }

        return entries;
    }

    /// <summary>
    /// Renames the types in the mixed catalog to names that can't be declared in C#.
    /// </summary>
    private static void RenameMixedCatalogTypes(ModuleDefinition module)
    {
        foreach (TypeDefinition type in module.TopLevelTypes)
        {
            string renamed = RenameMixedCatalogName(type.FullName);

            if (renamed != type.FullName)
            {
                int separator = renamed.LastIndexOf('.', renamed.Length - 2);

                type.Namespace = renamed[..separator];
                type.Name = renamed[(separator + 1)..];
            }
        }
    }

    /// <summary>
    /// Maps names in the mixed catalog to their final names (with a trailing '.' character, and with supplementary characters).
    /// </summary>
    private static string RenameMixedCatalogName(string name)
    {
        return name switch
        {
            "Hierarchy.Trailing" => "Hierarchy.Trailing.",
            "Unicode.Surrogate" => "Unicode.\U0001D49C\U0001D4B7c",
            _ => name
        };
    }

    /// <summary>
    /// Gets a long namespace for the wide catalogs.
    /// </summary>
    private static string GetWideNamespace(int index)
    {
        return $"Wide{index:D3}.{new string((char)('a' + (index % 26)), 1000)}";
    }

    /// <summary>
    /// Creates the source for a reference projection with the specified derived types (the bases are declared automatically).
    /// </summary>
    private static string CreateProjectionSource(IReadOnlyDictionary<string, string> entries)
    {
        HashSet<string> types = new(entries.Keys, StringComparer.Ordinal) { "Hierarchy.Root" };

        foreach (string value in entries.Values)
        {
            _ = types.Add(value);
        }

        StringBuilder builder = new();

        _ = builder.AppendLine("[assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssembly]");

        foreach (string type in types.Order(StringComparer.Ordinal))
        {
            int separator = type.LastIndexOf('.');
            string baseType = entries.TryGetValue(type, out string? value) ? $" : global::{value}" : "";
            string declaration = $"[WindowsRuntime.WindowsRuntimeType] public class {type[(separator + 1)..]}{baseType} {{ }}";

            _ = separator < 0
                ? builder.AppendLine(declaration)
                : builder.AppendLine($"namespace {type[..separator]} {{ {declaration} }}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Checks that all entries resolve to the expected chain of base runtime class names.
    /// </summary>
    private static void AssertAllEntriesResolve(HierarchyLookup lookup, IReadOnlyDictionary<string, string> entries)
    {
        foreach ((string key, string value) in entries)
        {
            List<string> expected = [];

            for (string? current = value; current is not null; current = entries.GetValueOrDefault(current))
            {
                expected.Add(current);
            }

            Assert.IsTrue(lookup.TryGetBase(key, out List<string>? actual), $"No match for '{key}'.");
            CollectionAssert.AreEqual(expected, actual, $"Wrong base classes for '{key}'.");
        }
    }

    /// <summary>
    /// Checks that the generated lookup type only contains data and constants, with no control flow.
    /// </summary>
    private static void AssertGeneratedCodeIsDataOnly(string interopPath)
    {
        ModuleDefinition module = ModuleDefinition.FromFile(interopPath);
        TypeDefinition type = module.TopLevelTypes.Single(static type => type.FullName == "WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData");

        CollectionAssert.AreEquivalent(
            new[] { "get_Buckets", "get_Keys", "get_Values", "get_UsesNarrowOffsets", "get_MinimumKeyLength", "get_MaximumKeyLength" },
            type.Methods.Select(static method => method.Name!.Value).ToArray());

        foreach (MethodDefinition method in type.Methods)
        {
            Assert.IsTrue(method.IsAggressiveInlining, $"Getter '{method.Name}' should be inlineable.");
            Assert.IsTrue(
                method.CilMethodBody!.Instructions.All(static instruction => instruction.OpCode.FlowControl is CilFlowControl.Next or CilFlowControl.Call or CilFlowControl.Return),
                $"Unexpected control flow in '{method.Name}'.");
        }

        // No opaque call or computed value may replace a literal format or length getter
        foreach (string name in new[] { "get_UsesNarrowOffsets", "get_MinimumKeyLength", "get_MaximumKeyLength" })
        {
            MethodDefinition method = type.Methods.Single(method => method.Name == name);

            Assert.AreEqual(2, method.CilMethodBody!.Instructions.Count);
            Assert.IsTrue(method.CilMethodBody.Instructions[0].IsLdcI4());
            Assert.AreEqual(CilOpCodes.Ret, method.CilMethodBody.Instructions[1].OpCode);
        }
    }

    /// <summary>
    /// Finds a name matching a given bucket predicate.
    /// </summary>
    private static string FindName(LookupData data, Func<int, string> nameFactory, Func<int, bool> bucketPredicate)
    {
        for (int i = 0; i < 1_000_000; i++)
        {
            string name = nameFactory(i);

            if (bucketPredicate(data.GetBucket(name)))
            {
                return name;
            }
        }

        throw new AssertFailedException("No name found for the bucket predicate.");
    }

    /// <summary>
    /// The same hash function used by the generator and the runtime.
    /// </summary>
    private static int ComputeHash(string value)
    {
        uint hash = 2166136261u;

        foreach (char c in value)
        {
            hash = (c ^ hash) * 16777619;
        }

        return (int)hash;
    }

    /// <summary>
    /// Metadata about the lookup data in a generated interop assembly.
    /// </summary>
    private sealed class LookupData
    {
        /// <summary>
        /// Gets whether the generated offsets use ushort storage.
        /// </summary>
        public required bool UsesNarrowOffsets { get; init; }

        /// <summary>
        /// Gets the size of the bucket data, in bytes.
        /// </summary>
        public required int BucketsSize { get; init; }

        /// <summary>
        /// Gets the size of the key data, in bytes.
        /// </summary>
        public required int KeysSize { get; init; }

        /// <summary>
        /// Gets the number of bucket entries.
        /// </summary>
        public int BucketCount => BucketsSize / (UsesNarrowOffsets ? sizeof(ushort) : sizeof(int));

        /// <summary>
        /// Computes the bucket index of a runtime class name.
        /// </summary>
        public int GetBucket(string name) => (int)((uint)ComputeHash(name) % (uint)BucketCount);

        /// <summary>
        /// Reads the generated format flag and field sizes.
        /// </summary>
        public static LookupData Read(string interopPath)
        {
            ModuleDefinition module = ModuleDefinition.FromFile(interopPath);
            TypeDefinition type = module.TopLevelTypes.Single(static type => type.FullName == "WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData");
            CilOpCode usesNarrowOffsets = type.Methods.Single(static method => method.Name == "get_UsesNarrowOffsets").CilMethodBody!.Instructions[0].OpCode;

            Assert.IsTrue(usesNarrowOffsets == CilOpCodes.Ldc_I4_1 || usesNarrowOffsets == CilOpCodes.Ldc_I4_0);

            return new LookupData
            {
                UsesNarrowOffsets = usesNarrowOffsets == CilOpCodes.Ldc_I4_1,
                BucketsSize = GetRvaFieldSize(module, "TypeHierarchyLookupBuckets"),
                KeysSize = GetRvaFieldSize(module, "TypeHierarchyLookupKeys")
            };
        }

        /// <summary>
        /// Replaces bucket data without changing the field size or generated accessor.
        /// </summary>
        public static string PatchBuckets(string interopPath, string directory, byte[] buckets)
        {
            ModuleDefinition module = ModuleDefinition.FromFile(interopPath);
            FieldDefinition field = module.GetAllTypes().SelectMany(static type => type.Fields).Single(static field => field.Name == "TypeHierarchyLookupBuckets");

            Assert.AreEqual(field.FieldRva!.GetPhysicalSize(), (uint)buckets.Length);

            field.FieldRva = new DataSegment(buckets);

            string patchedPath = Path.Combine(Directory.CreateDirectory(directory).FullName, "WinRT.Interop.dll");

            module.Write(patchedPath);

            return patchedPath;
        }

        /// <summary>
        /// Reads an actual bucket entry rather than inferring its offset from the expected layout.
        /// </summary>
        public static int ReadChainOffset(string interopPath, string name)
        {
            LookupData data = Read(interopPath);
            ModuleDefinition module = ModuleDefinition.FromFile(interopPath);
            byte[] buckets = ReadRvaData(module, "TypeHierarchyLookupBuckets");
            int bucket = data.GetBucket(name);

            return data.UsesNarrowOffsets
                ? BitConverter.ToUInt16(buckets, bucket * sizeof(ushort))
                : BitConverter.ToInt32(buckets, bucket * sizeof(int));
        }

        /// <summary>
        /// Patches key data without changing the field size or generated accessor.
        /// </summary>
        public static string PatchKeys(string interopPath, string directory, Action<byte[]> patch)
        {
            ModuleDefinition module = ModuleDefinition.FromFile(interopPath);
            byte[] keys = ReadRvaData(module, "TypeHierarchyLookupKeys");

            patch(keys);

            FieldDefinition field = module.GetAllTypes().SelectMany(static type => type.Fields).Single(static field => field.Name == "TypeHierarchyLookupKeys");

            field.FieldRva = new DataSegment(keys);

            string patchedPath = Path.Combine(Directory.CreateDirectory(directory).FullName, "WinRT.Interop.dll");

            module.Write(patchedPath);

            return patchedPath;
        }

        /// <summary>
        /// Reads an emitted RVA field's bytes.
        /// </summary>
        private static byte[] ReadRvaData(ModuleDefinition module, string name)
        {
            FieldDefinition field = module.GetAllTypes().SelectMany(static type => type.Fields).Single(field => field.Name == name);
            IReadableSegment segment = (IReadableSegment)field.FieldRva!;
            BinaryStreamReader reader = segment.CreateReader(segment.Offset, segment.GetPhysicalSize());
            byte[] bytes = new byte[checked((int)segment.GetPhysicalSize())];

            _ = reader.ReadBytes(bytes);

            return bytes;
        }

        /// <summary>
        /// Gets the byte size of an emitted RVA field.
        /// </summary>
        private static int GetRvaFieldSize(ModuleDefinition module, string name)
        {
            FieldDefinition field = module.GetAllTypes().SelectMany(static type => type.Fields).Single(field => field.Name == name);

            return (int)field.FieldRva!.GetPhysicalSize();
        }
    }

    /// <summary>
    /// Invokes the real runtime lookup from <c>WinRT.Runtime.dll</c>, bound to a given generated <c>WinRT.Interop.dll</c>.
    /// </summary>
    private sealed class HierarchyLookup
    {
        /// <summary>
        /// The real runtime's initial hierarchy lookup.
        /// </summary>
        private readonly TryGetBaseRuntimeClassNameCallback tryGetBaseRuntimeClassName;

        /// <summary>
        /// The real runtime's subsequent parent traversal.
        /// </summary>
        private readonly TryGetNextBaseRuntimeClassNameCallback tryGetNextBaseRuntimeClassName;

        /// <summary>
        /// Binds delegates to the implementation loaded for this generated sidecar.
        /// </summary>
        private HierarchyLookup(Type type)
        {
            tryGetBaseRuntimeClassName = type.GetMethod("TryGetBaseRuntimeClassName", BindingFlags.Public | BindingFlags.Static)!.CreateDelegate<TryGetBaseRuntimeClassNameCallback>();
            tryGetNextBaseRuntimeClassName = type.GetMethod("TryGetNextBaseRuntimeClassName", BindingFlags.Public | BindingFlags.Static)!.CreateDelegate<TryGetNextBaseRuntimeClassNameCallback>();
        }

        /// <summary>
        /// Matches the runtime's initial lookup signature, including its span outputs.
        /// </summary>
        private delegate bool TryGetBaseRuntimeClassNameCallback(ReadOnlySpan<char> runtimeClassName, out ReadOnlySpan<char> baseRuntimeClassName, out int nextBaseRuntimeClassNameIndex);

        /// <summary>
        /// Matches the runtime's parent traversal signature.
        /// </summary>
        private delegate bool TryGetNextBaseRuntimeClassNameCallback(int baseRuntimeClassNameIndex, out ReadOnlySpan<char> baseRuntimeClassName, out int nextBaseRuntimeClassNameIndex);

        /// <summary>
        /// Loads an isolated runtime implementation bound to the specified sidecar.
        /// </summary>
        public static HierarchyLookup Load(string interopPath)
        {
            // Each generated assembly needs its own copy of the runtime, as the forwarded accessors are bound only once
            LookupLoadContext context = new(Path.GetFullPath(interopPath));
            Assembly runtime = context.LoadFromAssemblyPath(typeof(Windows.Foundation.IStringable).Assembly.Location);

            return new HierarchyLookup(runtime.GetType("WindowsRuntime.InteropServices.WindowsRuntimeTypeHierarchy", throwOnError: true)!);
        }

        /// <summary>
        /// Looks up a runtime class name and collects all its base names.
        /// </summary>
        public bool TryGetBase(string runtimeClassName, out List<string>? baseRuntimeClassNames)
        {
            if (!tryGetBaseRuntimeClassName(runtimeClassName, out ReadOnlySpan<char> baseRuntimeClassName, out int nextIndex))
            {
                baseRuntimeClassNames = null;

                return false;
            }

            baseRuntimeClassNames = [baseRuntimeClassName.ToString()];

            while (tryGetNextBaseRuntimeClassName(nextIndex, out baseRuntimeClassName, out nextIndex))
            {
                baseRuntimeClassNames.Add(baseRuntimeClassName.ToString());
            }

            return true;
        }
    }

    /// <summary>
    /// A load context that resolves <c>WinRT.Interop.dll</c> to a specific generated assembly (loaded from memory, so the file isn't locked).
    /// </summary>
    /// <param name="interopPath">The generated sidecar to bind.</param>
    private sealed class LookupLoadContext(string interopPath) : AssemblyLoadContext(name: null, isCollectible: false)
    {
        /// <inheritdoc/>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return assemblyName.Name switch
            {
                "WinRT.Interop" => LoadFromStream(new MemoryStream(File.ReadAllBytes(interopPath))),
                "WinRT.Runtime" => Assemblies.FirstOrDefault(static assembly => assembly.GetName().Name == "WinRT.Runtime"),
                _ => null
            };
        }
    }
}
