using System.Device.Gpio;
using System.Diagnostics;

namespace Avans.StatisticalRobot;

/// <summary>Reads the state of a Grove button connected to a digital pin.</summary>
public class Button
{
    private readonly int _pin;
    private readonly bool _defHigh;
    private bool _previousState = false;

    private int _upDelay = 100;
    private Stopwatch watch = new Stopwatch();

    /// <summary>
    /// Initializes a button connected to the specified digital pin.
    /// The pin is configured as an input.
    /// </summary>
    /// <param name="pin">The digital pin number on the Grove board.</param>
    /// <param name="defHigh">The default-high setting, stored but currently unused when reading the button.</param>
    public Button(int pin, bool defHigh = false)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Input);
        _pin = pin;
        _defHigh = defHigh;
    }

    /// <summary>
    /// Returns the button state, interpreting HIGH as released and LOW as pressed.
    /// </summary>
    /// <returns>"Released" when the pin is HIGH; otherwise, "Pressed".</returns>
    public string GetState()
    {
        return (Robot.ReadDigitalPin(_pin) == PinValue.High) ? "Released" : "Pressed";
    }

    /// <summary>
    /// Reads whether the button is pressed, interpreting LOW as pressed.
    /// </summary>
    /// <returns>True when the pin is LOW (pressed); otherwise, false.</returns>
    public bool GetStateBool()
    {
        return (Robot.ReadDigitalPin(_pin) == PinValue.Low);
    }
    /// <summary>
    /// Detects a transition from pressed to released. Call this method on every polling cycle.
    /// 
    /// Release events are suppressed while the minimum-delay timer is running to reduce duplicate inputs.
    /// Configure the delay with SetMinDelay().
    /// </summary>
    /// <returns>True when a release transition is detected and the delay timer is not running; otherwise, false.</returns>
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
    /// Sets the minimum delay between reported button-release events.
    /// </summary>
    /// <param name="delay">The minimum delay in milliseconds.</param>
    public void SetMinDelay(int delay)
    {
        _upDelay = delay;
    }
}