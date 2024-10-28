#if NET
namespace Windows.Storage.Streams
{
    // Given we do not do automatic lookup table generation for projections, this class defines
    // one for the scenarios which are used by StreamOperationsImplementations for its task adapters.
    internal static class StreamTaskAdaptersImplementation
    {
        private static readonly bool _initialized = Init();
        internal static bool Initialized => _initialized;

        private static unsafe bool Init()
        {
            global::WinRT.ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup(LookupVtableEntries);
            global::WinRT.ComWrappersSupport.RegisterTypeRuntimeClassNameLookup(new Func<Type, string>(LookupRuntimeClassName));
            return true;
        }

        private static ComWrappers.ComInterfaceEntry[] LookupVtableEntries(Type type)
        {
            var typeName = type.ToString();
            if (typeName == "System.Threading.Tasks.TaskToAsyncOperationWithProgressAdapter`2[Windows.Storage.Streams.IBuffer,System.UInt32]")
            {
                _ = IAsyncOperationWithProgress_IBuffer_uint.Initialized;

                return new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry[]
                {
                    new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                    {
                        IID = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.IID,
                        Vtable = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.AbiToProjectionVftablePtr
                    },
                    new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                    {
                        IID = global::ABI.Windows.Foundation.IAsyncInfoMethods.IID,
                        Vtable = global::ABI.Windows.Foundation.IAsyncInfoMethods.AbiToProjectionVftablePtr
                    },
                };
            }
            else if (typeName == "System.Threading.Tasks.TaskToAsyncOperationWithProgressAdapter`2[System.UInt32,System.UInt32]")
            {
                _ = IAsyncOperationWithProgress_uint_uint.Initialized;

                return new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry[]
                {
                    new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                    {
                        IID = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.IID,
                        Vtable = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.AbiToProjectionVftablePtr
                    },
                    new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                    {
                        IID = global::ABI.Windows.Foundation.IAsyncInfoMethods.IID,
                        Vtable = global::ABI.Windows.Foundation.IAsyncInfoMethods.AbiToProjectionVftablePtr
                    },
                };
            }
            else if (typeName == "System.Threading.Tasks.TaskToAsyncOperationAdapter`1[System.Boolean]")
            {
                _ = IAsyncOperation_bool.Initialized;

                return new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry[]
                {
                    new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                    {
                        IID = global::ABI.Windows.Foundation.IAsyncOperationMethods<bool>.IID,
                        Vtable = global::ABI.Windows.Foundation.IAsyncOperationMethods<bool>.AbiToProjectionVftablePtr
                    },
                    new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                    {
                        IID = global::ABI.Windows.Foundation.IAsyncInfoMethods.IID,
                        Vtable = global::ABI.Windows.Foundation.IAsyncInfoMethods.AbiToProjectionVftablePtr
                    },
                };
            }

            return default;
        }

        private static string LookupRuntimeClassName(Type type)
        {
            var typeName = type.ToString();
            if (typeName == "System.Threading.Tasks.TaskToAsyncOperationWithProgressAdapter`2[Windows.Storage.Streams.IBuffer,System.UInt32]")
            {
                return "Windows.Foundation.IAsyncOperationWithProgress`2<Windows.Storage.Streams.IBuffer, UInt32>";
            }
            else if (typeName == "System.Threading.Tasks.TaskToAsyncOperationWithProgressAdapter`2[System.UInt32,System.UInt32]")
            {
                return "Windows.Foundation.IAsyncOperationWithProgress`2<Double, Double>";
            }
            else if (typeName == "System.Threading.Tasks.TaskToAsyncOperationAdapter`1[System.Boolean]")
            {
                return "Windows.Foundation.IAsyncOperation`1<Boolean>";
            }

            return default;
        }

