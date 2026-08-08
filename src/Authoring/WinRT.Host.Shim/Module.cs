// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

namespace WinRT.Host;

/// <summary>
/// Provides the activation factory entry point used by the native <c>WinRT.Host</c> shim to host managed Windows Runtime components.
/// </summary>
public static class Shim
{
    private const int S_OK = 0;
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int REGDB_E_READREGDB = unchecked((int)0x80040150);
    private const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);

    /// <summary>
    /// Delegate matching the native signature used to retrieve an activation factory from a hosted component.
    /// </summary>
    public unsafe delegate int GetActivationFactoryDelegate(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory);

    private unsafe delegate void* ManagedExportsGetActivationFactoryDelegate(ReadOnlySpan<char> activatableClassId);

    private static HashSet<string> _InitializedResolvers;

    /// <summary>
    /// Retrieves the activation factory for a runtime class from the specified target assembly, loading it into the default load context.
    /// </summary>
    public static unsafe int GetActivationFactory(IntPtr hstrTargetAssembly, IntPtr hstrRuntimeClassId, IntPtr* activationFactory)
    {
        *activationFactory = IntPtr.Zero;

        string targetAssembly = HStringMarshaller.ConvertToManaged((void*)hstrTargetAssembly);
        string runtimeClassId = HStringMarshaller.ConvertToManaged((void*)hstrRuntimeClassId);

        try
        {
            Assembly assembly = LoadInDefaultContext(targetAssembly);

            Type managedExportsType = GetManagedExportsType(assembly, targetAssembly);
            if (managedExportsType == null)
            {
                return REGDB_E_READREGDB;
            }
            MethodInfo GetActivationFactory = managedExportsType.GetMethod("GetActivationFactory", [typeof(ReadOnlySpan<char>)]);
            if (GetActivationFactory == null)
            {
                return REGDB_E_READREGDB;
            }
            // ReadOnlySpan<char> is a ref struct and can't be used with MethodInfo.Invoke.
            // Use a delegate to call the method directly.
            ManagedExportsGetActivationFactoryDelegate del = GetActivationFactory.CreateDelegate<ManagedExportsGetActivationFactoryDelegate>();
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

    /// <summary>
    /// Finds the type holding the component's managed <c>GetActivationFactory</c> method.
    /// </summary>
    /// <remarks>
    /// The component assembly points at it with <c>[WindowsRuntimeComponentAssemblyExportsType]</c>, which is
    /// the authoritative answer. The name is otherwise only derivable by convention from the assembly name, and
    /// that does not survive a name needing escaping to form a namespace: assembly <c>Contoso.Widgets</c> holds
    /// its exports in <c>ABI.Contoso_Widgets</c>, not <c>ABI.Contoso.Widgets</c>. The convention is still tried
    /// as a fallback, for components built before the attribute was emitted.
    /// </remarks>
    private static Type GetManagedExportsType(Assembly assembly, string targetAssembly)
    {
        const string ExportsTypeAttributeName = "WindowsRuntime.InteropServices.WindowsRuntimeComponentAssemblyExportsTypeAttribute";

        foreach (CustomAttributeData attribute in assembly.GetCustomAttributesData())
        {
            if (attribute.AttributeType.FullName == ExportsTypeAttributeName &&
                attribute.ConstructorArguments is [{ Value: Type exportsType }])
            {
                return exportsType;
            }
        }

        // ABI.<ModuleName>.ManagedExports.GetActivationFactory(ReadOnlySpan<char>) -> void*
        string moduleName = Path.GetFileNameWithoutExtension(targetAssembly);

        return assembly.GetType($"ABI.{moduleName}.ManagedExports");
    }

    private static Assembly LoadInDefaultContext(string targetAssembly)
    {
        if (_InitializedResolvers == null)
        {
            _ = Interlocked.CompareExchange(ref _InitializedResolvers, new HashSet<string>(StringComparer.OrdinalIgnoreCase), null);
        }

        lock (_InitializedResolvers)
        {
            if (!_InitializedResolvers.Contains(targetAssembly))
            {
                AssemblyDependencyResolver resolver = new(targetAssembly);
                AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) =>
                {
                    string assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                    return assemblyPath != null ? assemblyLoadContext.LoadFromAssemblyPath(assemblyPath) : null;
                };

                _ = _InitializedResolvers.Add(targetAssembly);
            }
        }

        return AssemblyLoadContext.Default.LoadFromAssemblyPath(targetAssembly);
    }
}