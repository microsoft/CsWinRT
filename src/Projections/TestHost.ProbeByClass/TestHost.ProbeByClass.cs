using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using WinRT;

namespace WinRT.Host
{
    internal partial class ActivationFactory : WinRT.Interop.IActivationFactory
    {
        public ConstructorInfo Constructor { get; private set; }

        public ActivationFactory(ConstructorInfo constructor) => Constructor = constructor;

        public IntPtr ActivateInstance()
        {
            return MarshalInspectable<object>.FromManaged(Constructor.Invoke(null));
        }
    }
}

namespace WinRT
{
    public static class Module
    {
#if NET
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(TestHost.ProbeByClass))]
        [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "We're manually keeping the target type around.")]

#endif
        public static unsafe IntPtr GetActivationFactory(string runtimeClassId)
        {
            if (string.CompareOrdinal(runtimeClassId, "TestHost.ProbeByClass") == 0)
            {
                var type = Type.GetType(runtimeClassId);
                if (type != null)
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        var factory = new WinRT.Host.ActivationFactory(ctor);
                        return MarshalInspectable<WinRT.Host.ActivationFactory>.FromManaged(factory);
                    }
                }
            }
            return IntPtr.Zero;
        }
    }
}

namespace TestHost
{
    public partial class ProbeByClass : IStringable
    {
#if NET
        [UnconditionalSuppressMessage("SingleFile", "IL3000", Justification = "We're not publishing this test as single file.")]
#endif
        public override string ToString()
        {
            return new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Name;
        }
    }
}
