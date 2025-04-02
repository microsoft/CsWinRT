// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;


#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Microsoft.UI.Xaml.Interop
{
    [global::WinRT.WindowsRuntimeType]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Interop.IBindableIterable))]
    internal interface IBindableIterable
    {
        IBindableIterator First();
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Interop.IBindableIterator))]
    internal interface IBindableIterator
    {
        bool MoveNext();
        // GetMany is not implemented by IBindableIterator, but it is here
        // for compat purposes with WinUI where there are scenarios they do
        // reinterpret_cast from IBindableIterator to IIterable<object>.  It is
        // the last function in the vftable and shouldn't be called by anyone.
        // If called, it will return NotImplementedException.
        uint GetMany(ref object[] items);
        object Current { get; }
        bool HasCurrent { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
    internal interface IBindableVector : IEnumerable
    {
        object GetAt(uint index);
        IBindableVectorView GetView();
        bool IndexOf(object value, out uint index);
        void SetAt(uint index, object value);
        void InsertAt(uint index, object value);
        void RemoveAt(uint index);
        void Append(object value);
        void RemoveAtEnd();
        void Clear();
        uint Size { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Interop.IBindableVectorView))]
    internal interface IBindableVectorView : IEnumerable
    {
        object GetAt(uint index);
        bool IndexOf(object value, out uint index);
        uint Size { get; }
    }
}

