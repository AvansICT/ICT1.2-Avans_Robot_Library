using System;
using System.Device.I2c;
using System.Numerics;
using System.Threading;

namespace Avans.StatisticalRobot
{
    /// <summary>Accelerometer full-scale range of the ICM-20600.</summary>
    public enum AccelRange : byte
    {
        G2 = 0,   // ±2 g  (16384 LSB/g)
        G4 = 1,   // ±4 g  (8192 LSB/g)
        G8 = 2,   // ±8 g  (4096 LSB/g)
        G16 = 3   // ±16 g (2048 LSB/g)
    }

    /// <summary>Gyroscope full-scale range of the ICM-20600.</summary>
    public enum GyroRange : byte
    {
        Dps250 = 0,   // ±250 °/s  (131 LSB/(°/s))
        Dps500 = 1,   // ±500 °/s  (65.5 LSB/(°/s))
        Dps1000 = 2,  // ±1000 °/s (32.8 LSB/(°/s))
        Dps2000 = 3   // ±2000 °/s (16.4 LSB/(°/s))
    }

    /// <summary>
    /// Driver for the TDK ICM-20600 6-axis accelerometer + gyroscope
    /// (the "LCM20600" on the Grove - IMU 9DOF module).
    /// Default I2C address 0x69 (0x68 after modifying the solder pad).
    /// </summary>
    public class ICM20600 : IDisposable
    {
        public const byte DefaultAddress = 0x69;
        public const byte AlternateAddress = 0x68;

        private const byte RegSampleRateDivider = 0x19;
        private const byte RegConfig = 0x1A;
        private const byte RegGyroConfig = 0x1B;
        private const byte RegAccelConfig = 0x1C;
        private const byte RegAccelConfig2 = 0x1D;
        private const byte RegAccelXOutH = 0x3B; // 14 bytes: accel XYZ, temp, gyro XYZ (big-endian)
        private const byte RegPowerManagement1 = 0x6B;
        private const byte RegPowerManagement2 = 0x6C;
        private const byte RegWhoAmI = 0x75;
        private const byte WhoAmIValue = 0x11;

        private readonly I2cDevice _device;
        private float _accelLsbPerG;
        private float _gyroLsbPerDps;

        public AccelRange AccelRange { get; private set; }
        public GyroRange GyroRange { get; private set; }

        /// <summary>Gyroscope offset (°/s) subtracted from every reading. Set by CalibrateGyro().</summary>
        public Vector3 GyroBias { get; set; } = Vector3.Zero;

        public ICM20600(byte address = DefaultAddress,
                        AccelRange accelRange = AccelRange.G4,
                        GyroRange gyroRange = GyroRange.Dps500,
                        int busId = 1)
        {
            _device = I2cDevice.Create(new I2cConnectionSettings(busId, address));

            byte id = ReadRegister(RegWhoAmI);
            if (id != WhoAmIValue)
            {
                Console.WriteLine($"ICM20600: unexpected WHO_AM_I 0x{id:X2} (expected 0x{WhoAmIValue:X2}) at address 0x{address:X2}");
            }

            WriteRegister(RegPowerManagement1, 0x80);  // device reset
            Thread.Sleep(100);
            WriteRegister(RegPowerManagement1, 0x01);  // wake up, auto-select best clock source
            Thread.Sleep(10);
            WriteRegister(RegPowerManagement2, 0x00);  // enable all accel and gyro axes
            WriteRegister(RegConfig, 0x03);            // gyro low-pass filter ~41 Hz
            WriteRegister(RegSampleRateDivider, 0x09); // 1 kHz / (1 + 9) = 100 Hz output data rate
            WriteRegister(RegAccelConfig2, 0x03);      // accel low-pass filter ~45 Hz

            SetAccelRange(accelRange);
            SetGyroRange(gyroRange);
        }

        public void SetAccelRange(AccelRange range)
        {
            byte value = (byte)(ReadRegister(RegAccelConfig) & ~0x18); // clear ACCEL_FS_SEL bits [4:3]
            WriteRegister(RegAccelConfig, (byte)(value | ((byte)range << 3)));
            _accelLsbPerG = 16384f / (1 << (int)range);
            AccelRange = range;
        }

        public void SetGyroRange(GyroRange range)
        {
            byte value = (byte)(ReadRegister(RegGyroConfig) & ~0x1B); // clear FS_SEL [4:3] and FCHOICE_B [1:0]
            WriteRegister(RegGyroConfig, (byte)(value | ((byte)range << 3)));
            _gyroLsbPerDps = 131f / (1 << (int)range);
            GyroRange = range;
        }

        /// <summary>
        /// Reads all values in one I2C transaction, so they belong to the same sample.
        /// acceleration in g, angularVelocity in °/s (bias-corrected), temperature in °C.
        /// </summary>
        public void Read(out Vector3 acceleration, out Vector3 angularVelocity, out float temperature)
        {
            Span<byte> buffer = stackalloc byte[14];
            ReadRegisters(RegAccelXOutH, buffer);

            acceleration = new Vector3(ToInt16(buffer, 0), ToInt16(buffer, 2), ToInt16(buffer, 4)) / _accelLsbPerG;
            temperature = ToInt16(buffer, 6) / 326.8f + 25f;
            angularVelocity = new Vector3(ToInt16(buffer, 8), ToInt16(buffer, 10), ToInt16(buffer, 12)) / _gyroLsbPerDps - GyroBias;
        }

        /// <summary>Acceleration in g (1 g ≈ 9.81 m/s²). Lying flat and still: roughly (0, 0, 1).</summary>
        public Vector3 GetAcceleration()
        {
            Read(out Vector3 acceleration, out _, out _);
            return acceleration;
        }

        /// <summary>Angular velocity in degrees per second, bias-corrected.</summary>
        public Vector3 GetAngularVelocity()
        {
            Read(out _, out Vector3 angularVelocity, out _);
            return angularVelocity;
        }

        /// <summary>Die temperature in °C (runs a few degrees above ambient).</summary>
        public float GetTemperature()
        {
            Read(out _, out _, out float temperature);
            return temperature;
        }

        /// <summary>
        /// Measures the gyroscope offset. The robot must be completely still while this runs
        /// (about samples × 10 ms). Returns and stores the bias in GyroBias.
        /// </summary>
        public Vector3 CalibrateGyro(int samples = 200)
        {
            GyroBias = Vector3.Zero;
            Vector3 sum = Vector3.Zero;
            for (int i = 0; i < samples; i++)
            {
                sum += GetAngularVelocity();
                Thread.Sleep(10);
            }
            GyroBias = sum / samples;
            return GyroBias;
        }

        private static short ToInt16(ReadOnlySpan<byte> buffer, int index)
        {
            return (short)((buffer[index] << 8) | buffer[index + 1]);
        }

        private byte ReadRegister(byte register)
        {
            Span<byte> buffer = stackalloc byte[1];
            ReadRegisters(register, buffer);
            return buffer[0];
        }

        private void ReadRegisters(byte register, Span<byte> buffer)
        {
            ReadOnlySpan<byte> write = stackalloc byte[] { register };
            _device.WriteRead(write, buffer);
        }

        private void WriteRegister(byte register, byte value)
        {
            ReadOnlySpan<byte> write = stackalloc byte[] { register, value };
            _device.Write(write);
        }

        public void Dispose()
        {
            _device.Dispose();
        }
    }
}
