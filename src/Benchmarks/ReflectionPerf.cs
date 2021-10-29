﻿using BenchmarkComponent;
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

        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMarshalingRoutines();
            instanceDictionary = instance.ExistingDictionary;
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
    }
}
