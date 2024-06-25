using System;

namespace WinRT.Interop
{
    // This attribute is only used by the IIDOptimizer for resolving the signature of a type that has different
    // IIDs on WUX/MUX targets. The actual IIDs are hardcoded in WinRT.Runtime, so they are not needed here.
    // TODO: remove this entirely when either IIDOptimizer is removed, or when the option to hardcode IIDs is added.

    /// <summary>
    /// This type signals that the type it is applied to is projected into .NET from either a Windows.UI.Xaml type or a Microsoft.UI.Xaml type.
    /// For this type, the GuidAttribute is not used and instead the GetGuidSignature method must be called to get the IID or generic IID signature part of the type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    internal sealed class WuxMuxProjectedTypeAttribute : Attribute
    {
    }
}
