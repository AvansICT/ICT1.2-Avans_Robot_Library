// This code is an AI translation into C# of the
// original source code for the Seeed Studio Grove Air Quality Sensor
// located at https://github.com/Seeed-Studio/Grove_Air_quality_Sensor/blob/master/Air_Quality_Sensor.cpp

// Kleine aanpassingen:
// AnalogRead was al in de Robot klasse aanwezig en is dus gebruikt
// Initialisatie met een pin nummer

namespace Avans.StatisticalRobot
{
    /// <summary>
    /// Represents the Air Quality Sensor V1.3
    /// Connect it to an analog input pin, call the Init() method and then call the Slope() method to obtain the air quality
    /// </summary>
    public class AirQualitySensor
    {
        /// <summary>
        /// Highest level of pollution that the sensor will detect
        /// </summary>
        public const int FORCE_SIGNAL = 0;
        /// <summary>
        /// Considerable pollution detected by the sensor
        /// </summary>
        public const int HIGH_POLLUTION = 1;
        /// <summary>
        /// Some pollution detected by the sensor
        /// </summary>
        public const int LOW_POLLUTION = 2;
        /// <summary>
        /// No pollution detected by the sensor
        /// </summary>
        public const int FRESH_AIR = 3;

        private byte _pin;
        private int _currentVoltage;
        private int _lastVoltage;
        private int _standardVoltage;
        private long _lastStdVolUpdated;

        private int _voltageSum;
        private int _volSumCount;

        /// <summary>
        /// Represents the Air Quality Sensor V1.3
        /// Connect the sensor to an analog input pin
        /// </summary>
        /// <param name="pin">The analog input pin for the sensor (e.g. 2 for pin A2)</param>
        public AirQualitySensor(byte pin)
        {
            _pin = pin;
            _voltageSum = 0;
            _volSumCount = 0;
        }

        /// <summary>
        /// Initialize the air quality sensor readings
        /// </summary>
        /// <returns>True if successful, false if not successful</returns>
        public bool Init()
        {
            int initVoltage = ReadAnalogValue();

            if (initVoltage > 10 && initVoltage < 798)
            {
                _currentVoltage = initVoltage;
                _lastVoltage = _currentVoltage;
                _standardVoltage = initVoltage;
                _lastStdVolUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a value that represents the air quality
        /// </summary>
        /// <returns>One of these predefined int values: FRESH_AIR, LOW_POLLUTION, HIGH_POLLUTION, FORCE_SIGNAL</returns>
        public int Slope()
        {
            _lastVoltage = _currentVoltage;
            _currentVoltage = ReadAnalogValue();

            _voltageSum += _currentVoltage;
            _volSumCount += 1;

            UpdateStandardVoltage();

            if (_currentVoltage - _lastVoltage > 400 || _currentVoltage > 700)
            {
                return FORCE_SIGNAL;
            }
            else if ((_currentVoltage - _lastVoltage > 400 && _currentVoltage < 700)
                     || _currentVoltage - _standardVoltage > 150)
            {
                return HIGH_POLLUTION;
            }
            else if ((_currentVoltage - _lastVoltage > 200 && _currentVoltage < 700)
                     || _currentVoltage - _standardVoltage > 50)
            {
                return LOW_POLLUTION;
            }
            else
            {
                return FRESH_AIR;
            }
        }

        /// <summary>
        /// Returns the current raw voltage produced by the sensor
        /// </summary>
        /// <returns>The value on the analog input pin</returns>
        public int GetValue()
        {
            return _currentVoltage;
        }

        private void UpdateStandardVoltage()
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (currentTime - _lastStdVolUpdated > 500000)
            {
                _standardVoltage = _voltageSum / _volSumCount;
                _lastStdVolUpdated = currentTime;
                _voltageSum = 0;
                _volSumCount = 0;
            }
        }

        private int ReadAnalogValue()
        {
            return Robot.AnalogRead(_pin);
        }
    }
}