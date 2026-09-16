namespace AuthoringTest2;

public interface IAdder
{
    int Add(int a, int b);
}

public struct AuthoredValue
{
    public int Value;
}

public enum AuthoredValueKind
{
    Answer = 42,
}

public delegate int AuthoredValueCallback(int value);
