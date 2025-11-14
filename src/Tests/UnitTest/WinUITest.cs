using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace UnitTest;

public class TestWinUI
{
    public TestWinUI()
    {
    }

    public class App : Microsoft.UI.Xaml.Application
    {

    }

    // Compile time test to ensure multiple allowed attributes 
    [TemplatePart(Name = "PartButton", Type = typeof(Button))]
    [TemplatePart(Name = "PartGrid", Type = typeof(Grid))]
    public class TestAllowMultipleAttributes { };

    [Fact]
    public void TestApp()
    {
        // TODO: load up some MUX!
        //Assert.Equal(true, true);
    }
}
