using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Foundation;
using WinRT;


namespace TestHost
{
    public class ManagedClass : IStringable
    {
        public override string ToString() => "ManagedClass";
    }
}
