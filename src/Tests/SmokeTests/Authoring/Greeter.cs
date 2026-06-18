namespace Authoring;

/// <summary>
/// A minimal Windows Runtime class used to smoke-test the C#/WinRT authoring pipeline
/// (WinMD generation, the reference projection, and the forwarder assembly) against the
/// real NuGet package.
/// </summary>
public sealed class Greeter
{
    /// <summary>
    /// Returns a greeting for the given name.
    /// </summary>
    /// <param name="name">The name to greet.</param>
    /// <returns>A greeting message.</returns>
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}
