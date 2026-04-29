// TODO: consider embedding this as a resource into WinRT.Host.dll, 
// to simplify deployment

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using WindowsRuntime.InteropServices.Marshalling;

[assembly: global::System.Runtime.Versioning.SupportedOSPlatform("Windows")]

#pragma warning disable CSWINRT3001 // Type or member is obsolete

namespace WinRT.Host;

public static class Shim
{
    private const int S_OK = 0;
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int REGDB_E_READREGDB = unchecked((int)0x80040150);
    private const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)(0x80040111));

    public unsafe delegate int GetActivationFactoryDelegate(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory);

    private unsafe delegate void* ManagedExportsGetActivationFactoryDelegate(ReadOnlySpan<char> activatableClassId);

    private static HashSet<string> _InitializedResolvers;

    public static unsafe int GetActivationFactory(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory)
    {
        *activationFactory = IntPtr.Zero;

        var targetAssembly = HStringMarshaller.ConvertToManaged((void*)hstrTargetAssembly);
        var runtimeClassId = HStringMarshaller.ConvertToManaged((void*)hstrRuntimeClassId);

        try
        {
            Assembly assembly = LoadInDefaultContext(targetAssembly);

            // ABI.<ModuleName>.ManagedExports.GetActivationFactory(ReadOnlySpan<char>) -> void*
            string moduleName = Path.GetFileNameWithoutExtension(targetAssembly);
            var managedExportsType = assembly.GetType($"ABI.{moduleName}.ManagedExports");
            if (managedExportsType == null)
            {
                return REGDB_E_READREGDB;
            }
            var GetActivationFactory = managedExportsType.GetMethod("GetActivationFactory", new Type[] { typeof(ReadOnlySpan<char>) });
            if (GetActivationFactory == null)
            {
                return REGDB_E_READREGDB;
            }
            // ReadOnlySpan<char> is a ref struct and can't be used with MethodInfo.Invoke.
            // Use a delegate to call the method directly.
            var del = GetActivationFactory.CreateDelegate<ManagedExportsGetActivationFactoryDelegate>();
            void* factory = del(runtimeClassId.AsSpan());
            if (factory == null)
            {
                return CLASS_E_CLASSNOTAVAILABLE;
            }
            *activationFactory = (IntPtr)factory;
            return S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    private static Assembly LoadInDefaultContext(string targetAssembly)
    {
        if (_InitializedResolvers == null)
        {
            Interlocked.CompareExchange(ref _InitializedResolvers, new HashSet<string>(StringComparer.OrdinalIgnoreCase), null);
        }

        lock (_InitializedResolvers)
        {
            if (!_InitializedResolvers.Contains(targetAssembly))
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

                _InitializedResolvers.Add(targetAssembly);
            }
        }

        return AssemblyLoadContext.Default.LoadFromAssemblyPath(targetAssembly);
    }
}