using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    public class UnitTestBase
    {
        static UnitTestBase()
        {
            ProjectionInitializer.RegisterCustomProjections();
        }
    }
}
