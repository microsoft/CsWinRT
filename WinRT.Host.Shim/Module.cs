// TODO: consider embedding this as a resource into WinRT.Host.dll, 
// to simplify deployment

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using WinRT;

namespace WinRT.Host
{
    public static class Shim
    {
        private const int S_OK = 0;
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        public unsafe delegate int GetActivationFactoryDelegate(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory);

        public static unsafe int GetActivationFactory(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory)
        {
            *activationFactory = IntPtr.Zero;

            var targetAssembly = MarshalString.FromAbi(hstrTargetAssembly);
            var runtimeClassId = MarshalString.FromAbi(hstrRuntimeClassId);

            try
            {
                // TODO: this may be risky - alternatives to ensure same ALC as shim?
                var assembly = Assembly.LoadFrom(targetAssembly);
                var type = assembly.GetType("WinRT.Module");
                if (type != null)
                {
                    var GetActivationFactory = type.GetMethod("GetActivationFactory");
                    if (GetActivationFactory != null)
                    {
                        IntPtr factory = (IntPtr)GetActivationFactory.Invoke(null, new object[] { runtimeClassId });
                        if (factory != IntPtr.Zero)
                        {
                            *activationFactory = factory;
                            return S_OK;
                        }
                    }
                }
                return REGDB_E_CLASSNOTREG;
            }
            catch (Exception e)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(e);
                return global::WinRT.ExceptionHelpers.GetHRForException(e);
            }
        }
    }
}
