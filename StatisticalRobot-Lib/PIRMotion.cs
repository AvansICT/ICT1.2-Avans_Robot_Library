using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    /// <summary>Reads a Grove motion sensor with configurable polling and alert delays.</summary>
    public class PIRMotion
    {
        private readonly int _pin;
        private readonly int _intervalms;
        private readonly int _alerttime;

        /// <summary>Initializes a digital motion sensor with a polling interval and an additional delay after motion detection.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        /// <param name="intervalms">The minimum interval between sensor readings, in milliseconds.</param>
        /// <param name="alerttime">The additional delay before the next reading after motion is detected, in milliseconds.</param>
        public PIRMotion(int pin, int intervalms, int alerttime)
        {
            Robot.SetDigitalPinMode(pin, PinMode.Input);
            _pin = pin;
            _intervalms = intervalms;
            _alerttime = alerttime;
        }


        private DateTime syncTime = new();
        private PinValue state;

        private int Update()
        {
            if (DateTime.Now - syncTime > TimeSpan.FromMilliseconds(_intervalms))
            {
                state = Robot.ReadDigitalPin(_pin);
                if (state == PinValue.High)
                {
                    syncTime = DateTime.Now.AddMilliseconds(_alerttime);
                }
                else
                { 
                    syncTime = DateTime.Now;
                }
                return (int)state;
            }
            return -1;
        }
        /// <summary>Samples motion when the polling interval has elapsed. A HIGH sample postpones the next reading by the alert delay plus the polling interval.</summary>
        /// <returns>1 when motion is detected, 0 when no motion is detected, or -1 when no new sample is available.</returns>
        public int Watch()
        {
            return Update();
        }
        //TODO: see wheren it has -1
    }
}