namespace ABI.Microsoft.UI.Xaml.Interop
{
    [DynamicInterfaceCastableImplementation]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    internal unsafe interface IBindableIterable : global::Microsoft.UI.Xaml.Interop.IBindableIterable, ABI.System.Collections.IEnumerable
    {
        
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
    internal unsafe interface IBindableIterator : global::Microsoft.UI.Xaml.Interop.IBindableIterator
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IBindableIterator()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IBindableIterator), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 4);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_get_Current_0;
            ((delegate* unmanaged[Stdcall]<IntPtr, byte*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_HasCurrent_1;
            ((delegate* unmanaged[Stdcall]<IntPtr, byte*, int>*)AbiToProjectionVftablePtr)[8] = &Do_Abi_MoveNext_2;
            ((delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, uint*, int>*)AbiToProjectionVftablePtr)[9] = &Do_Abi_GetMany_3;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_MoveNext_2(IntPtr thisPtr, byte* result)
        {
            bool __result = default;
            *result = default;
            try
            {
                __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableIterator>(thisPtr).MoveNext();
                *result = (byte)(__result ? 1 : 0);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, uint* result)
        {
            *result = default;

            try
            {
                // Should never be called.
                throw new NotImplementedException();
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_get_Current_0(IntPtr thisPtr, IntPtr* value)
        {
            object __value = default;
            *value = default;
            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableIterator>(thisPtr).Current;
                *value = MarshalInspectable<object>.FromManaged(__value);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_get_HasCurrent_1(IntPtr thisPtr, byte* value)
        {
            bool __value = default;
            *value = default;
            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableIterator>(thisPtr).HasCurrent;
                *value = (byte)(__value ? 1 : 0);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        internal static ObjectReference<IUnknownVftbl> FromAbi(IntPtr thisPtr) => ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, IID.IID_IUnknown);

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe bool global::Microsoft.UI.Xaml.Interop.IBindableIterator.MoveNext()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>**)ThisPtr)[8](ThisPtr, &__retval));
            GC.KeepAlive(_obj);
            return __retval != 0;
        }

        unsafe uint global::Microsoft.UI.Xaml.Interop.IBindableIterator.GetMany(ref object[] items)
        {
            // Should never be called.
            throw new NotImplementedException();
        }

        unsafe object global::Microsoft.UI.Xaml.Interop.IBindableIterator.Current
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval));
                    GC.KeepAlive(_obj);
                    return MarshalInspectable<object>.FromAbi(__retval);
                }
                finally
                {
                    MarshalInspectable<object>.DisposeAbi(__retval);
                }
            }
        }

        unsafe bool global::Microsoft.UI.Xaml.Interop.IBindableIterator.HasCurrent
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>**)ThisPtr)[7](ThisPtr, &__retval));
                GC.KeepAlive(_obj);
                return __retval != 0;
            }
        }

    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IBindableIterator_Delegates
    {
        public unsafe delegate int get_Current_0(IntPtr thisPtr, IntPtr* result);
        public unsafe delegate int get_HasCurrent_1(IntPtr thisPtr, byte* result);
        public unsafe delegate int MoveNext_2(IntPtr thisPtr, byte* result);
        public unsafe delegate int GetMany_3(IntPtr thisPtr, int itemSize, IntPtr items, uint* result);
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
    internal unsafe interface IBindableVectorView : global::Microsoft.UI.Xaml.Interop.IBindableVectorView
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static IBindableVectorView()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IBindableVectorView), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 3);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_GetAt_0;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_Size_1;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>*)AbiToProjectionVftablePtr)[8] = &Do_Abi_IndexOf_2;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, IntPtr* result)
        {
            object __result = default;

            try
            {
                __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>(thisPtr).GetAt(index);
                *result = MarshalInspectable<object>.FromManaged(__result);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_IndexOf_2(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue)
        {
            bool __returnValue = default;

            *index = default;
            *returnValue = default;
            uint __index = default;

            try
            {
                __returnValue = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>(thisPtr).IndexOf(MarshalInspectable<object>.FromAbi(value), out __index);
                *index = __index;
                *returnValue = (byte)(__returnValue ? 1 : 0);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* value)
        {
            uint __value = default;

            *value = default;

            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>(thisPtr).Size;
                *value = __value;

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        internal static ObjectReference<IUnknownVftbl> FromAbi(IntPtr thisPtr) => ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, IID.IID_IBindableVectorView);

        private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<IWinRTObject, ABI.System.Collections.IEnumerable.FromAbiHelper> _helperTable = new();

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe object global::Microsoft.UI.Xaml.Interop.IBindableVectorView.GetAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>**)ThisPtr)[6](ThisPtr, index, &__retval));
                GC.KeepAlive(_obj);
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe bool global::Microsoft.UI.Xaml.Interop.IBindableVectorView.IndexOf(object value, out uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>**)ThisPtr)[8](
                    ThisPtr,
                    MarshalInspectable<object>.GetAbi(__value),
                    &__index,
                    &__retval));
                GC.KeepAlive(_obj);
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        unsafe uint global::Microsoft.UI.Xaml.Interop.IBindableVectorView.Size
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)ThisPtr)[7](ThisPtr, &__retval));
                GC.KeepAlive(_obj);
                return __retval;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _helperTable.GetValue((IWinRTObject)this,
                        (enumerable) => new ABI.System.Collections.IEnumerable.FromAbiHelper((global::System.Collections.IEnumerable)(IWinRTObject)enumerable)
                   ).GetEnumerator();
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IBindableVectorView_Delegates
    {
        public unsafe delegate int GetAt_0(IntPtr thisPtr, uint index, IntPtr* result);
        public unsafe delegate int get_Size_1(IntPtr thisPtr, uint* result);
        public unsafe delegate int IndexOf_2(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue);
    }
}

namespace ABI.System.Collections
{
    using global::Microsoft.UI.Xaml.Interop;
    using global::System;
    using global::System.Diagnostics.CodeAnalysis;
    using global::System.Reflection;
    using global::System.Runtime.CompilerServices;

#if EMBED
    internal
#else
    public
#endif
    static class IEnumerableMethods
    {
        public static global::System.Guid IID => global::WinRT.Interop.IID.IID_IEnumerable;

