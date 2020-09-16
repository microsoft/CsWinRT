using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
                    ComWrappers.RegisterForTrackerSupport(_comWrappers);
                }
                return _comWrappers;
            }
            set
            {
                _comWrappers = value;
                ComWrappers.RegisterForTrackerSupport(_comWrappers);
            }
        }

        internal static unsafe InspectableInfo GetInspectableInfo(IntPtr pThis)
        {
            var _this = FindObject<object>(pThis);
            return InspectableInfoTable.GetValue(_this, o => PregenerateNativeTypeInformation(o).inspectableInfo);
        }

        public static object CreateRcwForComObject(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            var rcw = ComWrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.TrackerObject);
            // Because .NET will de-duplicate strings and WinRT doesn't,
            // our RCW factory returns a wrapper of our string instance.
            // This ensures that ComWrappers never sees the same managed object for two different
            // native pointers. We unwrap here to ensure that the user-experience is expected
            // and consumers get a string object for a Windows.Foundation.IReference<String>.
            // We need to do the same thing for System.Type because there can be multiple WUX.Interop.TypeName's
            // for a single System.Type.
            return rcw switch
            {
                ABI.System.Nullable<string> ns => ns.Value,
                ABI.System.Nullable<Type> nt => nt.Value,
                _ => rcw
            };
        }

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

        public static IUnknownVftbl IUnknownVftbl => DefaultComWrappers.IUnknownVftbl;
        public static IntPtr IUnknownVftblPtr => DefaultComWrappers.IUnknownVftblPtr;

        public static IntPtr AllocateVtableMemory(Type vtableType, int size) => RuntimeHelpers.AllocateTypeAssociatedMemory(vtableType, size);

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
        public static unsafe IUnknownVftbl IUnknownVftbl => Unsafe.AsRef<IUnknownVftbl>(IUnknownVftblPtr.ToPointer());

        internal static IntPtr IUnknownVftblPtr { get; }

        static unsafe DefaultComWrappers()
        {
            GetIUnknownImpl(out var qi, out var addRef, out var release);

            IUnknownVftblPtr = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IUnknownVftbl), sizeof(IUnknownVftbl));
            (*(IUnknownVftbl*)IUnknownVftblPtr) = new IUnknownVftbl
            {
                QueryInterface = (delegate* unmanaged[Stdcall]<IntPtr, ref Guid, out IntPtr, int>)qi,
                AddRef = (delegate* unmanaged[Stdcall]<IntPtr, uint>)addRef,
                Release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)release,
            };
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (IsRuntimeImplementedRCW(obj))
            {
                // If the object is a runtime-implemented RCW, let the runtime create a CCW.
                count = 0;
                return null;
            }

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

        private static unsafe bool IsRuntimeImplementedRCW(object obj)
        {
            Type t = obj.GetType();
            bool isRcw = t.IsCOMObject;
            if (t.IsGenericType)
            {
                foreach (var arg in t.GetGenericArguments())
                {
                    if (arg.IsCOMObject)
                    {
                        isRcw = true;
                        break;
                    }
                }
            }
            return isRcw;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            IObjectReference objRef = ComWrappersSupport.GetObjectReferenceForInterface(externalComObject);

            if (objRef.TryAs<IInspectable.Vftbl>(out var inspectableRef) == 0)
            {
                IInspectable inspectable = new IInspectable(inspectableRef);

                string runtimeClassName = inspectable.GetRuntimeClassName(noThrow: true);
                if (runtimeClassName == null)
                {
                    // If the external IInspectable has not implemented GetRuntimeClassName,
                    // we use the Inspectable wrapper directly.
                    return inspectable;
                }
                return ComWrappersSupport.GetTypedRcwFactory(runtimeClassName)(inspectable);
            }
            else if (objRef.TryAs<ABI.WinRT.Interop.IWeakReference.Vftbl>(out var weakRef) == 0)
            {
                // IWeakReference is IUnknown-based, so implementations of it may not (and likely won't) implement
                // IInspectable. As a result, we need to check for them explicitly.
                return new ABI.WinRT.Interop.IWeakReference(weakRef);
            }
            // If the external COM object isn't IInspectable or IWeakReference, we can't handle it.
            // If we're registered globally, we want to let the runtime fall back for IUnknown and IDispatch support.
            // Return null so the runtime can fall back gracefully in IUnknown and IDispatch scenarios.
            return null;
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
