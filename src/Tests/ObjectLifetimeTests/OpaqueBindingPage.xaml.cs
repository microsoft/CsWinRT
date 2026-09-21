using Microsoft.UI.Xaml.Controls;

namespace ObjectLifetimeTests;

public sealed partial class OpaqueBindingPage : UserControl
{
    public OpaqueBindingPage()
    {
        InitializeComponent();
    }

    public OpaqueElementSource ElementSource => ElementSourceObject;

    public OpaqueResourceSource ResourceSource => (OpaqueResourceSource)Resources["OutputSource"];
}
