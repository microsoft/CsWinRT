// TODO: consider embedding this as a resource into WinRT.Host.dll, 
// to simplify deployment

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !NETSTANDARD2_0
using System.Runtime.Loader;
#endif
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

#if NETSTANDARD2_0
        public static class ActivationLoader
        {
            public static Assembly LoadAssembly(string targetAssembly) => Assembly.LoadFrom(targetAssembly);
        }
#else
        class ActivationLoader : AssemblyLoadContext
        {
            private AssemblyDependencyResolver _resolver;

            public static Assembly LoadAssembly(string targetAssembly)
            {
                var loader = new ActivationLoader(targetAssembly);
                return loader.LoadFromAssemblyPath(targetAssembly);
            }

            private ActivationLoader(string path)
            {
                _resolver = new AssemblyDependencyResolver(path);
                AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName) =>
                {
                    // Consolidate all WinRT.Runtime loads to the default ALC, or failing that, the first shim ALC 
                    if (assemblyName.Name == "WinRT.Runtime")
                    {
                        string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                        if (assemblyPath != null)
                        {
                            return LoadFromAssemblyPath(assemblyPath);
                        }
                    }
                    return null;
                };
            }


            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (assemblyName.Name != "WinRT.Runtime")
                {
                    string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                    if (assemblyPath != null)
                    {
                        return LoadFromAssemblyPath(assemblyPath);
                    }
                }

                return null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (libraryPath != null)
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }

                return IntPtr.Zero;
            }
        }
#endif

        public static unsafe int GetActivationFactory(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory)
        {
            *activationFactory = IntPtr.Zero;

            var targetAssembly = MarshalString.FromAbi(hstrTargetAssembly);
            var runtimeClassId = MarshalString.FromAbi(hstrRuntimeClassId);

            try
            {
                var assembly = ActivationLoader.LoadAssembly(targetAssembly);
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

