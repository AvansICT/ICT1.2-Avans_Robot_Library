using Avans.StatisticalRobot;
using System.Device.Gpio;

public class DHT11
{
    private readonly int _pin;

    /// <summary>
    /// Initializes the DHT11 sensor.
    /// </summary>
    /// <param name="pin">GPIO pin where the sensor is connected.</param>
    public DHT11(int pin)
    {
        Robot.SetDigitalPinMode(pin, PinMode.Output);
        _pin = pin;
    }

    /// <summary>
    /// Reads humidity and temperature from the DHT11 sensor.
    /// </summary>
    /// <returns>
    /// A tuple containing humidity (in percentage) and temperature (in Celsius).
    /// If the data is invalid, returns (0, 0).
    /// </returns>
    public DHT11Data GetSensorReadings(int maxRetries = 5)
    {
        int retries = 0;

        while (retries < maxRetries)
        {
            int[] data = ReadDht11Data();

            // Validate the data using checksum
            if (data.Length == 5 && data[4] == ((data[0] + data[1] + data[2] + data[3]) & 0xFF))
            {
                DHT11Data dHTData = new DHT11Data();
                dHTData.Humidity = data[0];
                dHTData.Temperature = data[2];
                return dHTData;
            }

            // Log the failure and retry
            Console.WriteLine($"Attempt {retries + 1} failed: Invalid data received from the DHT11 sensor.");
            retries++;
            Thread.Sleep(500);
        }

        Console.WriteLine("All attempts to read from the DHT11 sensor failed.");
        DHT11Data dHTDataEmpty = new DHT11Data();
        return dHTDataEmpty; // Return default values if all retries fail
    }

    /// <summary>
    /// Reads raw data from the DHT11 sensor.
    /// </summary>
    /// <returns>An array of 5 integers containing raw sensor data.</returns>
    private int[] ReadDht11Data()
    {
        int[] dht11_dat = new int[5];
        PinValue lastState = PinValue.High;
        byte counter;
        byte j = 0, i;

        Array.Clear(dht11_dat, 0, dht11_dat.Length);

        Robot.SetDigitalPinMode(_pin, PinMode.Output);
        Robot.WriteDigitalPin(_pin, PinValue.Low);
        Robot.Wait(18); // Wait 18ms to signal the start
        Robot.WriteDigitalPin(_pin, PinValue.High);
        Robot.WaitUs(40); // Wait 40us
        Robot.SetDigitalPinMode(_pin, PinMode.Input);

        for (i = 0; i < 85; i++)
        {
            counter = 0;
            while (Robot.ReadDigitalPin(_pin) == lastState)
            {
                counter++;
                Robot.WaitUs(2); // More precise timing
                if (counter == 255)
                {
                    break;
                }
            }
            lastState = Robot.ReadDigitalPin(_pin);

            if (counter == 255)
                break;

            if ((i >= 4) && (i % 2 == 0))
            {
                dht11_dat[j / 8] <<= 1;
                if (counter > 16)
                    dht11_dat[j / 8] |= 1;
                j++;
            }
        }

        // Check if data read is complete and return
        return dht11_dat;
    }
    // make a temperature and humidity klass
    public class DHT11Data
    {
        public int Temperature { get; set; }
        public int Humidity { get; set; }
    }
}
