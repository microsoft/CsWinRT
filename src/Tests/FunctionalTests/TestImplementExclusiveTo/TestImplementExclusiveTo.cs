using System;
using TestComponentCSharp.TestPublicExclusiveTo;

namespace TestImplementExclusiveTo
{
    public sealed class TestClass : INonUniqueClass, IRegularInterface, INonUniqueClassFactory
    {
        public int Type => throw new NotImplementedException();
        public string Path => throw new NotImplementedException();
        public int StaticProperty => throw new NotImplementedException();
    }
}
