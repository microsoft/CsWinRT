﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;
using static System.Runtime.InteropServices.ComWrappers;

namespace WinRT
{
    public static partial class ComWrappersSupport
    {
        internal static readonly ConditionalWeakTable<object, ComWrappersSupport.InspectableInfo> InspectableInfoTable = new ConditionalWeakTable<object, ComWrappersSupport.InspectableInfo>();

        internal static unsafe InspectableInfo GetInspectableInfo(IntPtr pThis)
        {
            var _this = FindObject<object>(pThis);
            return InspectableInfoTable.GetValue(_this, o => ComWrappersSupport.PregenerateNativeTypeInformation(o).inspectableInfo);
        }

        public static object CreateRcwForComObject(IntPtr ptr) => throw new NotImplementedException();

        public static void RegisterObjectForInterface(object obj, IntPtr thisPtr) => throw new NotImplementedException();

        // If we aren't in the activation scenario and we need to register an RCW after the fact,
        // we need to be resilient to an RCW having already been created on another thread.
        // This method registers the given object as the RCW if there isn't already one registered
        // and returns the registered RCW if it is still alive.
        public static object TryRegisterObjectForInterface(object obj, IntPtr thisPtr) => throw new NotImplementedException();

        public static IObjectReference CreateCCWForObject(object obj) => throw new NotImplementedException();

        public static unsafe T FindObject<T>(IntPtr ptr)
            where T : class => ComInterfaceDispatch.GetInstance<T>((ComInterfaceDispatch*)ptr);

        private static T FindDelegate<T>(IntPtr thisPtr)
            where T : class, System.Delegate => FindObject<T>(thisPtr);

        public static IUnknownVftbl IUnknownVftbl { get; private set; }

        static partial void PlatformSpecificInitialize()
        {
            IUnknownVftbl = DefaultComWrappers.IUnknownVftbl;
        }

        internal static Func<IInspectable, object> GetTypedRcwFactory(string runtimeClassName) => TypedObjectFactoryCache.GetOrAdd(runtimeClassName, className => CreateTypedRcwFactory(className));
    }

    public class DefaultComWrappers : ComWrappers
    {
        private static ConditionalWeakTable<object, VtableEntriesCleanupScout> ComInterfaceEntryCleanupTable = new ConditionalWeakTable<object, VtableEntriesCleanupScout>();
        public static IUnknownVftbl IUnknownVftbl { get; }

        static DefaultComWrappers()
        {
            GetIUnknownImpl(out var qi, out var addRef, out var release);
            IUnknownVftbl = new IUnknownVftbl
            {
                QueryInterface = Marshal.GetDelegateForFunctionPointer<IUnknownVftbl._QueryInterface>(qi),
                AddRef = Marshal.GetDelegateForFunctionPointer<IUnknownVftbl._AddRef>(addRef),
                Release = Marshal.GetDelegateForFunctionPointer<IUnknownVftbl._Release>(release),
            };
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            var entries = ComWrappersSupport.GetInterfaceTableEntries(obj);

            if (flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown))
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = typeof(IUnknownVftbl).GUID,
                    Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
                });
            }

            entries.Add(new ComInterfaceEntry
            {
                IID = typeof(IInspectable).GUID,
                Vtable = IInspectable.Vftbl.AbiToProjectionVftablePtr
            });

            count = entries.Count;
            ComInterfaceEntry* nativeEntries = (ComInterfaceEntry*)Marshal.AllocCoTaskMem(sizeof(ComInterfaceEntry) * count);

            for (int i = 0; i < count; i++)
            {
                nativeEntries[i] = entries[i];
            }

            ComInterfaceEntryCleanupTable.Add(obj, new VtableEntriesCleanupScout(nativeEntries));

            return nativeEntries;
        }

        protected override object CreateObject(IntPtr externalComObject, IntPtr agileObjectRef, CreateObjectFlags flags)
        {
            // TODO: Figure out how to create an ObjectReference that wraps an interface ptr
            // and correctly handles calling Release on a finalizer thread.
            var inspectable = new IInspectable(ObjectReference<IUnknownVftbl>.Attach(ref externalComObject)); string runtimeClassName = inspectable.GetRuntimeClassName();
            return ComWrappersSupport.GetTypedRcwFactory(inspectable.GetRuntimeClassName())(inspectable);
        }

        unsafe class VtableEntriesCleanupScout
        {
            private ComInterfaceEntry* _data;

            public VtableEntriesCleanupScout(ComInterfaceEntry* data)
            {
                _data = data;
            }

            ~VtableEntriesCleanupScout()
            {
                Marshal.FreeCoTaskMem((IntPtr)_data);
            }
        }
    }
}
