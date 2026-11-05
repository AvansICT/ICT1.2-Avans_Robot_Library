using System.Device.Gpio;

namespace Avans.StatisticalRobot;

/// <summary>Controls a Grove digital buzzer with an application-driven beep interval.</summary>
public class Buzzer
{
    private readonly int _pin;
    private readonly int _intervalms;

    /// <summary>Initializes a digital buzzer output with the specified interval between state changes.</summary>
    /// <param name="pin">The pin number on the Grove board.</param>
    /// <param name="intervalms">The interval between buzzer output state changes, in milliseconds.</param>
    public Buzzer(int pin, int intervalms)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Output);
        _pin = pin;
        _intervalms = intervalms;
    }

    private DateTime syncTime = new();
    private PinValue state;

    private void Update()
    {
        if (DateTime.Now - syncTime > TimeSpan.FromMilliseconds(_intervalms))
        {
            Robot.WriteDigitalPin(_pin, state);
            syncTime = DateTime.Now;
            state = !state;
        }
    }

    /// <summary>Updates the buzzer output when the configured interval has elapsed. Call regularly; stopping calls leaves the output in its last state.</summary>
    public void Beep()
    {
        Update();
    }
    //TODO: add a beep once functions and a stop beep function

}