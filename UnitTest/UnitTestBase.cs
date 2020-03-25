using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    public class UnitTestBase
    {
        static UnitTestBase()
        {
#if !NETCOREAPP2_0
            WinRT.ComWrappersSupport.InitializeComWrappers();
#endif
        }
    }
}
