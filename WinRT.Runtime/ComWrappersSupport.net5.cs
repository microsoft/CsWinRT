using System;
using System.Collections;
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
        internal static readonly ConditionalWeakTable<object, InspectableInfo> InspectableInfoTable = new ConditionalWeakTable<object, InspectableInfo>();

        private static ComWrappers _comWrappers;
        private static ComWrappers ComWrappers
        {
            get
            {
                if (_comWrappers is null)
                {
                    _comWrappers = new DefaultComWrappers();
                    _comWrappers.RegisterAsGlobalInstance();
                }
                return _comWrappers;
            }
            set
            {
                _comWrappers = value;
                _comWrappers.RegisterAsGlobalInstance();
            }
        }

        internal static unsafe InspectableInfo GetInspectableInfo(IntPtr pThis)
        {
            var _this = FindObject<object>(pThis);
            return InspectableInfoTable.GetValue(_this, o => PregenerateNativeTypeInformation(o).inspectableInfo);
        }

        public static object CreateRcwForComObject(IntPtr ptr) => ComWrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.TrackerObject);

        public static void RegisterObjectForInterface(object obj, IntPtr thisPtr) => TryRegisterObjectForInterface(obj, thisPtr);

        public static object TryRegisterObjectForInterface(object obj, IntPtr thisPtr) => ComWrappers.GetOrRegisterObjectForComInstance(thisPtr, CreateObjectFlags.TrackerObject, obj);

        public static IObjectReference CreateCCWForObject(object obj)
        {
            IntPtr ccw = ComWrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.TrackerSupport);
            return ObjectReference<IUnknownVftbl>.Attach(ref ccw);
        }

        public static unsafe T FindObject<T>(IntPtr ptr)
            where T : class => ComInterfaceDispatch.GetInstance<T>((ComInterfaceDispatch*)ptr);

        private static T FindDelegate<T>(IntPtr thisPtr)
            where T : class, System.Delegate => FindObject<T>(thisPtr);

        public static IUnknownVftbl IUnknownVftbl { get; private set; }

        public static IntPtr AllocateVtableMemory(Type vtableType, int size) => RuntimeHelpers.AllocateTypeAssociatedMemory(vtableType, size);

        static partial void PlatformSpecificInitialize()
        {
            IUnknownVftbl = DefaultComWrappers.IUnknownVftbl;
        }

        /// <summary>
        /// Initialize the global <see cref="System.Runtime.InteropServices.ComWrappers"/> instance to use for WinRT.
        /// </summary>
        /// <param name="wrappers">The wrappers instance to use, or the default if null.</param>
        /// <remarks>
        /// A custom ComWrappers instance can be supplied to enable programs to fast-track some type resolution
        /// instead of using reflection when the full type closure is known.
        /// </remarks>
        public static void InitializeComWrappers(ComWrappers wrappers = null)
        {
            ComWrappers = wrappers ?? new DefaultComWrappers();
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

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            IObjectReference objRef = ComWrappersSupport.GetObjectReferenceForIntPtr(externalComObject);
            IInspectable inspectable;
            try
            {
                inspectable = new IInspectable(objRef);
            }
            catch (InvalidCastException)
            {
                // If the external COM object isn't IInspectable, we can't handle it.
                // If we're registered globally, we want to let the runtime fall back for IUnknown and IDispatch support.
                // Return null so the runtime can fall back gracefully in IUnknown and IDispatch scenarios.
                return null;
            }

            string runtimeClassName = inspectable.GetRuntimeClassName();
            return ComWrappersSupport.GetTypedRcwFactory(runtimeClassName)(inspectable);
        }

        protected override void ReleaseObjects(IEnumerable objects)
        {
            foreach (var obj in objects)
            {
                if (ComWrappersSupport.TryUnwrapObject(obj, out var objRef))
                {
                    objRef.Dispose();
                }
                else
                {
                    throw new InvalidOperationException("Cannot release objects that are not runtime wrappers of native WinRT objects.");
                }
            }
        }

        unsafe class VtableEntriesCleanupScout
        {
            private readonly ComInterfaceEntry* _data;

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
