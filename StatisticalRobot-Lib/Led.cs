using System.Device.Gpio;

namespace Avans.StatisticalRobot;

/// <summary>Switches a Grove digital LED on or off.</summary>
public class Led
{
    private readonly int _pin;

    /// <summary>Initializes an LED on the specified digital output pin.</summary>
    /// <param name="pin">The pin number on the Grove board.</param>
    public Led(int pin)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Output);
        _pin = pin;
    }
    /// <summary>Turns the LED on by setting its output pin HIGH.</summary>
    public void SetOn()
    {
        Robot.WriteDigitalPin(_pin, PinValue.High);
    }
    /// <summary>Turns the LED off by setting its output pin LOW.</summary>
    public void SetOff()
    {
        Robot.WriteDigitalPin(_pin, PinValue.Low);
    }
}