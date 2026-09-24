namespace ProjectionLibrary;

// Use a projected type, so that this library actually compiles against the reference projection
public static class GreeterExtensions
{
    public static string GreetWorld(Authoring.Greeter greeter)
    {
        return greeter.Greet("World");
    }
}
