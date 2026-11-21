using System;
using System.Reflection;
using TestComponentCSharp;
using Windows.Foundation.Metadata;
using Xunit;

namespace UnitTest
{
    public class CreateFromStringTests
    {
        [Theory]
        [InlineData(typeof(Class), "TestComponentCSharp.Class.CreateFromString")]
        [InlineData(typeof(NonBlittableStringStruct), "TestComponentCSharp.Class.CreateStructFromString")]
        public void TestCreateFromStringAttribute(Type type, string methodName)
        {
            var attribute = type.GetCustomAttribute<CreateFromStringAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal(methodName, attribute.MethodName);
        }

        [Theory]
        [InlineData(typeof(NonAgileClass))]
        [InlineData(typeof(BlittableStruct))]
        public void TestTypeWithoutCreateFromStringAttribute(Type type)
        {
            Assert.Null(type.GetCustomAttribute<CreateFromStringAttribute>());
        }
    }
}