        public static IntPtr AbiToProjectionVftablePtr => IEnumerable.AbiToProjectionVftablePtr;
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    internal unsafe interface IEnumerable : global::System.Collections.IEnumerable, global::Microsoft.UI.Xaml.Interop.IBindableIterable
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerable));

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class AdaptiveFromAbiHelper : FromAbiHelper, global::System.Collections.IEnumerable
#pragma warning restore CA2257
        {
            /// <summary>
            /// The cached <see cref="IEnumerable{T}.GetEnumerator"/> method.
            /// </summary>
            private static readonly MethodInfo EnumerableOfTGetEnumerator = typeof(IEnumerable<>).GetMethod("GetEnumerator");

#if NET8_0_OR_GREATER
            private readonly MethodInvoker _enumerator;
#else
            private readonly MethodInfo _enumerator;
#endif

            public AdaptiveFromAbiHelper(Type runtimeType, IWinRTObject winRTObject)
                :base(winRTObject)
            {
                Type enumGenericType;

                // First, look for and get the IEnumerable<> interface implemented by this type, if one exists. The scenario is, imagine you
                // got an 'IList<string>' from somewhere, and then you use LINQ with it. What LINQ does is it converts both to object. Then
                // it does a cast to 'IEnumerable'. At this point, you are trying an IDIC cast for 'IEnumerable' and asking the IDIC 'IEnumerable'
                // interface to handle it. The question to answer is, what version of 'IEnumerable' is it. Is it the one provided by 'IList<string>'
                // or is it the one that maps to 'IBindableIterable'. We actually don't know at this point which one to use, especially given the
                // 'IBindableIterable' interface may not even be implemented on the native object, as it was an 'IList<string>' originally. This is
                // also why we can't just check whether the object implements 'IEnumerable' and just call 'GetEnumerator()' on that.
                if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    enumGenericType = runtimeType;
                }
                else
                {
                    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification =
                        """
                        'SomeType.GetInterfaces().Any(t => t.GetGenericTypeDefinition() == typeof(IEnumerable<>)' is safe,
                        provided you obtained someType from something like an analyzable 'Type.GetType' or 'object.GetType'
                        (i.e. it is safe when the type you're asking about can exist on the GC heap as allocated).
                        """)]
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static Type GetEnumerableOfTInterface(Type runtimeType)
                    {
                        foreach (Type interfaceType in runtimeType.GetInterfaces())
                        {
                            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                            {
                                return interfaceType;
                            }
                        }

                        return null;
                    }

                    enumGenericType = GetEnumerableOfTInterface(runtimeType);
                }

                var methodInfo = (MethodInfo)enumGenericType?.GetMemberWithSameMetadataDefinitionAs(EnumerableOfTGetEnumerator);

#if NET8_0_OR_GREATER
                _enumerator = methodInfo is null ? null : MethodInvoker.Create(methodInfo);
#else
                _enumerator = methodInfo;
