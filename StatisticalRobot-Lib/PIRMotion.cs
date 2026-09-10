using System.Device.Gpio;

namespace Avans.StatisticalRobot
{
    public class PIRMotion
    {
        private readonly int _pin;
        private readonly int _intervalms;
        private readonly int _alerttime;

        /// <summary>
        /// This is a digital device
        /// 3.3V/5V
        /// Max Distance:2-5m (2m by default)
        /// Angle: X=110° Y=90°
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="intervalms">Time between measures in milliseconds</param>
        /// <param name="alerttime">Time to use for alerting after detection in milliseconds</param>
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
        /// <summary>
        /// checks to see if there is motion at the time of the interval and then checks again for the entire alert time
        /// </summary>
        /// <returns>an integer with PinValue.High (1) when motion is detect and PinValue.Low (0) when not detected</returns>
        public int Watch()
        {
            return Update();
        }
        //TODO: see wheren it has -1
    }
}
