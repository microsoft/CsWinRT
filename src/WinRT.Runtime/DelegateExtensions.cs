// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WinRT
{
    internal static class DelegateExtensions
    {
        public static void DynamicInvokeAbi(this Delegate del, object[] invoke_params)
        {
            Marshal.ThrowExceptionForHR((int)del.DynamicInvoke(invoke_params));
        }

        // These methods below can be used to efficiently change the signature of arbitrary delegate types
        // to one that has just 'object' as any of the type arguments, without the need to generate a new
        // closure and display class. The C# compiler will lower the method group expression over one of
        // the extension methods below into a direct constructor call of the new delegate types, passing
        // a function pointer for the target delegate, along with any target, if present. This is more
        // compact in binary size (and better in perf) than eg. 'return (object arg) => function(arg)';
        //
        // We have both some general purpose generic versions, as well two specialized variants to handle
        // the ObjectReferenceValue marshalling with arbitrary inputs, when marshalling reference types.
        // Once again, this just allows us to attach additional logic without needing a display class.

        public static Func<object, object> WithMarshaler2Support(this Func<IObjectReference, IntPtr> function)
        {
            return function.InvokeWithMarshaler2Support;
        }

        public static Action<object> WithMarshaler2Support(this Action<IObjectReference> action)
        {
            return action.InvokeWithMarshaler2Support;
        }

        public static Action<T, IntPtr> WithTypedT1<T>(this Action<object, IntPtr> action)
        {
            return action.InvokeWithTypedT1;
        }

        public static Func<object, TResult> WithObjectT<T, TResult>(this Func<T, TResult> function)
        {
            return function.InvokeWithObjectT;
        }

        public static Func<T, object> WithObjectTResult<T, TResult>(this Func<T, TResult> function)
        {
            return function.InvokeWithObjectTResult;
        }

        public static Action<object> WithObjectParams<T>(this Action<T> action)
        {
            return action.InvokeWithObjectParams;
        }

        private static object InvokeWithMarshaler2Support(this Func<IObjectReference, IntPtr> func, object arg)
        {
            if (arg is ObjectReferenceValue objectReferenceValue)
            {
                return objectReferenceValue.GetAbi();
            }

            return func.Invoke((IObjectReference)arg);
        }

        private static void InvokeWithMarshaler2Support(this Action<IObjectReference> action, object arg)
        {
            if (arg is ObjectReferenceValue objectReferenceValue)
            {
                objectReferenceValue.Dispose();
            }
            else
            {
                action((IObjectReference)arg);
            }
        }

        private static object InvokeWithObjectTResult<T, TResult>(this Func<T, TResult> func, T arg)
        {
            return func.Invoke(arg);
        }

        private static TResult InvokeWithObjectT<T, TResult>(this Func<T, TResult> func, object arg)
        {
            return func.Invoke((T)arg);
        }

        private static void InvokeWithObjectParams<T>(this Action<T> func, object arg)
        {
            func.Invoke((T)arg);
        }

        private static void InvokeWithTypedT1<T>(this Action<object, IntPtr> action, T arg1, IntPtr arg2)
        {
            action.Invoke(arg1, arg2);
        }

        // This extension method allows us to create a new delegate directly pointing to this method, which
        // also adds the resulting object to the conditional weak table before returning it to callers.
        public static object InvokeWithBoxedValueReferenceCacheInsertion(this Func<IInspectable, object> factory, IInspectable inspectable)
        {
            object resultingObject = factory(inspectable);

            ComWrappersSupport.BoxedValueReferenceCache.Add(resultingObject, inspectable);

            return resultingObject;
        }
    }
}
