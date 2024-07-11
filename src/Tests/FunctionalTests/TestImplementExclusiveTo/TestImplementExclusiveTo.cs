using TestComponentCSharp.TestPublicExclusiveTo;

namespace TestLibrary
{
    public sealed class TestClass : INonUniqueClass, IRegularInterface, INonUniqueClassFactory
    {
        int Type { get; } => throw new NotImplementedException();
        string Path { get; } => throw new NotImplementedException();
        int StaticProperty { get; } => throw new NotImplementedException();
    }
}
