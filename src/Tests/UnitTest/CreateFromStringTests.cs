using System;
using System.Reflection;
using TestComponentCSharp;
using Windows.Foundation.Metadata;

namespace UnitTest
{
    [TestClass]
    public class CreateFromStringTests
    {
        [TestMethod]
        [DataRow(typeof(Class))]
        [DataRow(typeof(NonBlittableStringStruct))]
        public void TestCreateFromStringAttributeIsReferenceProjectionOnly(Type type)
        {
            Assert.IsNull(type.GetCustomAttribute<CreateFromStringAttribute>());
        }

        [TestMethod]
        [DataRow(typeof(NonAgileClass))]
        [DataRow(typeof(BlittableStruct))]
        public void TestTypeWithoutCreateFromStringAttribute(Type type)
        {
            Assert.IsNull(type.GetCustomAttribute<CreateFromStringAttribute>());
        }
    }
}