#endif
            }

            public override IEnumerator GetEnumerator()
            {
                if (_enumerator is not null)
                {
                    // The method returns IEnumerator<>, which implements IEnumerator
#if NET8_0_OR_GREATER
                    return Unsafe.As<IEnumerator>(_enumerator.Invoke(_winrtObject));
#else
                    return Unsafe.As<IEnumerator>(_enumerator.Invoke(_winrtObject, null));
#endif
                }

                return base.GetEnumerator();
            }
        }

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public class FromAbiHelper : global::System.Collections.IEnumerable
#pragma warning restore CA2257
        {
            private readonly global::System.Collections.IEnumerable _iterable;
            protected readonly IWinRTObject _winrtObject;

            public FromAbiHelper(global::System.Collections.IEnumerable iterable)
            {
                _iterable = iterable;
            }

            protected FromAbiHelper(IWinRTObject winrtObject)
            {
                _iterable = null;
                _winrtObject = winrtObject;
            }

            private IWinRTObject GetIterable()
            {
                return (IWinRTObject)_iterable ?? _winrtObject;
            }

            public virtual global::System.Collections.IEnumerator GetEnumerator() =>
                new Generic.FromAbiEnumerator<object>(new NonGenericToGenericIterator(((global::Microsoft.UI.Xaml.Interop.IBindableIterable) GetIterable()).First()));

            private sealed class NonGenericToGenericIterator : global::Windows.Foundation.Collections.IIterator<object>
            {
                private readonly IBindableIterator iterator;

                public NonGenericToGenericIterator(IBindableIterator iterator) => this.iterator = iterator;

                public object _Current => iterator.Current;
                public bool HasCurrent => iterator.HasCurrent;
                public bool _MoveNext() { return iterator.MoveNext(); }
                public uint GetMany(ref object[] items) => throw new NotSupportedException();
            }
        }

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class ToAbiHelper : IBindableIterable
#pragma warning restore CA2257
        {
            private readonly IEnumerable m_enumerable;

            internal ToAbiHelper(IEnumerable enumerable) => m_enumerable = enumerable;

            IBindableIterator IBindableIterable.First() => MakeBindableIterator(m_enumerable.GetEnumerator());

            internal static IBindableIterator MakeBindableIterator(IEnumerator enumerator) =>
                new Generic.IEnumerator<object>.ToAbiHelper(new NonGenericToGenericEnumerator(enumerator));

            private sealed class NonGenericToGenericEnumerator : IEnumerator<object>
            {
                private readonly IEnumerator enumerator;

                public NonGenericToGenericEnumerator(IEnumerator enumerator) => this.enumerator = enumerator; 

                public object Current => enumerator.Current;
                public bool MoveNext() { return enumerator.MoveNext(); }
                public void Reset() { enumerator.Reset(); }
                public void Dispose() { }
            }
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IEnumerable()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IEnumerable), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 1);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_First_0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_First_0(IntPtr thisPtr, IntPtr* result)
        {
            *result = default;
            try
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IEnumerable>(thisPtr);
                var iterator = ToAbiHelper.MakeBindableIterator(__this.GetEnumerator());
                *result = MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableIterator>.FromManaged(iterator);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        internal static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, IID.IID_IUnknown);
        }

        private static FromAbiHelper _AbiHelper(IWinRTObject _this)
        {
            return (FromAbiHelper)_this.AdditionalTypeData.GetOrAdd(typeof(global::System.Collections.IEnumerable).TypeHandle,
                static (_, _this) => new FromAbiHelper((global::System.Collections.IEnumerable)_this), _this);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe global::Microsoft.UI.Xaml.Interop.IBindableIterator global::Microsoft.UI.Xaml.Interop.IBindableIterable.First()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IEnumerable).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval));
                GC.KeepAlive(_obj);
                return MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableIterator>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableIterator>.DisposeAbi(__retval);
            }
        }

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
        {
            return _AbiHelper((IWinRTObject)this).GetEnumerator();
        }
    }

    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public 
#endif
    static class IEnumerable_Delegates
    {
        public unsafe delegate int First_0(IntPtr thisPtr, IntPtr* result);
    }

#if EMBED
    internal
#else
    public
