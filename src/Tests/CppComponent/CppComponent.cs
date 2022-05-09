namespace CppComponent
{
    public enum E1
    {
        V1=42,
        V2=99,
    }

    public interface Interface1
    {
        void Gloop();

        string TheString { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct S
    {
        public string Field;
    }

    public sealed class Class1
    {
        public Class1();

        public void f(int x, E1 y);
    }

}