        private static class IAsyncOperationWithProgress_uint_uint
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                return global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint, uint, uint>.InitCcw(
                    &Do_Abi_put_Progress_0,
                    &Do_Abi_get_Progress_1,
                    &Do_Abi_put_Completed_2,
                    &Do_Abi_get_Completed_3,
                    &Do_Abi_GetResults_4
                );
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_GetResults_4(IntPtr thisPtr, uint* __return_value__)
            {
                uint ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.Do_Abi_GetResults_4(thisPtr);
                    *__return_value__ = ____return_value__;
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_put_Progress_0(IntPtr thisPtr, IntPtr handler)
            {
                _ = AsyncOperationProgressHandler_uint_uint.Initialized;
                try
                {
                    global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.Do_Abi_put_Progress_0(
                        thisPtr,
                        global::ABI.Windows.Foundation.AsyncOperationProgressHandler<uint, uint>.FromAbi(handler));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_get_Progress_1(IntPtr thisPtr, IntPtr* __return_value__)
            {
                _ = AsyncOperationProgressHandler_uint_uint.Initialized;
                global::Windows.Foundation.AsyncOperationProgressHandler<uint, uint> ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.Do_Abi_get_Progress_1(thisPtr);
                    *__return_value__ = global::ABI.Windows.Foundation.AsyncOperationProgressHandler<uint, uint>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_put_Completed_2(IntPtr thisPtr, IntPtr handler)
            {
                _ = AsyncOperationWithProgressCompletedHandler_uint_uint.Initialized;
                try
                {
                    global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.Do_Abi_put_Completed_2(
                        thisPtr,
                        global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandler<uint, uint>.FromAbi(handler));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_get_Completed_3(IntPtr thisPtr, IntPtr* __return_value__)
            {
                _ = AsyncOperationWithProgressCompletedHandler_uint_uint.Initialized;
                global::Windows.Foundation.AsyncOperationWithProgressCompletedHandler<uint, uint> ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.Do_Abi_get_Completed_3(thisPtr);
                    *__return_value__ = global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandler<uint, uint>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        private static class AsyncOperationProgressHandler_uint_uint
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                _ = global::ABI.Windows.Foundation.AsyncOperationProgressHandlerMethods<uint, uint, uint, uint>.InitCcw(&Do_Abi_Invoke);
                _ = global::ABI.Windows.Foundation.AsyncOperationProgressHandlerMethods<uint, uint, uint, uint>.InitRcwHelper(&Invoke);
                return true;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr asyncInfo, uint progressInfo)
            {
                try
                {
                    global::ABI.Windows.Foundation.AsyncOperationProgressHandlerMethods<uint, uint, uint, uint>.Abi_Invoke(
                        thisPtr,
                        MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint>>.FromAbi(asyncInfo),
                        progressInfo);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe void Invoke(IObjectReference objRef, global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint> asyncInfo, uint progressInfo)
            {
                IntPtr ThisPtr = objRef.ThisPtr;
                ObjectReferenceValue __asyncInfo = default;

                try
                {
                    __asyncInfo = MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint>>.CreateMarshaler2(
                        asyncInfo,
                        global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<uint, uint>.IID);
                    IntPtr abiAsyncInfo = MarshalInspectable<object>.GetAbi(__asyncInfo);

                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, int>**)ThisPtr)[3](
                        ThisPtr,
                        abiAsyncInfo,
                        progressInfo));
                    GC.KeepAlive(objRef);
                }
                finally
                {
                    MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint>>.DisposeMarshaler(__asyncInfo);

                }
            }
        }

        private static class AsyncOperationWithProgressCompletedHandler_uint_uint
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                return global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandlerMethods<uint, uint, uint, uint>.
                    InitCcw(&Do_Abi_Invoke);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr asyncInfo, global::Windows.Foundation.AsyncStatus asyncStatus)
            {
                try
                {
                    global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandlerMethods<uint, uint, uint, uint>.Abi_Invoke(
                        thisPtr,
                        MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<uint, uint>>.FromAbi(asyncInfo),
                        asyncStatus);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        private static class IAsyncOperationWithProgress_IBuffer_uint
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                return global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, IntPtr, uint, uint>.InitCcw(
                    &Do_Abi_put_Progress_0,
                    &Do_Abi_get_Progress_1,
                    &Do_Abi_put_Completed_2,
                    &Do_Abi_get_Completed_3,
                    &Do_Abi_GetResults_4
                );
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_GetResults_4(IntPtr thisPtr, IntPtr* __return_value__)
            {
                Windows.Storage.Streams.IBuffer ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.
                        Do_Abi_GetResults_4(thisPtr);
                    *__return_value__ = MarshalInterface<Windows.Storage.Streams.IBuffer>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_put_Progress_0(IntPtr thisPtr, IntPtr handler)
            {
                _ = AsyncOperationProgressHandler_IBuffer_uint.Initialized;
                try
                {
                    global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.Do_Abi_put_Progress_0(
                        thisPtr,
                        global::ABI.Windows.Foundation.AsyncOperationProgressHandler<Windows.Storage.Streams.IBuffer, uint>.FromAbi(handler));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_get_Progress_1(IntPtr thisPtr, IntPtr* __return_value__)
            {
                _ = AsyncOperationProgressHandler_IBuffer_uint.Initialized;
                global::Windows.Foundation.AsyncOperationProgressHandler<Windows.Storage.Streams.IBuffer, uint> ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.Do_Abi_get_Progress_1(thisPtr);
                    *__return_value__ = global::ABI.Windows.Foundation.AsyncOperationProgressHandler<Windows.Storage.Streams.IBuffer, uint>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_put_Completed_2(IntPtr thisPtr, IntPtr handler)
            {
                _ = AsyncOperationWithProgressCompletedHandler_IBuffer_uint.Initialized;
                try
                {
                    global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.Do_Abi_put_Completed_2(
                        thisPtr,
                        global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandler<Windows.Storage.Streams.IBuffer, uint>.FromAbi(handler));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_get_Completed_3(IntPtr thisPtr, IntPtr* __return_value__)
            {
                _ = AsyncOperationWithProgressCompletedHandler_IBuffer_uint.Initialized;
                global::Windows.Foundation.AsyncOperationWithProgressCompletedHandler<Windows.Storage.Streams.IBuffer, uint> ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.Do_Abi_get_Completed_3(thisPtr);
                    *__return_value__ = global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandler<Windows.Storage.Streams.IBuffer, uint>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        private static class AsyncOperationProgressHandler_IBuffer_uint
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                _ = global::ABI.Windows.Foundation.AsyncOperationProgressHandlerMethods<Windows.Storage.Streams.IBuffer, IntPtr, uint, uint>.InitCcw(&Do_Abi_Invoke);
                _ = global::ABI.Windows.Foundation.AsyncOperationProgressHandlerMethods<Windows.Storage.Streams.IBuffer, IntPtr, uint, uint>.InitRcwHelper(&Invoke);
                return true;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr asyncInfo, uint progressInfo)
            {
                try
                {
                    global::ABI.Windows.Foundation.AsyncOperationProgressHandlerMethods<Windows.Storage.Streams.IBuffer, IntPtr, uint, uint>.Abi_Invoke(
                        thisPtr,
                        MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<Windows.Storage.Streams.IBuffer, uint>>.FromAbi(asyncInfo),
                        progressInfo);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe void Invoke(
                IObjectReference objRef,
                global::Windows.Foundation.IAsyncOperationWithProgress<Windows.Storage.Streams.IBuffer, uint> asyncInfo,
                uint progressInfo)
            {
                IntPtr ThisPtr = objRef.ThisPtr;
                ObjectReferenceValue __asyncInfo = default;

                try
                {
                    __asyncInfo = MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<Windows.Storage.Streams.IBuffer, uint>>.CreateMarshaler2(
                        asyncInfo,
                        global::ABI.Windows.Foundation.IAsyncOperationWithProgressMethods<Windows.Storage.Streams.IBuffer, uint>.IID);
                    IntPtr abiAsyncInfo = MarshalInspectable<object>.GetAbi(__asyncInfo);

                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, int>**)ThisPtr)[3](
                        ThisPtr,
                        abiAsyncInfo,
                        progressInfo));
                    GC.KeepAlive(objRef);
                }
                finally
                {
                    MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<Windows.Storage.Streams.IBuffer, uint>>.DisposeMarshaler(__asyncInfo);

                }
            }
        }

        private static class AsyncOperationWithProgressCompletedHandler_IBuffer_uint
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                return global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandlerMethods<Windows.Storage.Streams.IBuffer, IntPtr, uint, uint>.
                    InitCcw(&Do_Abi_Invoke);
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr asyncInfo, global::Windows.Foundation.AsyncStatus asyncStatus)
            {
                try
                {
                    global::ABI.Windows.Foundation.AsyncOperationWithProgressCompletedHandlerMethods<Windows.Storage.Streams.IBuffer, IntPtr, uint, uint>.Abi_Invoke(
                        thisPtr,
                        MarshalInterface<global::Windows.Foundation.IAsyncOperationWithProgress<Windows.Storage.Streams.IBuffer, uint>>.FromAbi(asyncInfo),
                        asyncStatus);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        private static class IAsyncOperation_bool
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                return global::ABI.Windows.Foundation.IAsyncOperationMethods<bool, byte>.InitCcw(
                    &Do_Abi_put_Completed_0,
                    &Do_Abi_get_Completed_1,
                    &Do_Abi_GetResults_2
                );
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_GetResults_2(IntPtr thisPtr, byte* __return_value__)
            {
                bool ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationMethods<bool>.Do_Abi_GetResults_2(thisPtr);
                    *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_put_Completed_0(IntPtr thisPtr, IntPtr handler)
            {
                _ = AsyncOperationCompletedHandler_bool.Initialized;
                try
                {
                    global::ABI.Windows.Foundation.IAsyncOperationMethods<bool>.Do_Abi_put_Completed_0(thisPtr, global::ABI.Windows.Foundation.AsyncOperationCompletedHandler<bool>.FromAbi(handler));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_get_Completed_1(IntPtr thisPtr, IntPtr* __return_value__)
            {
                _ = AsyncOperationCompletedHandler_bool.Initialized;
                global::Windows.Foundation.AsyncOperationCompletedHandler<bool> ____return_value__ = default;

                *__return_value__ = default;

                try
                {
                    ____return_value__ = global::ABI.Windows.Foundation.IAsyncOperationMethods<bool>.Do_Abi_get_Completed_1(thisPtr);
                    *__return_value__ = global::ABI.Windows.Foundation.AsyncOperationCompletedHandler<bool>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }

        private static class AsyncOperationCompletedHandler_bool
        {
            private static readonly bool _initialized = Init();
            internal static bool Initialized => _initialized;

            private static unsafe bool Init()
            {
                return global::ABI.Windows.Foundation.AsyncOperationCompletedHandlerMethods<bool, byte>.InitCcw(
                    &Do_Abi_Invoke
                );
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
            private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr asyncInfo, global::Windows.Foundation.AsyncStatus asyncStatus)
            {
                try
                {
                    global::ABI.Windows.Foundation.AsyncOperationCompletedHandlerMethods<bool, byte>.Abi_Invoke(
                        thisPtr,
                        MarshalInterface<global::Windows.Foundation.IAsyncOperation<bool>>.FromAbi(asyncInfo),
                        asyncStatus);
                }
                catch (global::System.Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
    }
}
#endif