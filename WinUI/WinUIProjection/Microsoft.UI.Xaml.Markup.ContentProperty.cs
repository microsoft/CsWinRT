using System;

namespace Microsoft.UI.Xaml.Markup
{
    // This is a workaround for cs/winrt not projecting attributes, once we get that
    // we can remove this
    public class ContentPropertyAttribute : Attribute
    {
        public ContentPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}