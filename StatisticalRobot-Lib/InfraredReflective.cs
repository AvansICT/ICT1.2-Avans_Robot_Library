using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    /// <summary>Reads a Grove infrared reflective sensor at intervals of at least 50 ms.</summary>
    public class InfraredReflective
    {
        private readonly int _pin;

        /// <summary>Initializes an infrared reflective sensor on the specified digital input pin.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        public InfraredReflective(int pin)
        {
            Robot.SetDigitalPinMode(pin, PinMode.Input);
            _pin = pin;
        }

        private DateTime syncTime = new();
        private PinValue state;

        private int Update()
        {
            if (DateTime.Now - syncTime > TimeSpan.FromMilliseconds(50))
            {
                state = Robot.ReadDigitalPin(_pin);
                syncTime = DateTime.Now;
                return (int)state;
            }
            return -1;
        }

        /// <summary>Reads the raw digital sensor state when more than 50 milliseconds have elapsed since the last sample. Call regularly.</summary>
        /// <returns>1 for a HIGH input, 0 for a LOW input, or -1 when no new sample is available. Reflection polarity depends on the sensor.</returns>
        public int Watch()
        {
            return Update();
        }
        //TODO: double check the returns of the sensor
    }
}
