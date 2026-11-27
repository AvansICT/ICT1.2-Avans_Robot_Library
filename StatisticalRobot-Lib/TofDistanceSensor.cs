using System.Diagnostics;
using Iot.Device.Vl53L0X;

namespace Avans.StatisticalRobot
{
    /// <summary>
    /// Represents the Time of Flight Distance Sensor based on an VL53L0X sensor chip
    /// Warning: uses the same I2C address as the RGB Color Sensor (so don't use both)
    /// </summary>
    public class TofDistanceSensor
    {
        /// <summary>
        /// The default I2C address for the Time of Flight Distance Sensor
        /// </summary>
        public static readonly byte DEFAULT_I2C_ADDRESS = 0x29;

        private readonly Vl53L0X vl53L0XSensorChip;

        /// <summary>
        /// The measured distance (in millimeters) to an object, measured by the sensor
        /// This value is NaN if an object is out of range of the sensor
        /// </summary>
        public double Distance
        {
            get
            {
                ushort distance;
                try
                {
                    distance = vl53L0XSensorChip.Distance;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                    distance = (int) OperationRange.OutOfRange;
                }
                if (distance == (ushort) OperationRange.OutOfRange)
                {
                    // Use NaN (Not a Number) for out of range measurements
                    return double.NaN;
                }
                return (double) distance;
            }
        }

        /// <summary>
        /// Represents the ToF Distance Sensor
        /// </summary>
        /// <param name="address">The I2C address of the sensor</param>
        public TofDistanceSensor(byte address)
        {
            vl53L0XSensorChip = new Vl53L0X(Robot.CreateI2cDevice(address));
            // Let us use continuous measurement mode
            // and rely on the defaults set by the IoT library for the VL53L0X
            vl53L0XSensorChip.MeasurementMode = MeasurementMode.Continuous;
        }

        /// <summary>
        /// Represents the ToF Distance Sensor
        /// </summary>
        public TofDistanceSensor() : this(DEFAULT_I2C_ADDRESS)
        {}
    }
}