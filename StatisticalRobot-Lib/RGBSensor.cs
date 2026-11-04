using System;
using System.Threading;
using System.Device.I2c;

namespace Avans.StatisticalRobot
{


    /// <summary>Reads raw red, green, blue, and clear channels from a TCS34725 I2C sensor.</summary>
    public class RGBSensor
    {
        /// <summary>The default I2C address of the color sensor (0x29).</summary>
        public const byte DEFAULT_I2C_ADDRESS = 0x29;
        private const byte TCS34725_COMMAND_BIT = 0x80;
        private const byte TCS34725_ENABLE = 0x00;
        private const byte TCS34725_ENABLE_PON = 0x01;
        private const byte TCS34725_ENABLE_AEN = 0x02;
        private const byte TCS34725_ID = 0x12;
        private const byte TCS34725_ATIME = 0x01;
        private const byte TCS34725_CONTROL = 0x0F;
        private const byte TCS34725_CDATAL = 0x14;
        private const byte TCS34725_RDATAL = 0x16;
        private const byte TCS34725_GDATAL = 0x18;
        private const byte TCS34725_BDATAL = 0x1A;

        /// <summary>Defines sensor integration times used to collect a color sample.</summary>
        public enum IntegrationTime : byte
        {

            /// <summary>Integration time of approximately 2.4 milliseconds.</summary>
            INTEGRATION_TIME_2_4MS = 0xFF,
            /// <summary>Integration time of approximately 24 milliseconds.</summary>
            INTEGRATION_TIME_24MS = 0xF6,
            /// <summary>Integration time of approximately 50 milliseconds.</summary>
            INTEGRATION_TIME_50MS = 0xEB,
            /// <summary>Integration time of approximately 101 milliseconds.</summary>
            INTEGRATION_TIME_101MS = 0xD5,
            /// <summary>Integration time of approximately 154 milliseconds.</summary>
            INTEGRATION_TIME_154MS = 0xC0,
            /// <summary>Integration time of approximately 700 milliseconds.</summary>
            INTEGRATION_TIME_700MS = 0x00
        }

        /// <summary>Defines the amplification applied to the sensor's color channels.</summary>
        public enum Gain : byte
        {
            /// <summary>No gain (1x).</summary>
            GAIN_1X = 0x00,
            /// <summary>4x gain.</summary>
            GAIN_4X = 0x01,
            /// <summary>16x gain.</summary>
            GAIN_16X = 0x02,
            /// <summary>60x gain.</summary>
            GAIN_60X = 0x03
        }

        private I2cDevice _device;
        private bool _initialized;
        private IntegrationTime _integrationTime;
        private Gain _gain;

        /// <summary>Creates a color sensor using 700 ms integration time and 1x gain.</summary>
        /// <param name="address">The sensor's I2C address.</param>
        public RGBSensor(byte address)
        {
            _device = Robot.CreateI2cDevice(address);
            _integrationTime = (byte)IntegrationTime.INTEGRATION_TIME_700MS;
            _gain = (byte)Gain.GAIN_1X;
            _initialized = false;
        }
        /// <summary>
        /// This is a I2C device for reading RGB colors.
        /// </summary>
        /// <param name="address">I2C address of device</param>
        /// <param name="integrationTime">Time the device takes after reading</param>
        /// <param name="gain">Sets the sensitivity (not tested)</param>
        public RGBSensor(byte address, IntegrationTime integrationTime, Gain gain)
        {
            _device = Robot.CreateI2cDevice(address);
            _integrationTime = integrationTime;
            _gain = gain;
            _initialized = false;
        }

        private void Write8(byte reg, byte value)
        {
            _device.Write(new byte[] { (byte)(TCS34725_COMMAND_BIT | reg), value });
        }

        private byte Read8(byte reg)
        {
            _device.WriteByte((byte)(TCS34725_COMMAND_BIT | reg));
            byte[] readBuffer = new byte[1];
            _device.Read(readBuffer);
            return readBuffer[0];
        }

