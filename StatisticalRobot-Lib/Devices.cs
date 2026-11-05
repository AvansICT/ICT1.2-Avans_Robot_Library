namespace Avans.StatisticalRobot;

/// <summary>Groups factory methods for Grove analog, digital, and I2C devices.</summary>
public class Devices
{

    /// <summary>Provides factory methods for devices connected to analog Grove ports.</summary>
    public class Analog
    {
        /// <summary>Creates an analog light sensor with the specified polling interval.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        /// <param name="intervalms">The minimum interval between sensor readings, in milliseconds.</param>
        public static LightSensor LightSensor(byte pin, int intervalms)
        {
            return new LightSensor(pin, intervalms);
        }
    }

    /// <summary>Provides factory methods for devices connected to digital Grove ports.</summary>
    public class Digital
    {
        /// <summary>Creates a digital LED with a configurable interval between state changes.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        /// <param name="msBlink">The interval between LED state changes, in milliseconds.</param>
        public static BlinkLed BlinkLed(int pin, int msBlink)
        {
            return new BlinkLed(pin, msBlink);
        }

        /// <summary>Creates a digital LED that can be switched on and off.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        public static Led Led(int pin)
        {
            return new Led(pin);
        }

        /// <summary>Creates a button reader on the specified digital input pin.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        /// <param name="defHigh">The default-high setting, stored by the button but currently unused when reading it.</param>
        public static Button Button(int pin, bool defHigh = false)
        {
            return new Button(pin, defHigh);
        }

        /// <summary>Creates a DHT11 temperature and humidity sensor on the specified digital pin.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        public static DHT11 TempHumidity(int pin)
        {
            return new DHT11(pin);
        }

        /// <summary>Creates an ultrasonic distance sensor on the specified digital pin.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        public static Ultrasonic Ultrasonic(int pin)
        {
            return new Ultrasonic(pin);
        }

        /// <summary>Creates a motion sensor with a polling interval and an additional delay after detection.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        /// <param name="intervalms">The minimum interval between sensor readings, in milliseconds.</param>
        /// <param name="alerttime">The additional delay before the next reading after motion is detected, in milliseconds.</param>
        public static PIRMotion PIRMotion(int pin, int intervalms, int alerttime)
        {
            return new PIRMotion(pin, intervalms, alerttime);
        }

        /// <summary>Creates a digital infrared reflective sensor reader.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        public static InfraredReflective InfraredReflective(int pin)
        {
            return new InfraredReflective(pin);
        }

        /// <summary>Creates a digital buzzer with a configurable interval between output state changes.</summary>
        /// <param name="pin">The pin number on the Grove board.</param>
        /// <param name="intervalms">The interval between buzzer output state changes, in milliseconds.</param>
        public static Buzzer Buzzer(int pin, int intervalms)
        {
            return new Buzzer(pin, intervalms);
        }
    }

    /// <summary>Provides factory methods for devices connected to the I2C bus.</summary>
    public class I2c
    {
        /// <summary>Creates a two-line, 16-character LCD at the specified I2C address.</summary>
        /// <param name="address">The LCD I2C address, for example 0x3E.</param>
        public static LCD16x2 LCD16x2(byte address)
        {
            return new LCD16x2(address);
        }
    }
}
