using System.ComponentModel;
using System.Device.Gpio;
using System.Diagnostics;

namespace Avans.StatisticalRobot
{
    /// <summary>
    /// Represents the Grove Ultrasonic Ranger distance sensor
    /// Handles triggering of the sensor and measuring the echo pulse duration
    /// Can also calculate the distance from the echo pulse duration, provided that
    /// the speed of sound is set
    /// </summary>
    public class UltrasonicRanger
    {
        private static long StopwatchTicksPerMicrosecond = (long)(Stopwatch.Frequency / 1_000_000.0);

        // Maximum pulse duration that the Grove Ultrasonic Ranger sensor can produce
        // seems to be approx. 30 ms (measured with an oscilloscope)
        // so choose the timeout value slightly lower to avoid false readings when out of range
        private static long PulseTimeoutMicroseconds = 29000; // microseconds
        private static long PulseTimeoutTicks = PulseTimeoutMicroseconds * Stopwatch.Frequency / 1_000_000;

        // Minimum interval between two measurements depends on the maximum pulse duration,
        // i.e. the longest time that the sensor will wait for an echo before
        // giving up and ending the echo pulse prematurely
        private static readonly TimeSpan MeasurementInterval = TimeSpan.FromMilliseconds(100);

        private readonly int _pin;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly GpioController _gpioController = new GpioController();
        private DateTime _lastMeasurementTimestamp = DateTime.MinValue;

        private int _pulseDurationMicroseconds;
        private double _distanceMillimeters;

        // Define the speed of sound as a static class member because it is shared by all sensors
        private static double _speedOfSoundMetersPerSecond = double.NaN; // Speed of sound in m/s is typically 343

        /// <summary>
        /// The speed of sound in air, expressed in meters per second (m/s). This value must be positive.
        /// Distance calculations are only performed when this value is set.
        /// </summary>
        public static double SpeedOfSound
        {
            get
            {
                return _speedOfSoundMetersPerSecond;
            }

            set
            {
                if (value > 0.0)
                {
                    _speedOfSoundMetersPerSecond = value;
                }
                else
                {
                    // Don't allow negative speed of sound
                    _speedOfSoundMetersPerSecond = double.NaN;
                }
            }
        }

        /// <summary>
        /// The time (in microseconds) that it takes for the reflected ultrasonic sound pulse
        /// to travel from an object back to the sensor (i.e. the one way time)
        /// This value is zero if an object is out of range of the sensor
        /// If the sensor is not ready to perform a new measurement, the previous measurement
        /// values will be returned
        /// </summary>
        public int EchoTime
        {
            get
            {
                if (ReadyToReadPulse())
                {
                    PerformMeasurement();
                }
                return _pulseDurationMicroseconds / 2; // Echo time is half the pulse duration
            }
        }

        /// <summary>
        /// The measured distance (in millimeters) to an object, calculated from the echo time
        /// using the currently set value for the speed of sound
        /// This value is NaN if an object is out of range of the sensor
        /// If the sensor is not ready to perform a new measurement, the previous measurement
        /// values will be returned
        /// </summary>
        public double Distance
        {
            get
            {
                // Check for out of range, indicated by a pulse duration of zero
                if (EchoTime == 0)
                {
                    // Timeout occurred, signalling that an object is out of range of the sensor
                    _distanceMillimeters = double.NaN;
                }
                else if (double.IsNaN(SpeedOfSound))
                {
                    // No speed of sound defined so we cannot calculate the distance
                    _distanceMillimeters = double.NaN;
                }
                else
                {
                    // Calculate distance in millimeters
                    _distanceMillimeters = EchoTime * SpeedOfSound / 1000; ;                        
                }

                return _distanceMillimeters;
            }
        }

        /// <summary>
        /// Represents the Grove Ultrasonic Ranger distance sensor
        /// The speed of sound is undefined (NaN, Not a Number) until it
        /// is explicitly set
        /// </summary>
        /// <param name="pin">Pin number on grove board</param>
        public UltrasonicRanger(int pin)
        {
            _gpioController.OpenPin(pin);
            _pin = pin;
        }

        /// <summary>
        /// Start a measurement, measure the echo pulse length and convert it to a distance value
        /// </summary>
        private void PerformMeasurement()
        {
            // Store the current time as the moment of the latest measurement
            // so we can determine later whether enough time has passed to start a new measurement
            _lastMeasurementTimestamp = DateTime.UtcNow;
            // Start the measurement
            TriggerSensor();
            _pulseDurationMicroseconds = ReadPulseLengthMicroseconds();
        }

        /// <summary>
        /// Check if enough time has passed since the previous measurement
        /// to start a new measurement
        /// </summary>
        /// <returns>True if enough time has passed to start a new measurement</returns>
        private bool ReadyToReadPulse()
        {
            return (DateTime.UtcNow - _lastMeasurementTimestamp) >= MeasurementInterval;
        }

        // Trigger the sensor to start a measurement by transmitting an ultrasonic sound pulse
        // and start listening for the echo
        private void TriggerSensor()
        {
            // Produce a 10 microsecond high trigger pulse on the pin
            _gpioController.SetPinMode(_pin, PinMode.Output);
            _gpioController.Write(_pin, PinValue.Low);
            WaitMicroseconds(10);
            _gpioController.Write(_pin, PinValue.High);
            WaitMicroseconds(10);
            _gpioController.Write(_pin, PinValue.Low);
            WaitMicroseconds(10);
            // Switch back to input for the pulse duration measurement
            _gpioController.SetPinMode(_pin, PinMode.Input);
        }

        /// <summary>
        /// Measure the length of the echo pulse in microseconds
        /// </summary>
        /// <returns>The length of the entire echo pulse in microseconds, or zero when out of range</returns>
        private int ReadPulseLengthMicroseconds()
        {
            // Wait for the pin to go high, i.e. for the echo pulse to start
            _stopwatch.Restart(); // For timeout detection
            while (_gpioController.Read(_pin) == PinValue.Low)
            {
                if (_stopwatch.ElapsedTicks > PulseTimeoutTicks)
                {
                    return 0; // Timeout detected, pulse too long, indicating out of range
                }
            }
            // Start the measurement in order to measure the echo pulse from when the pin
            // goes high until it goes low again
            _stopwatch.Restart();
            while (_gpioController.Read(_pin) == PinValue.High)
            {
                if (_stopwatch.ElapsedTicks > PulseTimeoutTicks)
                {
                    return 0; // Timeout detected, pulse too long, out of range
                }
            }
            // Stop the pulse length measurement and convert the measurement from ticks to microseconds
            _stopwatch.Stop();
            return (int)(_stopwatch.ElapsedTicks / StopwatchTicksPerMicrosecond);
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
}
