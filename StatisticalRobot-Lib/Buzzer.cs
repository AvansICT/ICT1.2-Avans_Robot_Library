using System.Device.Gpio;

namespace Avans.StatisticalRobot;

public class Buzzer
{
    private readonly int _pin;
    private readonly int _intervalms;

    /// <summary>
    /// This is a digital device
    /// Beeps (On/Off)
    /// 3.3V/5V
    /// Resonant Frequency: 2300±300Hz
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="intervalms">Time in milliseconds between beeps</param>
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

    /// <summary>
    /// makes the buzzer beep rithmically with the interval specified in the contructor
    /// 
    /// it changes state every interval if you stop calling this while it's state is high it will continue with one indefinite beep
    /// </summary>
    public void Beep()
    {
        Update();
    }
    //TODO: add a beep once functions and a stop beep function

}