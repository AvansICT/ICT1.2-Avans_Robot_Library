using System.Device.Gpio;
using System.Diagnostics;

namespace Avans.StatisticalRobot;

public class Button
{
    private readonly int _pin;
    private readonly bool _defHigh;
    private bool _previousState = false;

    private int _upDelay = 100;
    private Stopwatch watch = new Stopwatch();

    /// <summary>
    /// This is a digital device
    /// 3.3V/5V
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="defHigh">button has default behaviour: HIGH</param>
    public Button(int pin, bool defHigh = false)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Input);
        _pin = pin;
        _defHigh = defHigh;
    }

    /// <summary>
    /// returns "Released" when button is up and "Pressed" when button is down
    /// </summary>
    /// <returns>string</returns>
    public string GetState()
    {
        return (Robot.ReadDigitalPin(_pin) == PinValue.High) ? "Released" : "Pressed";
    }

    /// <summary>
    /// returns True when button is up and False when button is down
    /// </summary>
    /// <returns>boolean</returns>
    public bool GetStateBool()
    {
        return (Robot.ReadDigitalPin(_pin) == PinValue.Low);
    }
    /// <summary>
    /// only returns true on a state change of down to up, must be called every cycle of your program
    /// 
    /// there is a minimum delay between up calls to prevent double inputs (this can happen if the button is making a partial connection)
    /// this can be changed with SetMinDelay()
    /// </summary>
    /// <returns>boolean</returns>
    public bool GetButtonUp()
    {
        bool current = GetStateBool();
        bool retVal = false;
        if (!current && _previousState && !watch.IsRunning){
            retVal = true;
            watch.Start();
        }
        else if(watch.ElapsedMilliseconds > _upDelay)
        {
            watch.Reset();
        }
        
        _previousState = current;
        return retVal;
    }

    /// <summary>
    /// changes the minimum amount of milliseconds between button up signals
    /// </summary>
    /// <param name="delay">the minimum number of milliseconds between a button up</param>
    public void SetMinDelay(int delay)
    {
        _upDelay = delay;
    }
}