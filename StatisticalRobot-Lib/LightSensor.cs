using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    /// <summary>Reads raw analog light sensor values at a configured interval.</summary>
    public class LightSensor
    {
        private readonly byte _pin;
        private readonly int _intervalms;

        /// <summary>Configures an analog light sensor channel and the minimum interval between readings.</summary>
        /// <param name="pin">The analog channel index on the Grove board, such as 0 for A0 or 2 for A2.</param>
        /// <param name="intervalms">Minimum time between readings, in milliseconds.</param>
        public LightSensor(byte pin, int intervalms)
        {
            _pin = pin;
            _intervalms = intervalms;
        }

        private DateTime syncTime = new();

        private int Update()
        {
            if (DateTime.Now - syncTime > TimeSpan.FromMilliseconds(_intervalms))
            {
                syncTime = DateTime.Now;
                int pulse = Robot.AnalogRead(_pin);
                return pulse;
            }
            return -1;
        }
        /// <summary>
        /// Reads the analog value from the light sensor when the read interval has passed.
        /// </summary>
        /// <returns>The analog reading, or -1 when no new sample is available.</returns>
        public int GetLux()
        {
            return Update();
        }
    }
}
