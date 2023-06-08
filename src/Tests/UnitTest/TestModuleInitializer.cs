using System;
using WinRT;

#if !NET

namespace UnitTest
{
    // In our .NET standard support, we generate most of the delegates needed by generic types as part of the projection.  But for the ones
    // passed or obtained as an Object, we are not able to statically detect that.  On .NET Core, we are able to utilize Expression.GetDelegateType
    // to dynamically create one in this case, but on .NET Framework we are not able to do that for ones with pointers in their parameters.
    // This tests that scenario where the ABI delegates need to be manually declared and registered by the caller.
    internal static class ProjectionTypesInitializer
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void InitalizeProjectionTypes()
        {
            Projections.RegisterAbiDelegate(new Type[] { typeof(void*), typeof(ABI.TestComponent.NonBlittable).MakeByRefType(), typeof(int) }, typeof(_get_Value_NonBlittable));
            Projections.RegisterAbiDelegate(new Type[] { typeof(void*), typeof(TestComponentCSharp.EnumValue).MakeByRefType(), typeof(int) }, typeof(_get_Value_EnumValue));
            Projections.RegisterAbiDelegate(new Type[] { typeof(void*), typeof(uint), typeof(ABI.TestComponent.Composable).MakeByRefType(), typeof(int) }, typeof(_get_at_Composable));
            Projections.RegisterAbiDelegate(new Type[] { typeof(void*), typeof(ABI.TestComponent.Composable), typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) }, typeof(_index_of_Composable));
            Projections.RegisterAbiDelegate(new Type[] { typeof(void*), typeof(uint), typeof(ABI.TestComponent.Composable), typeof(int) }, typeof(_set_at_Composable));
            Projections.RegisterAbiDelegate(new Type[] { typeof(void*), typeof(ABI.TestComponent.Composable), typeof(int) }, typeof(_append_Composable));
        }

        internal unsafe delegate int _get_Value_NonBlittable(void* thisPtr, out ABI.TestComponent.NonBlittable __return_value__);
        internal unsafe delegate int _get_Value_EnumValue(void* thisPtr, out TestComponentCSharp.EnumValue __return_value__);
        internal unsafe delegate int _get_at_Composable(void* thisPtr, uint index, out ABI.TestComponent.Composable __return_value__);
        internal unsafe delegate int _index_of_Composable(void* thisPtr, ABI.TestComponent.Composable value, out uint index, out byte found);
        internal unsafe delegate int _set_at_Composable(void* thisPtr, uint index, ABI.TestComponent.Composable value);
        internal unsafe delegate int _append_Composable(void* thisPtr, ABI.TestComponent.Composable value);
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}

#endif
