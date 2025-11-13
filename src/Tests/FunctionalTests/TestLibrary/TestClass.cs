using TestComponentCSharp;

namespace TestLibrary;

public class TestClass
{
    // Used to test multiple modules with generated lookup vtables.
    public void InitList()
    {
        Class instance = new Class();
        var expected = new int[] { 0, 1, 2 };
        instance.BindableIterableProperty = expected;
    }
}
