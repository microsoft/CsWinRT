// TODO: consider embedding this as a resource into WinRT.Host.dll, 
// to simplify deployment

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

#if NET
using System.Runtime.Loader;
using System.Threading;
[assembly: global::System.Runtime.Versioning.SupportedOSPlatform("Windows")]
#endif

namespace WinRT.Host
{
    public static class Shim
    {
        private const int S_OK = 0;
        private const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int REGDB_E_READREGDB = unchecked((int)0x80040150);
        private const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)(0x80040111));

        public unsafe delegate int GetActivationFactoryDelegate(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory);

#if NET
        private const string UseLoadComponentsInDefaultALCPropertyName = "CSWINRT_LOAD_COMPONENTS_IN_DEFAULT_ALC";
        private readonly static bool _IsLoadInDefaultContext = IsLoadInDefaultContext();

        private static HashSet<string> _InitializedResolversInDefaultContext = null;

        public static Assembly LoadInDefaultContext(string targetAssembly)
        {
            if (_InitializedResolversInDefaultContext == null)
            {
                Interlocked.CompareExchange(ref _InitializedResolversInDefaultContext, new HashSet<string>(StringComparer.OrdinalIgnoreCase), null);
            }

            lock (_InitializedResolversInDefaultContext)
            {
                if (!_InitializedResolversInDefaultContext.Contains(targetAssembly))
                {
                    var resolver = new AssemblyDependencyResolver(targetAssembly);
                    AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName) =>
                    {
                        string assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                        if (assemblyPath != null)
                        {
                            return assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
                        }
                        return null;
                    };

                    _InitializedResolversInDefaultContext.Add(targetAssembly);
                }
            }

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(targetAssembly);
        }

        public static bool IsLoadInDefaultContext()
        {
            if (AppContext.TryGetSwitch(UseLoadComponentsInDefaultALCPropertyName, out bool isEnabled))
            {
                return isEnabled;
            }

            return false;
        }
#endif

        public static unsafe int GetActivationFactory(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory)
        {
            *activationFactory = IntPtr.Zero;

            var targetAssembly = MarshalString.FromAbi(hstrTargetAssembly);
            var runtimeClassId = MarshalString.FromAbi(hstrRuntimeClassId);

            try
            {
#if NET
                Assembly assembly;
                if (_IsLoadInDefaultContext)
                {
                    assembly = LoadInDefaultContext(targetAssembly);
                }
                else
                {
                    assembly = ActivationLoader.LoadAssembly(targetAssembly);
                }
#else
                var assembly = ActivationLoader.LoadAssembly(targetAssembly);
#endif
                var type = assembly.GetType("WinRT.Module");
                if (type == null)
                {
                    return REGDB_E_READREGDB;
                }
                var GetActivationFactory = type.GetMethod("GetActivationFactory", new Type[] { typeof(string) });
                if (GetActivationFactory == null)
                {
                    return REGDB_E_READREGDB;
                }
                IntPtr factory = (IntPtr)GetActivationFactory.Invoke(null, new object[] { runtimeClassId });
                if (factory == IntPtr.Zero)
                {
                    return CLASS_E_CLASSNOTAVAILABLE;
                }
                *activationFactory = factory;
                return S_OK;
            }
            catch (Exception e)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(e);
                return global::WinRT.ExceptionHelpers.GetHRForException(e);
            }
        }

#if !NET
        private static class ActivationLoader
        {
            public static Assembly LoadAssembly(string targetAssembly) => Assembly.LoadFrom(targetAssembly);
        }
#else
        private class ActivationLoader : AssemblyLoadContext
        {
            private static readonly ConcurrentDictionary<string, ActivationLoader> ALCMapping = new ConcurrentDictionary<string, ActivationLoader>(StringComparer.Ordinal);
            private AssemblyDependencyResolver _resolver;

            public static Assembly LoadAssembly(string targetAssembly)
            {
                return ALCMapping.GetOrAdd(targetAssembly, (_) => new ActivationLoader(targetAssembly))
                    .LoadFromAssemblyPath(targetAssembly);
            }

            private ActivationLoader(string path)
            {
                _resolver = new AssemblyDependencyResolver(path);
                AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName) =>
                {
                    // Consolidate all WinRT.Runtime loads to the default ALC, or failing that, the first shim ALC 
                    if (string.CompareOrdinal(assemblyName.Name, "WinRT.Runtime") == 0)
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
                if (string.CompareOrdinal(assemblyName.Name, "WinRT.Runtime") != 0)
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
    }
}