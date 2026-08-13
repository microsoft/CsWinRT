using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("Windows")]

namespace ComponentUsingProjection;

/// <summary>
/// A Windows Runtime class, authored here, whose surface uses a type that comes from the packaged
/// projection. That is what puts the projected type into this component's own generated projection,
/// which is where a type declared by both the packaged reference assembly and the projection
/// generated for this build would be ambiguous.
/// </summary>
public sealed class GreeterFactory
{
    public Authoring.Greeter CreateGreeter()
    {
        return new Authoring.Greeter();
    }

    public string GreetWith(Authoring.Greeter greeter, string name)
    {
        return greeter.Greet(name);
    }
}

file sealed class Program
{
    // A component is activated by its consumer, so nothing needs to happen here. The entry point
    // exists only so that building this project runs the generators an application build runs.
    private static void Main()
    {
    }
}
