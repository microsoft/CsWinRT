using System;
using System.Collections.Generic;
using System.Text;

namespace WinRT.Interop
{
    /// <summary>
    /// This type signals that the type it is applied to is projected into .NET from either a Windows.UI.Xaml type or a Microsoft.UI.Xaml type.
    /// For this type, the GuidAttribute is not used and instead the GetGuidSignature method must be called to get the IID or generic IID signature part of the type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    internal sealed class WuxMuxProjectedTypeAttribute : Attribute
    {
    }
}
