using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    public class InfraredReflective
    {
        private readonly int _pin;

        /// <summary>
        /// This is a digital device
        /// 3.3V/5V
        /// Distance: 0 - 4.5 cm (best height is 1.2 cm)
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
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

        /// <summary>
        /// must be called every tick.
        /// will check if the infrared reflective sensor is getting a reflecion back
        /// will return PinValue.High (1) if it is reflective and PinValue.Low (0) if it isn't
        /// </summary>
        /// <returns>an integer</returns>
        public int Watch()
        {
            return Update();
        }
        //TODO: double check the returns of the sensor
    }
}
