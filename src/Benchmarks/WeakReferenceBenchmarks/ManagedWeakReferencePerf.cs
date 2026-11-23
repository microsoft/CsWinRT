using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace WeakReferenceBenchmarks;

/// <summary>
/// Compares the previous <c>ComWrappers</c>-based <c>IWeakReference</c> implementation with the thin native object,
/// for the operations a native caller performs on a weak reference to a managed object.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public unsafe class ManagedWeakReferencePerf
{
    private object _target = null!;
    private void* _legacy;
    private void* _thin;
    private void* _legacyDead;
    private void* _thinDead;
    private Guid _iidUnknown;
    private Guid _iidWeakReference;

    [GlobalSetup]
    public void Setup()
    {
        _target = new object();
        _iidUnknown = Guids.IUnknown;
        _iidWeakReference = Guids.IWeakReference;

        _legacy = LegacyManagedWeakReference.Create(_target);
        _thin = ThinManagedWeakReference.Create(_target);

        CreateDead(out _legacyDead, out _thinDead);

        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateDead(out void* legacy, out void* thin)
    {
        object temp = new();

        legacy = LegacyManagedWeakReference.Create(temp);
        thin = ThinManagedWeakReference.Create(temp);
    }

    [BenchmarkCategory("GetWeakReference (create + release)")]
    [Benchmark(Baseline = true)]
    public void Create_Legacy()
    {
        void* weakReference = LegacyManagedWeakReference.Create(_target);

        _ = Native.Release(weakReference);
    }

    [BenchmarkCategory("GetWeakReference (create + release)")]
    [Benchmark]
    public void Create_Thin()
    {
        void* weakReference = ThinManagedWeakReference.Create(_target);

        _ = Native.Release(weakReference);
    }

    [BenchmarkCategory("Resolve (target alive)")]
    [Benchmark(Baseline = true)]
    public void Resolve_Legacy()
    {
        void* result;

        fixed (Guid* iid = &_iidUnknown)
        {
            _ = Native.Resolve(_legacy, iid, &result);
        }

        _ = Native.Release(result);
    }

    [BenchmarkCategory("Resolve (target alive)")]
    [Benchmark]
    public void Resolve_Thin()
    {
        void* result;

        fixed (Guid* iid = &_iidUnknown)
        {
            _ = Native.Resolve(_thin, iid, &result);
        }

        _ = Native.Release(result);
    }

    [BenchmarkCategory("Resolve (target collected)")]
    [Benchmark(Baseline = true)]
    public void ResolveDead_Legacy()
    {
        void* result;

        fixed (Guid* iid = &_iidUnknown)
        {
            _ = Native.Resolve(_legacyDead, iid, &result);
        }
    }

    [BenchmarkCategory("Resolve (target collected)")]
    [Benchmark]
    public void ResolveDead_Thin()
    {
        void* result;

        fixed (Guid* iid = &_iidUnknown)
        {
            _ = Native.Resolve(_thinDead, iid, &result);
        }
    }

    [BenchmarkCategory("QueryInterface (IWeakReference) + release")]
    [Benchmark(Baseline = true)]
    public void QueryInterface_Legacy()
    {
        _ = Native.QueryInterface(_legacy, in _iidWeakReference, out void* result);
        _ = Native.Release(result);
    }

    [BenchmarkCategory("QueryInterface (IWeakReference) + release")]
    [Benchmark]
    public void QueryInterface_Thin()
    {
        _ = Native.QueryInterface(_thin, in _iidWeakReference, out void* result);
        _ = Native.Release(result);
    }

    [BenchmarkCategory("AddRef + Release")]
    [Benchmark(Baseline = true)]
    public void AddRefRelease_Legacy()
    {
        _ = Native.AddRef(_legacy);
        _ = Native.Release(_legacy);
    }

    [BenchmarkCategory("AddRef + Release")]
    [Benchmark]
    public void AddRefRelease_Thin()
    {
        _ = Native.AddRef(_thin);
        _ = Native.Release(_thin);
    }

    [BenchmarkCategory("Typical flow (create, resolve once, release)")]
    [Benchmark(Baseline = true)]
    public void Flow_Legacy()
    {
        void* weakReference = LegacyManagedWeakReference.Create(_target);
        void* result;

        fixed (Guid* iid = &_iidUnknown)
        {
            _ = Native.Resolve(weakReference, iid, &result);
        }

        _ = Native.Release(result);
        _ = Native.Release(weakReference);
    }

    [BenchmarkCategory("Typical flow (create, resolve once, release)")]
    [Benchmark]
    public void Flow_Thin()
    {
        void* weakReference = ThinManagedWeakReference.Create(_target);
        void* result;

        fixed (Guid* iid = &_iidUnknown)
        {
            _ = Native.Resolve(weakReference, iid, &result);
        }

        _ = Native.Release(result);
        _ = Native.Release(weakReference);
    }
}
