using System;
using Xunit;

namespace UnitTestEmbedded
{
    public class TestClass
    {
        TestEmbeddedLibrary.TestLib TestLib = new();

        [Fact]
        public void Test1()
        {
            Assert.Equal(10, TestLib.Test1());
        }

        [Fact]
        public void Test2()
        {
            Assert.Equal(5, TestLib.Test2());
        }

        [Fact]
        public void Test3()
        {
            (bool, bool) pair = TestLib.Test3();
            Assert.True(!pair.Item1);
            Assert.True(pair.Item2);
        }

        [Fact]
        public void Test4()
        {
            Assert.Equal(5, TestLib.Test4());
        }

        [Fact]
        public void Test5()
        {
            TestLib.Test5();
            Assert.True(true);
        }
    }
}
