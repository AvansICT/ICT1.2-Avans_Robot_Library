using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    /// <summary>Measures distance using a Grove ultrasonic sensor on a digital port.</summary>
    public class Ultrasonic
    {
        private readonly int _pin;

        /// <summary>Initializes an ultrasonic sensor on the specified digital pin, initially configured as an output.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        public Ultrasonic(int pin)
        {
            Robot.SetDigitalPinMode(pin, PinMode.Output);
            _pin = pin;
        }


        /// <summary>Sends a trigger pulse and measures the reflected pulse duration.</summary>
        /// <returns>The distance in whole centimeters; pulse failures produce 0 after integer conversion.</returns>
        public int GetUltrasoneDistance()
        {
            Robot.SetDigitalPinMode(_pin, PinMode.Output);
            Robot.WriteDigitalPin(_pin, PinValue.Low);
            Robot.Wait(1);
            Robot.WriteDigitalPin(_pin, PinValue.High);
            Robot.WaitUs(10);
            Robot.WriteDigitalPin(_pin, PinValue.Low);

            Robot.SetDigitalPinMode(_pin, PinMode.Input);
            int pulse = Robot.PulseIn(_pin, PinValue.High, 50);
            return pulse / 29 / 2;
        }
    }
}
