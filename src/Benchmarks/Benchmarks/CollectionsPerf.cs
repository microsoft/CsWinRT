using System.Collections.Generic;
using BenchmarkComponent;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    // Benchmarks for Windows.Foundation.Collections projections: bulk CopyTo, foreach
    // enumeration over IVector<T>/IMap<K,V>, and the read-only IVectorView<T>/IMapView<K,V>
    // side of those interfaces. Each scenario has a blittable-element variant (Int32) and an
    // object-element variant (WrappedClass) to isolate reference-type marshalling cost.
    [MemoryDiagnoser]
    public class CollectionsPerf
    {
        private const int BatchSize = 10_000;
        private const int VectorLen = 1024;
        private const int MapLen = 1024;
        private const int BulkCount = 100_000;

        private IList<int> vector;
        private IList<int> bulkVector;
        private int[] bulkBuffer;
        private int[] managedBulkVector;
        private ClassWithMarshalingRoutines instance;
        private IList<string> bulkStringVector;
        private string[] bulkStringBuffer;
        private IDictionary<string, int> stringMap;
        private IReadOnlyList<int> vectorView;
        private IReadOnlyDictionary<int, int> mapView;

        private IList<WrappedClass> objectVector;
        private IList<WrappedClass> bulkObjectVector;
        private WrappedClass[] bulkObjectBuffer;
        private IDictionary<string, WrappedClass> objectMap;
        private IReadOnlyList<WrappedClass> objectVectorView;
        private IReadOnlyDictionary<string, WrappedClass> objectMapView;

        [GlobalSetup]
        public void Setup()
        {
            instance = new();

            vector = instance.Items(VectorLen);
            bulkVector = instance.Items(BulkCount);
            bulkBuffer = new int[BulkCount];
            managedBulkVector = new int[BulkCount];
            for (int i = 0; i < BulkCount; i++)
            {
                managedBulkVector[i] = i;
            }
            // Will be uncommented once the TestWinRT change is done.
            // _ = instance.GetManyFromManagedList(managedBulkVector);
            bulkStringVector = instance.NewList();
            bulkStringBuffer = new string[BulkCount];
            for (int i = 0; i < BulkCount; i++)
            {
                bulkStringVector.Add(i.ToString());
            }
            stringMap = instance.StringMap(MapLen);
            vectorView = instance.ItemsView(VectorLen);
            mapView = instance.MapView(MapLen);

            objectVector = instance.ObjectItems(VectorLen);
            bulkObjectVector = instance.ObjectItems(BulkCount);
            bulkObjectBuffer = new WrappedClass[BulkCount];
            objectMap = instance.ObjectMap(MapLen);
            objectVectorView = instance.ObjectItemsView(VectorLen);
            objectMapView = instance.ObjectMapView(MapLen);
        }

        [Benchmark(OperationsPerInvoke = VectorLen)]
        public int IterateVector()
        {
            int sum = 0;
            foreach (int v in vector)
            {
                sum += v;
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = VectorLen)]
        public int IterateVectorObjects()
        {
            int sum = 0;
            foreach (WrappedClass v in objectVector)
            {
                sum += v.DefaultIntProperty;
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = BatchSize)]
        public int Vector()
        {
            int sum = 0;
            for (int i = 0; i < BatchSize; i++)
            {
                sum += vector[i % VectorLen];
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = BulkCount)]
        public void GetMany()
        {
            bulkVector.CopyTo(bulkBuffer, 0);
        }

        // Will be uncommented once the TestWinRT change is done.
        // [Benchmark(OperationsPerInvoke = BulkCount)]
        // public uint GetManyFromManagedList()
        // {
        //     return instance.GetManyFromManagedList(managedBulkVector);
        // }

        [Benchmark(OperationsPerInvoke = BulkCount)]
        public void GetManyStrings()
        {
            bulkStringVector.CopyTo(bulkStringBuffer, 0);
        }

        [Benchmark(OperationsPerInvoke = BulkCount)]
        public void GetManyObjects()
        {
            bulkObjectVector.CopyTo(bulkObjectBuffer, 0);
        }

        [Benchmark(OperationsPerInvoke = MapLen)]
        public int Map()
        {
            int sum = 0;
            foreach (KeyValuePair<string, int> pair in stringMap)
            {
                sum += pair.Value;
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = MapLen)]
        public int MapObjects()
        {
            int sum = 0;
            foreach (KeyValuePair<string, WrappedClass> pair in objectMap)
            {
                sum += pair.Value.DefaultIntProperty;
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = BatchSize)]
        public int Lookup()
        {
            int sum = 0;
            for (int i = 0; i < BatchSize; i++)
            {
                sum += stringMap[(i % MapLen).ToString()];
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = BatchSize)]
        public int VectorView()
        {
            int sum = 0;
            for (int i = 0; i < BatchSize; i++)
            {
                sum += vectorView[i % VectorLen];
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = BatchSize)]
        public int VectorViewObjects()
        {
            int sum = 0;
            for (int i = 0; i < BatchSize; i++)
            {
                sum += objectVectorView[i % VectorLen].DefaultIntProperty;
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = BatchSize)]
        public int MapView()
        {
            int sum = 0;
            for (int i = 0; i < BatchSize; i++)
            {
                sum += mapView[i % MapLen];
            }
            return sum;
        }

        [Benchmark(OperationsPerInvoke = BatchSize)]
        public int MapViewObjects()
        {
            int sum = 0;
            for (int i = 0; i < BatchSize; i++)
            {
                sum += objectMapView[(i % MapLen).ToString()].DefaultIntProperty;
            }
            return sum;
        }
    }
}
