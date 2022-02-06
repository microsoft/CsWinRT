using BenchmarkComponent;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ReflectionPerf
    {
        ClassWithMarshalingRoutines instance;
        IDictionary<String, WrappedClass> instanceDictionary;
        ManagedObjectWithInterfaces managedObject;
        ConcurrentDictionary<Type, bool> ttest1;
        ConcurrentDictionary<Type, Type> ttest2;
        ConcurrentDictionary<Type, bool> ttest3;
        ConcurrentDictionary<Type, string> ttest4;
        ConcurrentDictionary<Type, TypeCache> ttest5;
        ConcurrentDictionary<Type, TypeCache2> ttest6;
        ConcurrentDictionary<Type, bool> ttest7;
        ConcurrentDictionary<Type, Type> ttest8;
        ConcurrentDictionary<Type, bool> ttest9;
        ConcurrentDictionary<Type, string> ttest10;
        ConcurrentDictionary<Type, TypeCache> ttest11;
        ConcurrentDictionary<Type, TypeCache2> ttest12;


        [GlobalSetup]
        public void Setup()
        {
            instance = new ClassWithMarshalingRoutines();
            instanceDictionary = instance.ExistingDictionary;
            managedObject = new ManagedObjectWithInterfaces();
            ttest1 = new();
            ttest2 = new();
            ttest3 = new();
            ttest4 = new();
            ttest5 = new();
            ttest6 = new();
            ttest7 = new();
            ttest8 = new();
            ttest9 = new();
            ttest10 = new();
            ttest11= new();
            ttest12 = new();
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

        private Type[] GetTypes()
        {
            return new Type [] { 
                typeof(ClassWithMarshalingRoutines),
                typeof(int), 
                typeof(ReflectionPerf), 
                typeof(System.Uri), 
                typeof(double), 
                typeof(ManagedObjectWithInterfaces), 
                typeof(ManagedComposableObjectWithInterfaces), 
                typeof(ManagedEvents),
                typeof(object),
                typeof(ReflectionPerf),
                typeof(System.Uri),
                typeof(double),
                typeof(double),
                typeof(ManagedObjectWithInterfaces),
                typeof(ManagedComposableObjectWithInterfaces),
                typeof(BlittableStruct),
                typeof(NonBlittable),
                typeof(ClassWithMarshalingRoutines),
                typeof(int),
                typeof(ReflectionPerf),
                typeof(System.Uri),
                typeof(double),
                typeof(ManagedObjectWithInterfaces),
                typeof(ManagedComposableObjectWithInterfaces),
                typeof(ClassWithMarshalingRoutines),
                typeof(int),
                typeof(ReflectionPerf),
                typeof(System.Uri),
                typeof(double),
                typeof(ManagedObjectWithInterfaces),
                typeof(BlittableStruct),
                typeof(NonBlittable),
                typeof(ClassWithMarshalingRoutines),
            };
        }

        private sealed class TypeCache
        {
            private readonly Type type;
            private bool isLengthGreaterThan4Set;
            private bool lengthGreaterThan4;
            private bool isBaseTypeSet;
            private Type baseType;
            private bool isLengthLessThan4;
            private bool lengthLessThan4;
            private bool isName;
            private string name;

            public TypeCache(Type type)
            {
                this.type = type;
            }

            private static bool MakeLengthGreaterThan4(Type type)
            {
                return type.FullName.Length > 4;
            }

            private bool SetLengthGreaterThan4()
            {
                lengthGreaterThan4 = MakeLengthGreaterThan4(type);
                isLengthGreaterThan4Set = true;
                return lengthGreaterThan4;
            }

            public bool LengthGreaterThan4 => isLengthGreaterThan4Set ? lengthGreaterThan4 : SetLengthGreaterThan4();

            private static Type MakeBaseType(Type type)
            {
                return type.BaseType;
            }

            private Type SetBaseType()
            {
                baseType = MakeBaseType(type);
                isBaseTypeSet = true;
                return baseType;
            }

            public Type BaseType => isBaseTypeSet ? baseType : SetBaseType();

            private static bool MakeLengthLessThan4(Type type)
            {
                return type.FullName.Length < 4;
            }

            private bool SetLengthLessThan4()
            {
                lengthLessThan4 = MakeLengthLessThan4(type);
                isLengthLessThan4 = true;
                return lengthLessThan4;
            }

            public bool LengthLessThan4 => isLengthLessThan4 ? lengthLessThan4 : SetLengthLessThan4();

            private static string MakeName(Type type)
            {
                return type.Name;
            }

            private string SetName()
            {
                name = MakeName(type);
                isName = true;
                return name;
            }

            public string Name => isName ? name : SetName();
        }

        private sealed class TypeCache2
        {
            private readonly Type type;
            private volatile bool isLengthGreaterThan4Set;
            private volatile bool lengthGreaterThan4;
            private volatile bool isBaseTypeSet;
            private volatile Type baseType;
            private volatile bool isLengthLessThan4;
            private volatile bool lengthLessThan4;
            private volatile bool isName;
            private volatile string name;

            public TypeCache2(Type type)
            {
                this.type = type;
            }

            private static bool MakeLengthGreaterThan4(Type type)
            {
                return type.FullName.Length > 4;
            }

            private bool SetLengthGreaterThan4()
            {
                lengthGreaterThan4 = MakeLengthGreaterThan4(type);
                isLengthGreaterThan4Set = true;
                return lengthGreaterThan4;
            }

            public bool LengthGreaterThan4 => isLengthGreaterThan4Set ? lengthGreaterThan4 : SetLengthGreaterThan4();

            private static Type MakeBaseType(Type type)
            {
                return type.BaseType;
            }

            private Type SetBaseType()
            {
                baseType = MakeBaseType(type);
                isBaseTypeSet = true;
                return baseType;
            }

            public Type BaseType => isBaseTypeSet ? baseType : SetBaseType();

            private static bool MakeLengthLessThan4(Type type)
            {
                return type.FullName.Length < 4;
            }

            private bool SetLengthLessThan4()
            {
                lengthLessThan4 = MakeLengthLessThan4(type);
                isLengthLessThan4 = true;
                return lengthLessThan4;
            }

            public bool LengthLessThan4 => isLengthLessThan4 ? lengthLessThan4 : SetLengthLessThan4();

            private static string MakeName(Type type)
            {
                return type.Name;
            }

            private string SetName()
            {
                name = MakeName(type);
                isName = true;
                return name;
            }

            public string Name => isName ? name : SetName();
        }

        [Benchmark]
        public object Test1()
        {
            Type[] types = GetTypes();
            ConcurrentDictionary<Type, bool> test1 = new();
            ConcurrentDictionary<Type, Type> test2 = new();
            ConcurrentDictionary<Type, bool> test3 = new();
            ConcurrentDictionary<Type, string> test4 = new();
            foreach (Type type in types)
            {
                _ = test1.GetOrAdd(type, (type) => type.FullName.Length > 4);
                _ = test2.GetOrAdd(type, (type) => type.BaseType);
                _ = test3.GetOrAdd(type, (type) => type.FullName.Length < 4);
                _ = test4.GetOrAdd(type, (type) => type.Name);
            }
            return null;
        }

        [Benchmark]
        public object Test2()
        {
            Type[] types = GetTypes();
            ConcurrentDictionary<Type, TypeCache> test1 = new();
            foreach (Type type in types)
            {
                var typeCache = test1.GetOrAdd(type, (type) => new(type));
                _ = typeCache.LengthGreaterThan4;
                _ = typeCache.BaseType;
                _ = typeCache.LengthLessThan4;
                _ = typeCache.Name;
            }
            return null;
        }

        [Benchmark]
        public object Test3()
        {
            Type[] types = GetTypes();
            ConcurrentDictionary<Type, TypeCache2> test1 = new();
            foreach (Type type in types)
            {
                var typeCache = test1.GetOrAdd(type, (type) => new(type));
                _ = typeCache.LengthGreaterThan4;
                _ = typeCache.BaseType;
                _ = typeCache.LengthLessThan4;
                _ = typeCache.Name;
            }
            return null;
        }

        [Benchmark]
        public object Test33()
        {
            Type[] types = GetTypes();
            ConcurrentDictionary<Type, TypeCache2> test1 = new();
            foreach (Type type in types)
            {
                _ = test1.GetOrAdd(type, (type) => new(type)).LengthGreaterThan4;
                _ = test1.GetOrAdd(type, (type) => new(type)).BaseType;
                _ = test1.GetOrAdd(type, (type) => new(type)).LengthLessThan4;
                _ = test1.GetOrAdd(type, (type) => new(type)).Name;
            }
            return null;
        }

        [Benchmark]
        public object Test4()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                _ = ttest1.GetOrAdd(type, (type) => type.FullName.Length > 4);
                _ = ttest2.GetOrAdd(type, (type) => type.BaseType);
                _ = ttest3.GetOrAdd(type, (type) => type.FullName.Length < 4);
                _ = ttest4.GetOrAdd(type, (type) => type.Name);
            }
            return null;
        }

        [Benchmark]
        public object Test5()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                var typeCache = ttest5.GetOrAdd(type, (type) => new(type));
                _ = typeCache.LengthGreaterThan4;
                _ = typeCache.BaseType;
                _ = typeCache.LengthLessThan4;
                _ = typeCache.Name;
            }
            return null;
        }

        [Benchmark]
        public object Test6()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                var typeCache = ttest6.GetOrAdd(type, (type) => new(type));
                _ = typeCache.LengthGreaterThan4;
                _ = typeCache.BaseType;
                _ = typeCache.LengthLessThan4;
                _ = typeCache.Name;
            }
            return null;
        }
        [Benchmark]
        public object Test66()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                _ = ttest6.GetOrAdd(type, (type) => new(type)).LengthGreaterThan4;
                _ = ttest6.GetOrAdd(type, (type) => new(type)).BaseType;
                _ = ttest6.GetOrAdd(type, (type) => new(type)).LengthLessThan4;
                _ = ttest6.GetOrAdd(type, (type) => new(type)).Name;
            }
            return null;
        }

        [Benchmark]
        public object Test7()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                _ = ttest7.GetOrAdd(type, (type) => type.FullName.Length > 4);
                _ = ttest8.GetOrAdd(type, (type) => type.BaseType);
                _ = ttest9.GetOrAdd(type, (type) => type.FullName.Length < 4);
                _ = ttest10.GetOrAdd(type, (type) => type.Name);
            }

            ttest7.Clear();
            ttest8.Clear();
            ttest9.Clear();
            ttest10.Clear();
            return null;
        }

        [Benchmark]
        public object Test8()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                var typeCache = ttest11.GetOrAdd(type, (type) => new(type));
                _ = typeCache.LengthGreaterThan4;
                _ = typeCache.BaseType;
                _ = typeCache.LengthLessThan4;
                _ = typeCache.Name;
            }

            ttest11.Clear();
            return null;
        }

        [Benchmark]
        public object Test9()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                var typeCache = ttest12.GetOrAdd(type, (type) => new(type));
                _ = typeCache.LengthGreaterThan4;
                _ = typeCache.BaseType;
                _ = typeCache.LengthLessThan4;
                _ = typeCache.Name;
            }

            ttest12.Clear();
            return null;
        }

        [Benchmark]
        public object Test99()
        {
            Type[] types = GetTypes();
            foreach (Type type in types)
            {
                _ = ttest12.GetOrAdd(type, (type) => new(type)).LengthGreaterThan4;
                _ = ttest12.GetOrAdd(type, (type) => new(type)).BaseType;
                _ = ttest12.GetOrAdd(type, (type) => new(type)).LengthLessThan4;
                _ = ttest12.GetOrAdd(type, (type) => new(type)).Name;
            }

            ttest12.Clear();
            return null;
        }

    }
}
