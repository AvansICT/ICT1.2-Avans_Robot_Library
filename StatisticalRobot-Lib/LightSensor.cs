using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    public class LightSensor
    {
        private readonly byte _pin;
        private readonly int _intervalms;

        /// <summary>
        /// This is a analog device
        /// 3.3V/5V
        /// Maxlux detected: 350lux
        /// Responsetime: 20 ~ 30 milliseconds
        /// Peak wavelength: 540 nm
        /// </summary>
        /// <param name="pin">Pin number on grove board (A0=0, A2=2)</param>
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
