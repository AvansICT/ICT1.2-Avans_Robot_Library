using System.Device.Gpio;
using System.Diagnostics; // For Stopwatch

namespace Avans.StatisticalRobot;

/// <summary>Reads temperature and relative humidity from a DHT11 digital sensor.</summary>
public class DHT11
{
    private static long StopwatchTicksPerMicrosecond = (long)(Stopwatch.Frequency * 0.000_001);
    
    // Define how often data can be read from the DHT11 (interval in seconds)
    private static TimeSpan MinimumDht11ReadInterval = TimeSpan.FromSeconds(2.0);
    private DateTime _lastDht11ReadTimestamp;
    
    // Set the pulse length threshold between a 0 bit and a 1 bit in the
    // middle between 50+28 (0 bit) and 50+70 (1 bit) microseconds
    private static int Dht11BitvalueThresholdMicroseconds = 99;
    
    private static long WakeupTimeoutMicroseconds = 300;
    
    private static int LoopCounterTimeout = 40000;

    // Array of 5 bytes to hold the data from the DHT11
    // Bytes 0 & 1 are for relative humidity, 2 & 3 are for temperature, 4 is a checksum
    private int[] _dhtData = new int[5];
    
    private readonly int _pin; // DHT11 data line is connected to this GPIO pin
    private GpioController _gpioController; // For accessing the GPIO pin
    
    private Stopwatch _stopwatch;

    private int _temperatureIntegral = 0;
    private int _temperatureDecimal = 0;
    /// <summary>
    /// Returns the most recently obtained temperature from the sensor (in degrees Celcius)
    /// </summary>
    public double Temperature {
        get {
            if (ReadyToReadNewDht11Data())
            {
                ReadDht11Data();
            }
            return _temperatureIntegral + (_temperatureDecimal / 10.0);
        }
    }

    private int _relativeHumidity = 0;
    /// <summary>
    /// Returns the most recently obtained relative humidity from the sensor (in %)
    /// </summary>
    public int RelativeHumidity {
        get {
            if (ReadyToReadNewDht11Data())
            {
                ReadDht11Data();
            }
            return _relativeHumidity;
        }
    }

    /// <summary>
    /// Represents the DHT11 relative humidity and temperature sensor
    /// Handles communication with the DHT11 sensor, including error checking
    /// Provides the temperature and relative humidity measurement values as properties
    /// </summary>
    /// <param name="pin">Pin number on the grove board to which the DHT11 is connected</param>
    public DHT11(int pin)
    {
        _pin = pin;
        _lastDht11ReadTimestamp = DateTime.Now - MinimumDht11ReadInterval; // So that first read is OK
        _gpioController = new GpioController();
        _gpioController.OpenPin(_pin);
        _stopwatch = new Stopwatch();
    }

    /// <summary>
    /// Returns true if last time that DHT11 was read is long enough ago for a new attempt
    /// </summary>
    /// <returns>True if enough time has passed to access the DHT11 again</returns>
    private bool ReadyToReadNewDht11Data()
    {
        // Return true if last access of DHT11 was more than the allowed interval ago
        return (DateTime.Now - _lastDht11ReadTimestamp) >= MinimumDht11ReadInterval;
    }

    /// <summary>
    /// Accesses the DHT11 and reads its sensor data (relative humidity and temperature)
    /// If the checksum is correct then stores the newly obtained values so that they
    /// can be obtained through the RelativeHumidity and Temperature properties
    /// Writes appropriate error messages to console
    /// </summary>
    private void ReadDht11Data()
    {
        // Pseudocode of what needs to be done to read data from DHT11:
        //
        // Generate a >= 18 millisecond low pulse on the pin
        // to wake up the DHT11 and start its data transfer
        // Switch pin to input and let it be pulled high
        //
        // After 20 to 40 microseconds DHT11 will pull pin
        // low for 80 microseconds, then high for
        // another 80 microseconds
        // Detect this falling edge, then rising edge
        //
        // Observe timeout (if no edges within approximately 300 microseconds,
        // then abort and ignore further signals)
        //
        // DHT11 sends 5 * 8 = 40 bits, highest bit first, in the following way:
        // DHT11 pulls pin low for 50 microseconds
        // DHT11 lets pin be pulled high for 26-28 microseconds for "0" bit
        // or pulled high for 70 microseconds for "1" bit
        //
        // So measure time between falling edge and next falling edge
        // If time is < 50 microseconds ==> error, abort
        // If time is > 120 + margin microseconds ==> error, abort
        // If time is >= 50 & <= 99 ==> "0"
        // If time is > 99 && <= 120 + margin ==> "1"


        if (!WakeDht11())
        {
            Console.WriteLine("Error: DHT11 failed to respond, check connections and pin number");
            return; // Give up, reuse old data
        }
        // Read the 5 bytes of data from the DHT11
        for (int i = 0; i < 5; i++)
        {
            _dhtData[i] = ReadByteFromDht11();
        }
        // Console.WriteLine($"DEBUG: bytes read {_dhtData[0]} {_dhtData[1]} {_dhtData[2]} {_dhtData[3]}   {_dhtData[4]}");
        // Perform checksum check (last byte should be the sum of the previous 4 bytes)
        if (_dhtData[4] == ((_dhtData[0] + _dhtData[1] + _dhtData[2] + _dhtData[3]) & 0xFF))
        {
            // Checksum is correct, so update relative humidity and temperature using new data
            _relativeHumidity = _dhtData[0]; // Ignore _dhtData[1], not used by DHT11
            _temperatureIntegral = _dhtData[2];
            _temperatureDecimal = _dhtData[3];
        }
        else
        {
            // Checksum is wrong so retain old data
            Console.WriteLine("Error: DHT11 data checksum incorrect, ignoring data");
        }
        _lastDht11ReadTimestamp = DateTime.Now;
    }

