namespace ClassLibrary2
{
    public sealed class Greeter
    {
        public string Greet(string name)
        {
            return $"Hello from ClassLibrary2, {name}!";
        }

        public string Shout(string text)
        {
            return text.ToUpperInvariant() + "!!!";
        }
    }
}
