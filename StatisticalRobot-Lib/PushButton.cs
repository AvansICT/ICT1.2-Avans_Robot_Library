using System.Device.Gpio;

namespace Avans.StatisticalRobot;

/// <summary>
/// Represents a push button and provides two ways to check if it is pressed
/// </summary>
public class PushButton
{
    private readonly int _pin;
    private readonly bool _pressedIsLow;

    // Preserve last pin states separately for pressed check and for released check
    // to avoid interference between the checks
    private PinValue _lastPressedPinState; 
    private PinValue _lastReleasedPinState;

    /// <summary>
    /// The PinValue that corresponds with a pressed button
    /// </summary>
    private PinValue PinValueButtonDown
    {
        get { return _pressedIsLow ? PinValue.Low : PinValue.High; }
    }
    /// <summary>
    /// The PinValue that corresponds with a button that is not pressed
    /// </summary>
    private PinValue PinValueButtonUp
    {
        get { return _pressedIsLow ? PinValue.High : PinValue.Low; }
    }

    /// <summary>
    /// Represents a push button and provides two ways to check if it is pressed:
    /// both the current state (currently pressed or not) and the change in that
    /// state (pressed or released)
    /// Pass a boolean value false as the second parameter if the button produces a
    /// High pin value when pressed (the default is true, i.e. the button produces
    /// a Low pin value when pressed)
    /// </summary>
    /// <param name="pin">Pin number on grove board</param>
    /// <param name="pressedIsLow">True (the default) if button pressed equals PinValue.Low, false if pressed equals PinValue.High</param>
    public PushButton(int pin, bool pressedIsLow = true)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Input);
        _lastPressedPinState = Robot.ReadDigitalPin(pin);
        _lastReleasedPinState = _lastPressedPinState;
        _pin = pin;
        _pressedIsLow = pressedIsLow;
    }

    /// <summary>
    /// Checks if the button was previously released but is now pressed
    /// Returns true only once when the button is held down, even when called multiple times
    /// </summary>
    /// <returns>True if the button was previously not pressed but is now pressed</returns>
    public bool IsPressed()
    {
        PinValue currentState = Robot.ReadDigitalPin(_pin);
        bool isPressed =
            currentState == PinValueButtonDown && _lastPressedPinState == PinValueButtonUp;
        _lastPressedPinState = currentState;
        return isPressed;
    }

    /// <summary>
    /// Checks if the button was previously pressed but is now released
    /// Returns true only once, even when called multiple times
    /// </summary>
    /// <returns>True if the button was previously pressed but is now not pressed</returns>
    public bool IsReleased()
    {
        PinValue currentState = Robot.ReadDigitalPin(_pin);
        bool isReleased =
            currentState == PinValueButtonUp && _lastReleasedPinState == PinValueButtonDown;
        _lastReleasedPinState = currentState;
        return isReleased;
    }

    /// <summary>
    /// Checks if the button is pressed at this moment, and ignores if the button was previously pressed or not
    /// Returns true repeatedly when called multiple times
    /// </summary>
    /// <returns>True if the button is pressed at this moment</returns>
    public bool IsDown() => Robot.ReadDigitalPin(_pin) == PinValueButtonDown;

    /// <summary>
    /// Checks if the button is not pressed at this moment, and ignores if the button was previously pressed or not
    /// Returns true repeatedly when called multiple times
    /// </summary>
    /// <returns>True if the button is not pressed at this moment</returns>
    public bool IsUp() => Robot.ReadDigitalPin(_pin) == PinValueButtonUp;

}