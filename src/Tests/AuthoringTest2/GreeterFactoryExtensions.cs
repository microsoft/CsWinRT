using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using WindowsRuntime.InteropServices;

namespace AuthoringTest2;

/// <summary>
/// A COM interop interface (not a Windows Runtime one) exposed on the activation factory for <see cref="Greeter"/>.
/// </summary>
[GeneratedComInterface]
[Guid("6E1D3C6B-2E5B-4C8A-9E4F-3A7B1F2C9D10")]
internal partial interface IGreeterFactoryInterop
{
    int GetMagicNumber();
}

/// <summary>
/// Extends the activation factory CsWinRT generates for <see cref="Greeter"/>, which derives from this class and so
/// also exposes <see cref="IGreeterFactoryInterop"/> on its vtable. Activation and statics are still generated as usual.
/// </summary>
[WindowsRuntimeActivationFactory(typeof(Greeter))]
public abstract class GreeterFactoryExtensions : IGreeterFactoryInterop
{
    int IGreeterFactoryInterop.GetMagicNumber()
    {
        return 42;
    }
}
