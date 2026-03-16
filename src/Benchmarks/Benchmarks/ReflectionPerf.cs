using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ReflectionPerf
    {
        ClassWithMarshalingRoutines instance;
        IDictionary<String, WrappedClass> instanceDictionary;
        ManagedObjectWithInterfaces managedObject;

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMarshalingRoutines();
            instanceDictionary = instance.ExistingDictionary;
            managedObject = new ManagedObjectWithInterfaces();
        }

        [Benchmark]
        public object ExecuteMarshalingForNewKeyValuePair()
        {
            return instance.NewTypeErasedKeyValuePairObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForNewArray()
        {
            return instance.NewTypeErasedArrayObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForNewNullable()
        {
            return instance.NewTypeErasedNullableObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForExistingKeyvaluePair()
        {
            return instance.ExistingTypeErasedKeyValuePairObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForExistingArray()
        {
            return instance.ExistingTypeErasedArrayObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForExistingNullable()
        {
            return instance.ExistingTypeErasedNullableObject;
        }

        [Benchmark]
        public object ExecuteMarshalingForString()
        {
            return instance.DefaultStringProperty;
        }

        [Benchmark]
        public object ExecuteMarshalingForCustomObject()
        {
            return instance.NewWrappedClassObject;
        }

        [Benchmark]
        public int ExecuteMarshalingForDelegate()
        {
            int y = 1;
            instance.CallForInt(() => y);
            return y;
        }

        [Benchmark]
        public void IntEventSource()
        {
            ClassWithMarshalingRoutines instance = new ClassWithMarshalingRoutines();

            int x = 0;
            int y = 1;
            int z;

            instance.IntProperty = x;
            instance.CallForInt(() => y);

            instance.IntPropertyChanged += (object sender, int value) => z = value;
            instance.RaiseIntChanged();
        }

        [Benchmark]
        public int ExistingDictionaryLookup()
        {
            var dict = instance.ExistingDictionary;
            var count = 0;
            for (int i = 0; i < 100; i++)
            {
                if (dict["a"] != null)
                {
                    count++;
                }
            }
            return count;
        }

        [Benchmark]
        public object ExistingDictionaryLookupCached()
        {
            var dict = instance.ExistingDictionary;
            WrappedClass cache = null;
            for (int i = 0; i < 1000; i++)
            {
                cache = dict["a"];
            }
            return cache;
        }

        [Benchmark]
        public object ExistingDictionaryLookup2()
        {
            return instanceDictionary["a"];
        }

        [Benchmark]
        public object ExistingDictionaryLookup3()
        {
            var dict = instance.ExistingDictionary;
            return dict["a"];
        }

        [Benchmark]
        public int GetNullableInt()
        {
            return instance.NullableInt.Value;
        }

        [Benchmark]
        public void SetNullableInt()
        {
            instance.NullableInt = new Nullable<int>(4);
        }

        [Benchmark]
        public object GetNullableBittableStruct()
        {
            return instance.NullableBlittableStruct.Value;
        }

        [Benchmark]
        public void SetNullableBittableStruct()
        {
            BlittableStruct blittableStruct = new BlittableStruct() { i32 = 2 };
            instance.NullableBlittableStruct = new Nullable<BlittableStruct>(blittableStruct);
        }

        [Benchmark]
        public object GetNullableTimeSpan()
        {
            return instance.NullableTimeSpan.Value;
        }

        [Benchmark]
        public void SetNullableTimeSpan()
        {
            TimeSpan timeSpan = new TimeSpan(100);
            instance.NullableTimeSpan = new Nullable<TimeSpan>(timeSpan);
        }

        [Benchmark]
        public object GetNullableNonBittableStruct()
        {
            return instance.NullableNonBlittableStruct.Value;
        }

        [Benchmark]
        public void SetNullableNonBittableStruct()
        {
            NonBlittable nonBlittable = new NonBlittable() { A = true, C = "beta" };
            instance.NullableNonBlittableStruct = new Nullable<NonBlittable>(nonBlittable);
        }

        [Benchmark]
        public void SetNullableDelegate()
        {
            int z;
            System.EventHandler<int> s = (object sender, int value) => z = value;
            instance.NewTypeErasedNullableObject = s;
        }

        [Benchmark]
        public void SetNullableIntDelegate()
        {
            ProvideInt s = () => 4;
            instance.BoxedDelegate = s;
        }

        [Benchmark]
        public object GetNullableIntDelegate()
        {
            return instance.BoxedDelegate as ProvideInt;
        }

        [Benchmark]
        public object GetNewIntDelegate()
        {
            return instance.NewIntDelegate;
        }

        [Benchmark]
        public object GetExistingIntDelegate()
        {
            return instance.ExistingIntDelegate;
        }

        [Benchmark]
        public string CreateAndIterateList()
        {
            var list = instance.NewList();
            list.Add("How");
            list.Add("Are");
            list.Add("You");
            var sentence = "";
            for (int i = 0; i < list.Count; i++)
            {
                sentence += list[i];
            }
            return sentence;
        }

        [Benchmark]
        public object GetUri()
        {
            return instance.NewUri;
        }

        [Benchmark]
        public void SetUri()
        {
            instance.NewUri = new Uri("https://github.com");
        }

        [Benchmark]
        public object GetExistingUri()
        {
            return instance.ExistingUri;
        }

        [Benchmark]
        public object GetWinRTType()
        {
            return instance.NewType;
        }

        [Benchmark]
        public void SetWinRTType()
        {
            instance.NewType = typeof(ClassWithMarshalingRoutines);
        }

        [Benchmark]
        public void SetPrimitiveType()
        {
            instance.NewType = typeof(int);
        }

        [Benchmark]
        public void SetNonWinRTType()
        {
            instance.NewType = typeof(ReflectionPerf);
        }

        [Benchmark]
        public object GetExistingWinRTType()
        {
            return instance.ExistingType;
        }

        [Benchmark]
        public void GetWeakReferenceOfManagedObject()
        {
            instance.GetWeakReference(managedObject);
        }

        [Benchmark]
        public object GetAndResolveWeakReferenceOfManagedObject()
        {
            return instance.GetAndResolveWeakReference(managedObject);
        }

        [Benchmark]
        public object GetWeakReferenceOfNativeObject()
        {
            return new WeakReference<ClassWithMarshalingRoutines>(instance);
        }
    }
}