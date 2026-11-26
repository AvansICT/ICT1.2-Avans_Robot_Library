namespace Avans.StatisticalRobot;

/// <summary>Groups factory methods for Grove analog, digital, and I2C devices.</summary>
public class Devices
{

    /// <summary>Provides factory methods for devices connected to analog Grove ports.</summary>
    public class Analog
    {
        /// <summary>
        /// 3.3V/5V
        /// Maxlux detected: 350lux
        /// Responsetime: 20 ~ 30 milliseconds
        /// Peak wavelength: 540 nm
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="intervalms">Time between measures in milliseconds</param>
        public static LightSensor LightSensor(byte pin, int intervalms)
        {
            return new LightSensor(pin, intervalms);
        }
    }

    /// <summary>Provides factory methods for devices connected to digital Grove ports.</summary>
    public class Digital
    {
        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="msBlink">Time in milliseconds</param>
        public static BlinkLed BlinkLed(int pin, int msBlink)
        {
            return new BlinkLed(pin, msBlink);
        }

        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static Led Led(int pin)
        {
            return new Led(pin);
        }

        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="defHigh">button has default behaviour: HIGH</param>
        public static Button Button(int pin, bool defHigh = false)
        {
            return new Button(pin, defHigh);
        }

        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static DHT11 TempHumidity(int pin)
        {
            return new DHT11(pin);
        }

        /// <summary>
        /// 3.3V/5V
        /// Detecting range: 0-4m
        /// Resolution: 1cm
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static Ultrasonic Ultrasonic(int pin)
        {
            return new Ultrasonic(pin);
        }

        /// <summary>
        /// 3.3V/5V
        /// Max Distance:2-5m (2m by default)
        /// Angle: X=110° Y=90°
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="intervalms">Time between measures in milliseconds</param>
        /// <param name="alerttime">Time to use for alerting after detection in milliseconds</param>
        public static PIRMotion PIRMotion(int pin, int intervalms, int alerttime)
        {
            return new PIRMotion(pin, intervalms, alerttime);
        }

        /// <summary>
        /// This is a digital device
        /// 3.3V/5V
        /// Distance: 0 - 4.5 cm (best height is 1.2 cm)
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public static InfraredReflective InfraredReflective(int pin)
        {
            return new InfraredReflective(pin);
        }

        /// <summary>
        /// This is a digital device
        /// Beeps (On/Off)
        /// 3.3V/5V
        /// Resonant Frequency: 2300±300Hz
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        /// <param name="intervalms">Time in milliseconds between beeps</param>
        public static Buzzer Buzzer(int pin, int intervalms)
        {
            return new Buzzer(pin, intervalms);
        }
    }

    /// <summary>Provides factory methods for devices connected to the I2C bus.</summary>
    public class I2c
    {
        /// <summary>
        /// 3.3V/5V
        /// </summary>
        /// <param name="address">Address for example: 0x3E</param>
        public static LCD16x2 LCD16x2(byte address)
        {
            return new LCD16x2(address);
        }
    }
}