    /// <summary>
    /// Sends a low pulse to the DHT11 via the GPIO pin to wake it up and trigger its data transfer
    /// </summary>
    /// <returns>True if the DHT11 responds, false if it doesn't respond</returns>
    private bool WakeDht11()
    {
        _stopwatch.Reset();
        // Let the pin stabilize at high level
        _gpioController.SetPinMode(_pin, PinMode.Output);
        _gpioController.Write(_pin, PinValue.High);
        WaitMilliseconds(20);
        // Wake up the DHT11
        _gpioController.Write(_pin, PinValue.Low);
        WaitMilliseconds(18);
        _gpioController.Write(_pin, PinValue.High);
        // Let the pin stabilize at high level before switching to input
        WaitMicroseconds(20);
        _gpioController.SetPinMode(_pin, PinMode.Input);
        // Check if DHT11 responds
        _stopwatch.Start();
        PinValue pinValue;
        // Wait until pin goes low
        do { pinValue = _gpioController.Read(_pin);
        } while (_stopwatch.Elapsed.Microseconds < WakeupTimeoutMicroseconds && pinValue == PinValue.High);
        // Wait until pin goes high again
        do { pinValue = _gpioController.Read(_pin);
        } while (_stopwatch.Elapsed.Microseconds < WakeupTimeoutMicroseconds && pinValue == PinValue.Low);
        // If this took too long then return false
        _stopwatch.Stop();
        return _stopwatch.Elapsed.Microseconds < WakeupTimeoutMicroseconds;
    }

    /// <summary>
    /// Reads a single byte of data from the DHT11 bitstream via the GPIO pin
    /// Writes appropriate error messages to console
    /// </summary>
    /// <returns>The byte value obtained or -9999 in case of an error</returns>
    private int ReadByteFromDht11()
    {
        // Assume that the DHT11 is awake and sending data
        // but use a loop counter to time out in case the DHT11 stops sending pulses
        int loopCounter;
        int resultByte = 0;
        // Remember that bits are sent with the highest bit first, so start at bit 7
        for (int bit = 7; bit >= 0; bit--)
        {
            // Measure time from falling edge to falling edge
            // Ensure that the pin has gone low before we start the measurement
            loopCounter = LoopCounterTimeout; // Some very large value
            while (_gpioController.Read(_pin) == PinValue.High)
            {
                if (loopCounter-- == 0)
                {
                    Console.WriteLine("Error: DHT11 IO failed, invalid data");
                    return -9999;
                }
            }
            // Pin is low, start measurement
            _stopwatch.Reset();
            _stopwatch.Start();
            loopCounter = LoopCounterTimeout; // Some very large value
            while (_gpioController.Read(_pin) == PinValue.Low)
            {
                if (loopCounter-- == 0)
                {
                    Console.WriteLine("Error: DHT11 IO failed, invalid data");
                    return -9999;
                }
            }
            loopCounter = LoopCounterTimeout; // Some very large value
            while (_gpioController.Read(_pin) == PinValue.High)
            {
                if (loopCounter-- == 0)
                {
                    Console.WriteLine("Error: DHT11 IO failed, invalid data");
                    return -9999;
                }
            }
            // Pin has gone high and low again, stop measurement
            _stopwatch.Stop();
            // Determine from measured time if the bit was a 0 or a 1
            if (_stopwatch.Elapsed.Microseconds > Dht11BitvalueThresholdMicroseconds)
            {
                // Bit was a 1 so put it in the appropriate place in the byte
                resultByte |= 1 << bit;
            }
        }
        //Console.WriteLine($"DEBUG: byte read = {resultByte}");
        return resultByte;
    }

    /// <summary>
    /// Waits a given number of milliseconds (using Thread.Sleep)
    /// </summary>
    /// <param name="ms">Number of milliseconds</param>
    private void WaitMilliseconds(int ms)
    {
        Thread.Sleep(ms);
    }

    /// <summary>
    /// Waits a given number of microseconds
    /// </summary>
    /// <param name="us">Number of microseconds</param>
    // TODO Perhaps disable optimization?
    private void WaitMicroseconds(int us)
    {
        long startTicks = Stopwatch.GetTimestamp();
        long endTicks = startTicks + StopwatchTicksPerMicrosecond * us;
        long nowTicks;
        do { nowTicks = Stopwatch.GetTimestamp();
        } while (nowTicks < endTicks);
    }
}
