using System.ComponentModel;
using Microsoft.UI.Xaml;
using WindowsRuntime.Xaml;

namespace ObjectLifetimeTests;

// Do not add a property provider or a WinRT interface to either outer source. They
// must exercise the runtime's opaque CCW, not a generated per-type interface table.
public class OpaqueBindingSourceBase : DependencyObject
{
    public static readonly DependencyProperty IsOnScreenProperty = DependencyProperty.Register(
        nameof(IsOnScreen), typeof(bool), typeof(OpaqueBindingSourceBase), new PropertyMetadata(false));

    public static readonly DependencyProperty VisibleAreaProperty = DependencyProperty.Register(
        nameof(VisibleArea), typeof(OpaqueVisibleArea), typeof(OpaqueBindingSourceBase), new PropertyMetadata(null));

    public bool IsOnScreen
    {
        get => (bool)GetValue(IsOnScreenProperty);
        set => SetValue(IsOnScreenProperty, value);
    }

    public OpaqueVisibleArea VisibleArea
    {
        get => (OpaqueVisibleArea)GetValue(VisibleAreaProperty);
        set => SetValue(VisibleAreaProperty, value);
    }
}

public sealed class OpaqueElementSource : OpaqueBindingSourceBase;

public sealed class OpaqueResourceSource : DependencyObject
{
    public static readonly DependencyProperty OutputProperty = DependencyProperty.Register(
        nameof(Output), typeof(string), typeof(OpaqueResourceSource), new PropertyMetadata(null));

    public OpaqueResourceSource()
    {
        SetOutput("resource-initial");
    }

    // The CLR getter is deliberately read-only; changes still come from the DP.
    public string Output => (string)GetValue(OutputProperty);

    public void SetOutput(string value) => SetValue(OutputProperty, value);
}

// Only the nested CLR object has an explicit provider, as in the original repro.
// That must not hide a failure to resolve VisibleArea on the opaque outer source.
[GeneratedCustomPropertyProvider]
public sealed partial class OpaqueVisibleArea : INotifyPropertyChanged
{
    private double _visibleHeightRatio;

    public double VisibleHeightRatio
    {
        get => _visibleHeightRatio;
        set
        {
            if (_visibleHeightRatio != value)
            {
                _visibleHeightRatio = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisibleHeightRatio)));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
