using System.Device.Gpio;

namespace Avans.StatisticalRobot;

public class Speaker
{
    private readonly int _pin;
    private readonly GpioController _gpio;

    public Speaker(int pin)
    {
        _pin = pin;
        _gpio = new GpioController();
        Initialize();
    }

    private void Initialize()
    {
        _gpio.OpenPin(_pin, PinMode.Output);
        _gpio.Write(_pin, PinValue.Low);
    }

    public void PlayNote(int periodMicroseconds, int duration = 100)
    {
        if (periodMicroseconds <= 0)
            return;

        for (int i = 0; i < duration; i++)
        {
            _gpio.Write(_pin, PinValue.High);
            Thread.Sleep(TimeSpan.FromMicroseconds(periodMicroseconds));
            _gpio.Write(_pin, PinValue.Low);
            Thread.Sleep(TimeSpan.FromMicroseconds(periodMicroseconds));
        }
    }

    public void PlaySequence(int[] notePeriods, int delayBetweenNotes = 500)
    {
        foreach (int period in notePeriods)
        {
            PlayNote(period);
            Thread.Sleep(delayBetweenNotes);
        }
    }

    public void Silence()
    {
        _gpio.Write(_pin, PinValue.Low);
    }

    public void Dispose()
    {
        _gpio.Dispose();
    }
}