        private ushort Read16(byte reg)
        {
            _device.WriteByte((byte)(TCS34725_COMMAND_BIT | reg));
            byte[] readBuffer = new byte[2];
            _device.Read(readBuffer);
            return (ushort)((readBuffer[1] << 8) | readBuffer[0]);
        }

        /// <summary>Powers on the sensor and enables color measurement.</summary>
        public void Enable()
        {
            Write8(TCS34725_ENABLE, TCS34725_ENABLE_PON);
            Thread.Sleep(3);
            Write8(TCS34725_ENABLE, (byte)(TCS34725_ENABLE_PON | TCS34725_ENABLE_AEN));
        }

        /// <summary>Disables color measurement and powers down the sensor.</summary>
        public void Disable()
        {
            byte reg = Read8(TCS34725_ENABLE);
            Write8(TCS34725_ENABLE, (byte)(reg & ~(TCS34725_ENABLE_PON | TCS34725_ENABLE_AEN)));
        }

        /// <summary>Checks the device ID, applies the configured settings, and enables measurement.</summary>
        /// <returns>True if the device ID is supported; otherwise, false.</returns>
        public bool Begin()
        {
            byte x = Read8(TCS34725_ID);
            if (x != 0x44 && x != 0x10)
            {
                return false;
            }
            _initialized = true;
            SetIntegrationTime(_integrationTime);
            SetGain(_gain);
            Enable();
            return true;
        }

        /// <summary>Sets the color channel amplification, initializing the sensor if needed.</summary>
        /// <param name="gain">The amplification setting to apply.</param>
        public void SetGain(Gain gain)
        {
            if (!_initialized)
            {
                Begin();
            }
            Write8(TCS34725_CONTROL, (byte)gain);
            _gain = gain;
        }

        /// <summary>Sets the sample integration time, initializing the sensor if needed.</summary>
        /// <param name="integrationTime">The time setting to apply to color measurements.</param>
        public void SetIntegrationTime(IntegrationTime integrationTime)
        {
            if (!_initialized)
            {
                Begin();
            }
            Write8(TCS34725_ATIME, (byte)integrationTime);
            _integrationTime = integrationTime;

        }

        private void SleepForIntegrationTime()
        {
            switch (_integrationTime)
            {
                case IntegrationTime.INTEGRATION_TIME_2_4MS:
                    Thread.Sleep(3);
                    break;
                case IntegrationTime.INTEGRATION_TIME_24MS:
                    Thread.Sleep(24);
                    break;
                case IntegrationTime.INTEGRATION_TIME_50MS:
                    Thread.Sleep(50);
                    break;
                case IntegrationTime.INTEGRATION_TIME_101MS:
                    Thread.Sleep(101);
                    break;
                case IntegrationTime.INTEGRATION_TIME_154MS:
                    Thread.Sleep(154);
                    break;
                case IntegrationTime.INTEGRATION_TIME_700MS:
                    Thread.Sleep(700);
                    break;
            }
        }


        /// <summary>
        /// GetRawData can be used to get data from the Sensor.
        /// Be aware that this method uses the Integration time as set in the constructor after reading the data.
        /// </summary>
        /// <param name="r">returns the value of the RED channel</param>
        /// <param name="g">returns the value of the GREEN channel</param>
        /// <param name="b">returns the value of the BLUE channel</param>
        /// <param name="c">returns the value of the CLEAR channel</param>

        public void GetRawData(out ushort r, out ushort g, out ushort b, out ushort c)
        {
            if (!_initialized)
            {
                Begin();
            }
            c = Read16(TCS34725_CDATAL);
            r = Read16(TCS34725_RDATAL);
            g = Read16(TCS34725_GDATAL);
            b = Read16(TCS34725_BDATAL);

            SleepForIntegrationTime();
        }
    }
}