#endif
    static class IListMethods
    {
        public static Guid IID => global::WinRT.Interop.IID.IID_IList;

        public static IntPtr AbiToProjectionVftablePtr => IList.AbiToProjectionVftablePtr;
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
    internal unsafe interface IList : global::System.Collections.IList, global::Microsoft.UI.Xaml.Interop.IBindableVector
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IList));

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class FromAbiHelper : global::System.Collections.IList
#pragma warning restore CA2257
        {
            private readonly global::Microsoft.UI.Xaml.Interop.IBindableVector _vector;

            public FromAbiHelper(global::Microsoft.UI.Xaml.Interop.IBindableVector vector)
            {
                _vector = vector;
            }

            public bool IsSynchronized => false;

            public object SyncRoot { get => this; }

            public int Count
            {
                get
                { 
                    uint size = _vector.Size;
                    if (((uint)int.MaxValue) < size)
                    {
                        throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                    }

                    return (int)size;
                }
            }

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                // ICollection expects the destination array to be single-dimensional.
                if (array.Rank != 1)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Arg_RankMultiDimNotSupported);

                int destLB = array.GetLowerBound(0);
                int srcLen = Count;
                int destLen = array.GetLength(0);

                if (arrayIndex < destLB)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                // Does the dimension in question have sufficient space to copy the expected number of entries?
                // We perform this check before valid index check to ensure the exception message is in sync with
                // the following snippet that uses regular framework code:
                //
                // ArrayList list = new ArrayList();
                // list.Add(1);
                // Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { -1 });
                // list.CopyTo(items, 0);

                if (srcLen > (destLen - (arrayIndex - destLB)))
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_InsufficientSpaceToCopyCollection);

                if (arrayIndex - destLB > destLen)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_IndexOutOfArrayBounds);

                // We need to verify the index as we;
                for (uint i = 0; i < srcLen; i++)
                {
                    array.SetValue(_vector.GetAt(i), i + arrayIndex);
                }
            }

            public object this[int index]
            {
                get => Indexer_Get(index);
                set => Indexer_Set(index, value);
            }

            internal object Indexer_Get(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return GetAt(_vector, (uint)index);
            }

            internal void Indexer_Set(int index, object value)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                SetAt(_vector, (uint)index, value);
            }

            public int Add(object value)
            {
                _vector.Append(value);

                uint size = _vector.Size;
                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                }

                return (int)(size - 1);
            }

            public bool Contains(object item)
            {
                return _vector.IndexOf(item, out _);
            }

            public void Clear()
            {
                _vector.Clear();
            }

            public bool IsFixedSize { get => false; }

            public bool IsReadOnly { get => false; }

            public int IndexOf(object item)
            {
                uint index;
                bool exists = _vector.IndexOf(item, out index);

                if (!exists)
                    return -1;

                if (((uint)int.MaxValue) < index)
                {
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                }

                return (int)index;
            }

            public void Insert(int index, object item)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                InsertAtHelper(_vector, (uint)index, item);
            }

            public void Remove(object item)
            {
                uint index;
                bool exists = _vector.IndexOf(item, out index);

                if (exists)
                {
                    if (((uint)int.MaxValue) < index)
                    {
                        throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                    }

                    RemoveAtHelper(_vector, index);
                }
            }

            public void RemoveAt(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                RemoveAtHelper(_vector, (uint)index);
            }

            private static object GetAt(global::Microsoft.UI.Xaml.Interop.IBindableVector _this, uint index)
            {
                try
                {
                    return _this.GetAt(index);

                    // We delegate bounds checking to the underlying collection and if it detected a fault,
                    // we translate it to the right exception:
                }
                catch (Exception ex)
                {
                    if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    throw;
                }
            }

            private static void SetAt(global::Microsoft.UI.Xaml.Interop.IBindableVector _this, uint index, object value)
            {
                try
                {
                    _this.SetAt(index, value);

                    // We delegate bounds checking to the underlying collection and if it detected a fault,
                    // we translate it to the right exception:
                }
                catch (Exception ex)
                {
                    if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    throw;
                }
            }

            private static void InsertAtHelper(global::Microsoft.UI.Xaml.Interop.IBindableVector _this, uint index, object item)
            {
                try
                {
                    _this.InsertAt(index, item);

                    // We delegate bounds checking to the underlying collection and if it detected a fault,
                    // we translate it to the right exception:
                }
                catch (Exception ex)
                {
                    if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    throw;
                }
            }

            private static void RemoveAtHelper(global::Microsoft.UI.Xaml.Interop.IBindableVector _this, uint index)
            {
                try
                {
                    _this.RemoveAt(index);

                    // We delegate bounds checking to the underlying collection and if it detected a fault,
                    // we translate it to the right exception:
                }
                catch (Exception ex)
                {
                    if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    throw;
                }
            }

            public IEnumerator GetEnumerator()
            {
                return ((IEnumerable)(IWinRTObject)_vector).GetEnumerator();
            }
        }

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class ToAbiHelper : IBindableVector
#pragma warning restore CA2257
        {
            private readonly global::System.Collections.IList _list;

            public ToAbiHelper(global::System.Collections.IList list) => _list = list;

            public object GetAt(uint index)
            {
                EnsureIndexInt32(index, _list.Count);

                try
                {
                    return _list[(int)index];
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, WinRTRuntimeErrorStrings.ArgumentOutOfRange_Index);
                }
            }

            public uint Size { get => (uint)_list.Count; }
            
            IBindableVectorView IBindableVector.GetView()
            {
                return new ListToBindableVectorViewAdapter(_list);
            }

            public bool IndexOf(object value, out uint index)
            {
                int ind = _list.IndexOf(value);

                if (-1 == ind)
                {
                    index = 0;
                    return false;
                }

                index = (uint)ind;
                return true;
            }

            public void SetAt(uint index, object value)
            {
                EnsureIndexInt32(index, _list.Count);

                try
                {
                    _list[(int)index] = value;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, WinRTRuntimeErrorStrings.ArgumentOutOfRange_Index);
                }
            }

            public void InsertAt(uint index, object value)
            {
                // Inserting at an index one past the end of the list is equivalent to appending
                // so we need to ensure that we're within (0, count + 1).
                EnsureIndexInt32(index, _list.Count + 1);

                try
                {
                    _list.Insert((int)index, value);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Change error code to match what WinRT expects
                    ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw;
                }
            }

            public void RemoveAt(uint index)
            {
                EnsureIndexInt32(index, _list.Count);

                try
                {
                    _list.RemoveAt((int)index);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Change error code to match what WinRT expects
                    ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw;
                }
            }

            public void Append(object value)
            {
                _list.Add(value);
            }

            public void RemoveAtEnd()
            {
                if (_list.Count == 0)
                {
                    Exception e = new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CannotRemoveLastFromEmptyCollection);
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }

                uint size = (uint)_list.Count;
                RemoveAt(size - 1);
            }

            public void Clear()
            {
                _list.Clear();
            }

            private static void EnsureIndexInt32(uint index, int listCapacity)
            {
                // We use '<=' and not '<' becasue int.MaxValue == index would imply
                // that Size > int.MaxValue:
                if (((uint)int.MaxValue) <= index || index >= (uint)listCapacity)
                {
                    Exception e = new ArgumentOutOfRangeException(nameof(index), WinRTRuntimeErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }
            }

            public IEnumerator GetEnumerator() => _list.GetEnumerator();

            internal sealed class ListToBindableVectorViewAdapterTypeDetails : IWinRTExposedTypeDetails
            {
                public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
                {
                    return new ComWrappers.ComInterfaceEntry[]
                    {
                        new ComWrappers.ComInterfaceEntry
                        {
                            IID = IID.IID_IBindableVectorView,
                            Vtable = ABI.Microsoft.UI.Xaml.Interop.IBindableVectorView.AbiToProjectionVftablePtr
                        },
                        new ComWrappers.ComInterfaceEntry
                        {
                            IID = ABI.System.Collections.IEnumerableMethods.IID,
                            Vtable = ABI.System.Collections.IEnumerableMethods.AbiToProjectionVftablePtr
                        }
                    };
                }
            }

            /// A Windows Runtime IBindableVectorView implementation that wraps around a managed IList exposing
            /// it to Windows runtime interop.
            [global::WinRT.WinRTExposedType(typeof(ListToBindableVectorViewAdapterTypeDetails))]
            internal sealed class ListToBindableVectorViewAdapter : IBindableVectorView
            {
                private readonly global::System.Collections.IList list;

                internal ListToBindableVectorViewAdapter(global::System.Collections.IList list)
                {
                    if (list == null)
                        throw new ArgumentNullException(nameof(list));
                    this.list = list;
                }

                private static void EnsureIndexInt32(uint index, int listCapacity)
                {
                    // We use '<=' and not '<' becasue int.MaxValue == index would imply
                    // that Size > int.MaxValue:
                    if (((uint)int.MaxValue) <= index || index >= (uint)listCapacity)
                    {
                        Exception e = new ArgumentOutOfRangeException(nameof(index), WinRTRuntimeErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                        e.SetHResult(ExceptionHelpers.E_BOUNDS);
                        throw e;
                    }
                }

                public IBindableIterator First() =>
                    IEnumerable.ToAbiHelper.MakeBindableIterator(list.GetEnumerator());

                public object GetAt(uint index)
                {
                    EnsureIndexInt32(index, list.Count);

                    try
                    {
                        return list[(int)index];
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, WinRTRuntimeErrorStrings.ArgumentOutOfRange_Index);
                    }
                }

                public uint Size => (uint)list.Count;

                public bool IndexOf(object value, out uint index)
                {
                    int ind = list.IndexOf(value);

                    if (-1 == ind)
                    {
                        index = 0;
                        return false;
                    }

                    index = (uint)ind;
                    return true;
                }

                public IEnumerator GetEnumerator() => list.GetEnumerator();
            }
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IList()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IList), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 10);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_GetAt_0;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_Size_1;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[8] = &Do_Abi_GetView_2;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>*)AbiToProjectionVftablePtr)[9] = &Do_Abi_IndexOf_3;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>*)AbiToProjectionVftablePtr)[10] = &Do_Abi_SetAt_4;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>*)AbiToProjectionVftablePtr)[11] = &Do_Abi_InsertAt_5;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, int>*)AbiToProjectionVftablePtr)[12] = &Do_Abi_RemoveAt_6;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>*)AbiToProjectionVftablePtr)[13] = &Do_Abi_Append_7;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)AbiToProjectionVftablePtr)[14] = &Do_Abi_RemoveAtEnd_8;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)AbiToProjectionVftablePtr)[15] = &Do_Abi_Clear_9;
        }

        private static readonly ConditionalWeakTable<global::System.Collections.IList, ToAbiHelper> _adapterTable = new();

        private static IBindableVector FindAdapter(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IList>(thisPtr);
            return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, IntPtr* result)
        {
            object __result = default;
            *result = default;
            try
            {
                __result = FindAdapter(thisPtr).GetAt(index);
                *result = MarshalInspectable<object>.FromManaged(__result);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_GetView_2(IntPtr thisPtr, IntPtr* result)
        {
            global::Microsoft.UI.Xaml.Interop.IBindableVectorView __result = default;
            *result = default;
            try
            {
                __result = FindAdapter(thisPtr).GetView();
                *result = MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>.FromManaged(__result);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_IndexOf_3(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue)
        {
            bool __returnValue = default;
            *index = default;
            *returnValue = default;
            uint __index = default;
            try
            {
                __returnValue = FindAdapter(thisPtr).IndexOf(MarshalInspectable<object>.FromAbi(value), out __index);
                *index = __index;
                *returnValue = (byte)(__returnValue ? 1 : 0);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_SetAt_4(IntPtr thisPtr, uint index, IntPtr value)
        {
            try
            {
                FindAdapter(thisPtr).SetAt(index, MarshalInspectable<object>.FromAbi(value));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_InsertAt_5(IntPtr thisPtr, uint index, IntPtr value)
        {
            try
            {
                FindAdapter(thisPtr).InsertAt(index, MarshalInspectable<object>.FromAbi(value));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_RemoveAt_6(IntPtr thisPtr, uint index)
        {
            try
            {
                FindAdapter(thisPtr).RemoveAt(index);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_Append_7(IntPtr thisPtr, IntPtr value)
        {
            try
            {
                FindAdapter(thisPtr).Append(MarshalInspectable<object>.FromAbi(value));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_RemoveAtEnd_8(IntPtr thisPtr)
        {
            try
            {
                FindAdapter(thisPtr).RemoveAtEnd();
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_Clear_9(IntPtr thisPtr)
        {
            try
            {
                FindAdapter(thisPtr).Clear();
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* value)
        {
            uint __value = default;

            *value = default;

            try
            {
                __value = FindAdapter(thisPtr).Size;
                *value = __value;
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        internal static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, IID.IID_IUnknown);
        }

        internal static FromAbiHelper _VectorToList(IWinRTObject _this)
        {
            return (FromAbiHelper)_this.AdditionalTypeData.GetOrAdd(typeof(global::System.Collections.IList).TypeHandle,
                static (_, _this) => new FromAbiHelper((global::Microsoft.UI.Xaml.Interop.IBindableVector)_this), _this);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe object global::Microsoft.UI.Xaml.Interop.IBindableVector.GetAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>**)ThisPtr)[6](ThisPtr, index, &__retval));
                GC.KeepAlive(_obj);
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe global::Microsoft.UI.Xaml.Interop.IBindableVectorView global::Microsoft.UI.Xaml.Interop.IBindableVector.GetView()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[8](ThisPtr, &__retval));
                GC.KeepAlive(_obj);
                return MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>.DisposeAbi(__retval);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe bool global::Microsoft.UI.Xaml.Interop.IBindableVector.IndexOf(object value, out uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>**)ThisPtr)[9](ThisPtr, MarshalInspectable<object>.GetAbi(__value), &__index, &__retval));
                GC.KeepAlive(_obj);
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe void global::Microsoft.UI.Xaml.Interop.IBindableVector.SetAt(uint index, object value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>**)ThisPtr)[10](ThisPtr, index, MarshalInspectable<object>.GetAbi(__value)));
                GC.KeepAlive(_obj);
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe void global::Microsoft.UI.Xaml.Interop.IBindableVector.InsertAt(uint index, object value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>**)ThisPtr)[11](ThisPtr, index, MarshalInspectable<object>.GetAbi(__value)));
                GC.KeepAlive(_obj);
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe void global::Microsoft.UI.Xaml.Interop.IBindableVector.RemoveAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, int>**)ThisPtr)[12](ThisPtr, index));
            GC.KeepAlive(_obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe void global::Microsoft.UI.Xaml.Interop.IBindableVector.Append(object value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>**)ThisPtr)[13](ThisPtr, MarshalInspectable<object>.GetAbi(__value)));
                GC.KeepAlive(_obj);
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe void global::Microsoft.UI.Xaml.Interop.IBindableVector.RemoveAtEnd()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[14](ThisPtr));
            GC.KeepAlive(_obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe void global::Microsoft.UI.Xaml.Interop.IBindableVector.Clear()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[15](ThisPtr));
            GC.KeepAlive(_obj);
        }

        unsafe uint global::Microsoft.UI.Xaml.Interop.IBindableVector.Size
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)ThisPtr)[7](ThisPtr, &__retval));
                GC.KeepAlive(_obj);
                return __retval;
            }
        }

        object global::System.Collections.IList.this[int index]
        {
            get => _VectorToList((IWinRTObject)this)[index];
             
            set => _VectorToList((IWinRTObject)this)[index] = value;
        }

        bool global::System.Collections.IList.IsFixedSize => _VectorToList((IWinRTObject)this).IsFixedSize;

        bool global::System.Collections.IList.IsReadOnly => _VectorToList((IWinRTObject)this).IsReadOnly;

        int global::System.Collections.ICollection.Count => _VectorToList((IWinRTObject)this).Count;

        bool global::System.Collections.ICollection.IsSynchronized => _VectorToList((IWinRTObject)this).IsSynchronized;

        object global::System.Collections.ICollection.SyncRoot => _VectorToList((IWinRTObject)this).SyncRoot;

        int global::System.Collections.IList.Add(object value) => _VectorToList((IWinRTObject)this).Add(value);

        void global::System.Collections.IList.Clear() => _VectorToList((IWinRTObject)this).Clear();

        bool global::System.Collections.IList.Contains(object value) => _VectorToList((IWinRTObject)this).Contains(value);

        int global::System.Collections.IList.IndexOf(object value) => _VectorToList((IWinRTObject)this).IndexOf(value);

        void global::System.Collections.IList.Insert(int index, object value) => _VectorToList((IWinRTObject)this).Insert(index, value);

        void global::System.Collections.IList.Remove(object value) => _VectorToList((IWinRTObject)this).Remove(value);

        void global::System.Collections.IList.RemoveAt(int index) => _VectorToList((IWinRTObject)this).RemoveAt(index);

        void global::System.Collections.ICollection.CopyTo(Array array, int index) => _VectorToList((IWinRTObject)this).CopyTo(array, index);

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _VectorToList((IWinRTObject)this).GetEnumerator();
    }
    internal static class IList_Delegates
    {
        public unsafe delegate int GetAt_0(IntPtr thisPtr, uint index, IntPtr* result);
        public unsafe delegate int get_Size_1(IntPtr thisPtr, uint* result);
        public unsafe delegate int GetView_2(IntPtr thisPtr, IntPtr* result);
        public unsafe delegate int IndexOf_3(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue);
        public unsafe delegate int SetAt_4(IntPtr thisPtr, uint index, IntPtr value);
        public unsafe delegate int InsertAt_5(IntPtr thisPtr, uint index, IntPtr value);
        public unsafe delegate int RemoveAt_6(IntPtr thisPtr, uint index);
        public unsafe delegate int Append_7(IntPtr thisPtr, IntPtr value);
        public unsafe delegate int RemoveAtEnd_8(IntPtr thisPtr);
        public unsafe delegate int Clear_9(IntPtr thisPtr);
    }
}
