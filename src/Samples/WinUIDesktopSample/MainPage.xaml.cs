using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT;

namespace WinUIDesktopSample
{
    public class DerivedGrid : Grid
    {
        byte[] bytes = new byte[10_000_000];
    };

    public class Derived : ARRPage
    {
        byte[] bytes = new byte[10_000_000];
    };

    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public static object factory;
        public static System.Reflection.PropertyInfo propInfo;
        public MainPage()
        {
            this.InitializeComponent();

            var factoryType = Type.GetType("Microsoft.UI.Xaml.Controls.Page+_IPageFactory,WinUI");
            propInfo = factoryType.GetProperty("ThisPtr", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            factory = Activator.CreateInstance(factoryType);
        }

        public unsafe static IntPtr CreatePage(IntPtr outer, out IntPtr inner)
        {
            IntPtr factoryPtr = (IntPtr)propInfo.GetValue(factory);

            IntPtr comp;
            int hr = (*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out IntPtr, out IntPtr, int>**)factoryPtr)[6](factoryPtr, outer, out inner, out comp);
            return comp;
        }

        private WeakReference baseRef;
        private WeakReference derivedRef;
        private WeakReference gridRef;
        private WeakReference derivedGridRef;
        private List<object> pressure = new List<object>();

        private void Alloc_Click(object sender, RoutedEventArgs e)
        {
            baseRef = new WeakReference(new ARRPage());
            derivedRef = new WeakReference(new Derived());
            gridRef = new WeakReference(new Grid());
            derivedGridRef = new WeakReference(new DerivedGrid());
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            pressure.Add(new byte[10_000_000]);
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            var baseStatus = baseRef.IsAlive ? "ARRPage leaked" : "ARRPage collected";
            var derivedStatus = derivedRef.IsAlive ? "Derived leaked" : "Derived collected";
            var gridStatus = gridRef.IsAlive ? "Grid leaked" : "Grid collected";
            var derivedGridStatus = derivedGridRef.IsAlive ? "DerivedGrid leaked" : "DerivedGrid collected";
            Status.Text = baseStatus + ", " + derivedStatus + ", " + gridStatus + ", " + derivedGridStatus;
        }
    }

    public struct VtblPtr
    {
        public IntPtr Vtbl;
    }

    public class ARRPage : ICustomQueryInterface
    {
        private static readonly ComWrappers cw = new ARRComWrappers();
        private WinRT.IObjectReference _inner = null;

        private static ComWrappers GCW()
        {
            return cw;
        }

        private static IntPtr CI(IntPtr outer, out IntPtr inner)
        {
            return MainPage.CreatePage(outer, out inner);
        }

        private struct ARRPageVtbl
        {
            public IntPtr QueryInterface;
            public _AddRef AddRef;
            public _Release Release;
        }

        private delegate int _AddRef(IntPtr This);
        private delegate int _Release(IntPtr This);

        private ComWrappersHelper.ClassNative classNative;
        private readonly ARRPageVtbl vtable;

        public unsafe ARRPage()
        {
            ComWrappersHelper.Init<ARRPage>(ref this.classNative, this, &GCW, &CI);

            // Create and hold wrapper around Inner, as cswinrt base classes do - causes leak
            _inner = ComWrappersSupport.GetObjectReferenceForInterface(classNative.Inner);

            var inst = Marshal.PtrToStructure<VtblPtr>(this.classNative.Instance);
            this.vtable = Marshal.PtrToStructure<ARRPageVtbl>(inst.Vtbl);
        }

        ~ARRPage()
        {
            ComWrappersHelper.Cleanup(ref this.classNative);
        }

        CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
        {
            if (this.classNative.Inner == IntPtr.Zero)
            {
                ppv = IntPtr.Zero;
                return CustomQueryInterfaceResult.NotHandled;
            }

            const int S_OK = 0;
            const int E_NOINTERFACE = unchecked((int)0x80004002);

            int hr = Marshal.QueryInterface(this.classNative.Inner, ref iid, out ppv);
            if (hr == S_OK)
            {
                return CustomQueryInterfaceResult.Handled;
            }

            return hr == E_NOINTERFACE
                ? CustomQueryInterfaceResult.NotHandled
                : CustomQueryInterfaceResult.Failed;
        }
    }

    class ComWrappersHelper
    {
        private static Guid IID_IReferenceTracker = new Guid("11d3b13a-180e-4789-a8be-7712882893e6");

        [Flags]
        public enum ReleaseFlags
        {
            None = 0,
            Instance = 1,
            Inner = 2,
            ReferenceTracker = 4
        }

        public struct ClassNative
        {
            public ReleaseFlags Release;
            public IntPtr Instance;
            public IntPtr Inner;
            public IntPtr ReferenceTracker;
        }

        public unsafe static void Init<T>(
            ref ClassNative classNative,
            object thisInstance,
            delegate*<ComWrappers> GetComWrapper,
            delegate*<IntPtr, out IntPtr, IntPtr> CreateInstance)
        {
            bool isAggregation = typeof(T) != thisInstance.GetType();

            {
                IntPtr outer = default;
                if (isAggregation)
                {
                    // Create a managed object wrapper (i.e. CCW) to act as the outer.
                    // Passing the CreateComInterfaceFlags.TrackerSupport can be done if
                    // IReferenceTracker support is possible.
                    //
                    // The outer is now owned in this context.
                    outer = GetComWrapper().GetOrCreateComInterfaceForObject(thisInstance, CreateComInterfaceFlags.TrackerSupport);
                }

                // Create an instance of the COM/WinRT type.
                // This is typically accomplished through a call to CoCreateInstance() or RoActivateInstance().
                //
                // Ownership of the outer has been transferred to the new instance.
                // Some APIs do return a non-null inner even with a null outer. This
                // means ownership may now be owned in this context in either aggregation state.
                classNative.Instance = CreateInstance(outer, out classNative.Inner);
            }

            {
                // Determine if the instance supports IReferenceTracker (e.g. WinUI).
                // Acquiring this interface is useful for:
                //   1) Providing an indication of what value to pass during RCW creation.
                //   2) Informing the Reference Tracker runtime during non-aggregation
                //      scenarios about new references.
                //
                // If aggregation, query the inner since that will have the implementation
                // otherwise the new instance will be used. Since the inner was composed
                // it should answer immediately without going through the outer. Either way
                // the reference count will go to the new instance.
                IntPtr queryForTracker = isAggregation ? classNative.Inner : classNative.Instance;
                int hr = Marshal.QueryInterface(queryForTracker, ref IID_IReferenceTracker, out classNative.ReferenceTracker);
                if (hr != 0)
                {
                    classNative.ReferenceTracker = default;
                }
            }

            {
                // Determine flags needed for native object wrapper (i.e. RCW) creation.
                var createObjectFlags = CreateObjectFlags.None;
                IntPtr instanceToWrap = classNative.Instance;

                // Update flags if the native instance is being used in an aggregation scenario.
                if (isAggregation)
                {
                    // Indicate the scenario is aggregation
                    createObjectFlags |= (CreateObjectFlags)4;

                    // The instance supports IReferenceTracker.
                    if (classNative.ReferenceTracker != default(IntPtr))
                    {
                        createObjectFlags |= CreateObjectFlags.TrackerObject;

                        // IReferenceTracker is not needed in aggregation scenarios.
                        // It is not needed because all QueryInterface() calls on an
                        // object are followed by an immediately release of the returned
                        // pointer - see below for details.
                        Marshal.Release(classNative.ReferenceTracker);

                        // .NET 5 limitation
                        //
                        // For aggregated scenarios involving IReferenceTracker
                        // the API handles object cleanup. In .NET 5 the API
                        // didn't expose an option to handle this so we pass the inner
                        // in order to handle its lifetime.
                        //
                        // The API doesn't handle inner lifetime in any other scenario
                        // in the .NET 5 timeframe.
                        instanceToWrap = classNative.Inner;
                    }
                }

                // Create a native object wrapper (i.e. RCW).
                //
                // Note this function will call QueryInterface() on the supplied instance,
                // therefore it is important that the enclosing CCW forwards to its inner
                // if aggregation is involved. This is typically accomplished through an
                // implementation of ICustomQueryInterface.
                GetComWrapper().GetOrRegisterObjectForComInstance(instanceToWrap, createObjectFlags, thisInstance);
            }

            if (isAggregation)
            {
                // We release the instance here, but continue to use it since
                // ownership was transferred to the API and it will guarantee
                // the appropriate lifetime.
                Marshal.Release(classNative.Instance);
            }
            else
            {
                // In non-aggregation scenarios where an inner exists and
                // reference tracker is involved, we release the inner.
                //
                // .NET 5 limitation - see logic above.
                if (classNative.Inner != default(IntPtr) && classNative.ReferenceTracker != default(IntPtr))
                {
                    Marshal.Release(classNative.Inner);
                }
            }

            // The following describes the valid local values to consider and details
            // on their usage during the object's lifetime.
            classNative.Release = ReleaseFlags.None;
            if (isAggregation)
            {
                // Aggregation scenarios should avoid calling AddRef() on the
                // newInstance value. This is due to the semantics of COM Aggregation
                // and the fact that calling an AddRef() on the instance will increment
                // the CCW which in turn will ensure it cannot be cleaned up. Calling
                // AddRef() on the instance when passed to unmanagec code is correct
                // since unmanaged code is required to call Release() at some point.
                if (classNative.ReferenceTracker == default(IntPtr))
                {
                    // COM scenario
                    // The pointer to dispatch on for the instance.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // Instance

                    // A pointer to the inner that should be queried for
                    //    additional interfaces. Immediately after a QueryInterface()
                    //    a Release() should be called on the returned pointer but the
                    //    pointer can be retained and used.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.Inner; // Inner
                }
                else
                {
                    // WinUI scenario
                    // The pointer to dispatch on for the instance.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // Instance

                    // A pointer to the inner that should be queried for
                    //    additional interfaces. Immediately after a QueryInterface()
                    //    a Release() should be called on the returned pointer but the
                    //    pointer can be retained and used.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // Inner

                    // No longer needed.
                    // ** Never release.
                    classNative.Release |= ReleaseFlags.None; // ReferenceTracker
                }
            }
            else
            {
                if (classNative.ReferenceTracker == default(IntPtr))
                {
                    // COM scenario
                    // The pointer to dispatch on for the instance.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.Instance; // Instance
                }
                else
                {
                    // WinUI scenario
                    // The pointer to dispatch on for the instance.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.Instance; // Instance

                    // This instance should be used to tell the
                    //    Reference Tracker runtime whenever an AddRef()/Release()
                    //    is performed on newInstance.
                    // ** Release in this class's Finalizer.
                    classNative.Release |= ReleaseFlags.ReferenceTracker; // ReferenceTracker
                }
            }
        }

        public static void Cleanup(ref ClassNative classNative)
        {
            if (classNative.Release.HasFlag(ReleaseFlags.Inner))
            {
                Marshal.Release(classNative.Inner);
            }
            if (classNative.Release.HasFlag(ReleaseFlags.Instance))
            {
                Marshal.Release(classNative.Instance);
            }
            if (classNative.Release.HasFlag(ReleaseFlags.ReferenceTracker))
            {
                Marshal.Release(classNative.ReferenceTracker);
            }
        }
    }

    class ARRComWrappers : ComWrappers
    {
        [UnmanagedCallersOnly]
        private static int GetIids(IntPtr thisPtr, IntPtr iidCount, IntPtr iids)
        {
            System.Diagnostics.Trace.WriteLine($"Calling IInspectable::{nameof(GetIids)}");
            throw new NotImplementedException();
        }
        [UnmanagedCallersOnly]
        private static int GetRuntimeClassName(IntPtr thisPtr, IntPtr className)
        {
            System.Diagnostics.Trace.WriteLine($"Calling IInspectable::{nameof(GetRuntimeClassName)}");
            throw new NotImplementedException();
        }
        [UnmanagedCallersOnly]
        private static int GetTrustLevel(IntPtr thisPtr, IntPtr trustLevel)
        {
            System.Diagnostics.Trace.WriteLine($"Calling IInspectable::{nameof(GetTrustLevel)}");
            throw new NotImplementedException();
        }

        protected unsafe override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            IntPtr fpQueryInteface = default;
            IntPtr fpAddRef = default;
            IntPtr fpRelease = default;
            ComWrappers.GetIUnknownImpl(out fpQueryInteface, out fpAddRef, out fpRelease);

            var vtblRaw = RuntimeHelpers.AllocateTypeAssociatedMemory(obj.GetType(), IntPtr.Size * 6);
            var vtable = (IntPtr*)vtblRaw;
            vtable[0] = fpQueryInteface;
            vtable[1] = fpAddRef;
            vtable[2] = fpRelease;
            vtable[3] = new IntPtr((delegate* unmanaged<IntPtr, IntPtr, IntPtr, int>)&GetIids);
            vtable[4] = new IntPtr((delegate* unmanaged<IntPtr, IntPtr, int>)&GetRuntimeClassName);
            vtable[5] = new IntPtr((delegate* unmanaged<IntPtr, IntPtr, int>)&GetTrustLevel);

            ComInterfaceEntry* entryRaw = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(obj.GetType(), sizeof(ComInterfaceEntry));
            entryRaw->IID = new Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90");
            entryRaw->Vtable = vtblRaw;
            count = 1;

            return entryRaw;
        }

        protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flag)
        {
            throw new NotImplementedException();
        }

        protected override void ReleaseObjects(System.Collections.IEnumerable objects)
        {
        }
    }
}
