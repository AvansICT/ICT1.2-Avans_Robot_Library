namespace Avans.StatisticalRobot
{
    // ! Made by Snufie | https://github.com/Snufie
    /// <summary>
    /// Driver for the SeeedStudio Grove Multichannel Gas Sensor V2 (I2C).
    /// 
    /// This class implements the same protocol used by Seeed's Arduino
    /// library: send a single command byte (selecting a channel or action)
    /// and read 4 bytes (LSB-first) which represent a 32-bit ADC measurement.
    /// 
    /// Notes:
    /// - The default I2C address is 0x08.
    /// - Use <see cref="CalcVoltage"/> to convert ADC to volts (defaults match Arduino).
    /// - This file uses the project's I2C extension helpers (I2cDeviceExtensions).
    /// </summary>
    /// <remarks>
    /// Create the sensor using the project's Robot helper to create an I2C device.
    /// </remarks>
    /// <param name="address">7-bit I2C address (defaults to 0x08)</param>
    public sealed class MultichannelGasSensor(byte address = DefaultI2cAddress)
    {
        // Commands (matching Seeed's Arduino library)
        private const byte CMD_GM_102B = 0x01; // NO2 or Nitrogen Dioxide
        private const byte CMD_GM_302B = 0x03; // C2H5OH or Ethanol
        private const byte CMD_GM_502B = 0x05; // VOC or Volatile Organic Compound
        private const byte CMD_GM_702B = 0x07; // CO or Carbon Monoxide
        private const byte CMD_CHANGE_I2C_ADDR = 0x55; // Change I2C address
        private const byte CMD_WARMING_UP = 0xFE; // Start preheating
        private const byte CMD_WARMING_DOWN = 0xFF; // Stop preheating

        /// <summary>
        /// Default I2C address for the module.
        /// </summary>
        public const int DefaultI2cAddress = 0x08;

        // Hold I2cDevice as a property (created in ctor like LCD16x2)
        private I2cDevice Device { get; } = Robot.CreateI2cDevice(address);

        // Note: dispose removed to match LCD16x2 style. Ensure Robot or caller
        // takes ownership of the I2cDevice lifetime if needed.

        /// <summary>
        /// Send the preheat (warm up) command to the module.
        /// </summary>
        public void Preheat()
        {
            Device.WriteRegister(CMD_WARMING_UP);
        }

        /// <summary>
        /// Stop preheating the module.
        /// </summary>
        public void StopPreheat()
        {
            Device.WriteRegister(CMD_WARMING_DOWN);
        }

        /// <summary>
        /// Read the ADC value for the GM102B (NO2) channel.
        /// </summary>
        public uint GetNO2() => ReadUInt32LE(CMD_GM_102B);

        /// <summary>
        /// Read the ADC value for the GM302B (C2H5OH / Ethanol) channel.
        /// </summary>
        public uint GetEthanol() => ReadUInt32LE(CMD_GM_302B);

        /// <summary>
        /// Read the ADC value for the GM502B (VOC) channel.
        /// </summary>
        public uint GetVOC() => ReadUInt32LE(CMD_GM_502B);

        /// <summary>
        /// Read the ADC value for the GM702B (CO) channel.
        /// </summary>
        public uint GetCO() => ReadUInt32LE(CMD_GM_702B);

        /// <summary>
        /// Read all four gas channel ADC values in a single call.
        /// </summary>
        /// <returns>Tuple with (NO2, Ethanol, VOC, CO) ADC values.</returns>
        public (uint NO2, uint Ethanol, uint VOC, uint CO) MeasureAll()
        {
            return (GetNO2(), GetEthanol(), GetVOC(), GetCO());
        }

        /// <summary>
        /// Change the I2C address of the module.
        /// After changing the address the I2C device created by Robot.CreateI2cDevice
        /// will still be configured for the old address; recreate the device to use the new address.
        /// </summary>
        /// <param name="newAddress">New 7-bit address (1..127). If invalid, DefaultI2cAddress is used.</param>
        public void ChangeAddress(int newAddress)
        {
            if (newAddress == 0 || newAddress > 127)
            {
                newAddress = DefaultI2cAddress;
            }

            // Write two bytes: command and new address
            Device.WriteRegister(CMD_CHANGE_I2C_ADDR, new byte[] { (byte)newAddress });
        }

        /// <summary>
        /// Convert a raw ADC reading (as returned by the sensor) to a voltage.
        /// Default values are 3.3V and 1023 resolution (10-bit ADC).
        /// </summary>
        public static float CalcVoltage(uint adc, float verf = 3.3f, int resolution = 1023)
        {
            if (resolution <= 0) throw new ArgumentOutOfRangeException(nameof(resolution));
            return adc * verf / (resolution * 1.0f);
        }

        private uint ReadUInt32LE(byte command)
        {
            Span<byte> buffer = stackalloc byte[4];
            // Use extension helper to write command and read 4 bytes
            Device.ReadRegister(command, buffer);

            return (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }
    }
}