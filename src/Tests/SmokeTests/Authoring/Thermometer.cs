using System;

namespace Authoring;

// A richer set of authored Windows Runtime types, exercised end-to-end by the projection smoke
// test: building the component produces 'Authoring.winmd', which the projection smoke test then
// generates a reference projection for. Between them, these cover the main projection shapes:
// enums, a flags enum, a struct, a delegate, an interface, and a runtime class with multiple
// constructors, instance methods, properties, an event, and static members.

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

[Flags]
public enum SensorCapabilities : uint
{
    None = 0,
    Temperature = 1,
    Humidity = 2,
    All = Temperature | Humidity
}

public struct Measurement
{
    public int Value;
    public Season Season;
}

public delegate void TemperatureChangedHandler(int previousValue, int currentValue);

public interface IThermometer
{
    int Temperature { get; }

    void Reset();
}

public sealed class Thermometer : IThermometer
{
    private int _temperature;

    public event TemperatureChangedHandler? TemperatureChanged;

    public Thermometer()
    {
    }

    public Thermometer(int initialTemperature)
    {
        _temperature = initialTemperature;
    }

    public static int AbsoluteZero => -273;

    public int Temperature => _temperature;

    public string Label { get; set; }

    public Season CurrentSeason { get; set; }

    public SensorCapabilities Capabilities { get; set; }

    public static Thermometer CreateFreezing()
    {
        return new Thermometer(0);
    }

    public void Reset()
    {
        SetTemperature(0);
    }

    public void SetTemperature(int value)
    {
        int previousValue = _temperature;

        _temperature = value;

        TemperatureChanged?.Invoke(previousValue, value);
    }

    public Measurement Measure()
    {
        return new Measurement { Value = _temperature, Season = CurrentSeason };
    }
}
