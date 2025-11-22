using System.Device.Gpio;
using Avans.StatisticalRobot.Interfaces;

namespace Avans.StatisticalRobot;

public class BlinkLed : IUpdatable
{
    private readonly int _pin;
    private readonly int _msBlink;

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="msBlink">Time in milliseconds</param>
    public BlinkLed(int pin, int msBlink)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Output);
        _pin = pin;
        _msBlink = msBlink;
    }

    private DateTime lastBlink = new();
    private PinValue ledState = PinValue.Low;

    /// <summary>
    /// changes the leds staate from on to off
    /// when the time specified in the constructor has passed
    /// </summary>
    public void Update()
    {
        if(DateTime.Now - lastBlink > TimeSpan.FromMilliseconds(_msBlink))
        {
            ledState = !ledState;
            Robot.WriteDigitalPin(_pin, ledState);
            lastBlink = DateTime.Now;
        }
    }
}