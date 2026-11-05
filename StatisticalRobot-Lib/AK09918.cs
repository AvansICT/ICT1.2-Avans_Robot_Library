using System;
using System.Device.I2c;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace Avans.StatisticalRobot
{
    /// <summary>Operating modes of the AK09918 (values of the CNTL2 register).</summary>
    public enum MagnetometerMode : byte
    {
        PowerDown = 0x00,
        SingleMeasurement = 0x01,
        Continuous10Hz = 0x02,
        Continuous20Hz = 0x04,
        Continuous50Hz = 0x06,
        Continuous100Hz = 0x08
    }

    /// <summary>
    /// Driver for the AKM AK09918 3-axis magnetometer (electronic compass)
    /// on the Grove - IMU 9DOF module. Fixed I2C address 0x0C.
    /// </summary>
    public class AK09918 : IDisposable
    {
        public const byte DefaultAddress = 0x0C;

        private const byte RegWia2 = 0x01;  // device ID
        private const byte RegSt1 = 0x10;   // bit0 DRDY (data ready)
        private const byte RegHxl = 0x11;   // HXL..HZH (little-endian), TMPS, ST2 = 8 bytes
        private const byte RegCntl2 = 0x31; // mode
        private const byte RegCntl3 = 0x32; // soft reset
        private const byte DeviceId = 0x0C;
        private const byte St2Overflow = 0x08;
        private const float MicroTeslaPerLsb = 0.15f;

        private readonly I2cDevice _device;
        private Vector3 _lastRaw = Vector3.Zero;

        public MagnetometerMode Mode { get; private set; }

        /// <summary>Hard-iron offset (µT) subtracted from every reading. Set by CalibrateHardIron().</summary>
        public Vector3 HardIronOffset { get; set; } = Vector3.Zero;

        /// <summary>True when the last measurement overflowed (field too strong, e.g. a magnet or motor nearby).</summary>
        public bool LastReadOverflowed { get; private set; }

        public AK09918(MagnetometerMode mode = MagnetometerMode.Continuous100Hz, int busId = 1)
        {
            _device = I2cDevice.Create(new I2cConnectionSettings(busId, DefaultAddress));

            byte id = ReadRegister(RegWia2);
            if (id != DeviceId)
            {
                Console.WriteLine($"AK09918: unexpected device ID 0x{id:X2} (expected 0x{DeviceId:X2})");
            }

            WriteRegister(RegCntl3, 0x01); // soft reset
            Thread.Sleep(10);
            SetMode(mode);
        }

        /// <summary>Changes the mode. The datasheet requires passing through power-down first.</summary>
        public void SetMode(MagnetometerMode mode)
        {
            WriteRegister(RegCntl2, (byte)MagnetometerMode.PowerDown);
            Thread.Sleep(1);
            if (mode != MagnetometerMode.PowerDown)
            {
                WriteRegister(RegCntl2, (byte)mode);
                Thread.Sleep(1);
            }
            Mode = mode;
        }

        public bool IsDataReady()
        {
            return (ReadRegister(RegSt1) & 0x01) != 0;
        }

        /// <summary>
        /// Magnetic field in µT with the hard-iron offset removed.
        /// In continuous mode this returns the newest sample (or the previous one if nothing new has arrived yet).
        /// </summary>
        public Vector3 GetMagneticField()
        {
            return GetRawMagneticField() - HardIronOffset;
        }

        /// <summary>Magnetic field in µT without offset correction.</summary>
        public Vector3 GetRawMagneticField()
        {
            if (Mode == MagnetometerMode.PowerDown)
            {
                throw new InvalidOperationException("AK09918 is powered down; call SetMode() first.");
            }

            if (Mode == MagnetometerMode.SingleMeasurement)
            {
                // After a single measurement the chip returns to power-down, so trigger a new one.
                WriteRegister(RegCntl2, (byte)MagnetometerMode.SingleMeasurement);
                Stopwatch timer = Stopwatch.StartNew();
                while (!IsDataReady())
                {
                    if (timer.ElapsedMilliseconds > 20)
                    {
                        return _lastRaw;
                    }
                    Thread.Sleep(1);
                }
            }
            else if (!IsDataReady())
            {
                return _lastRaw;
            }

            // Read X, Y, Z, TMPS and ST2 in one go. Reading ST2 tells the chip the read is finished.
            Span<byte> buffer = stackalloc byte[8];
            ReadRegisters(RegHxl, buffer);

            LastReadOverflowed = (buffer[7] & St2Overflow) != 0;
            if (!LastReadOverflowed)
            {
                _lastRaw = new Vector3(
                    (short)(buffer[0] | (buffer[1] << 8)),
                    (short)(buffer[2] | (buffer[3] << 8)),
                    (short)(buffer[4] | (buffer[5] << 8))) * MicroTeslaPerLsb;
            }
            return _lastRaw;
        }

        /// <summary>
        /// Hard-iron calibration: during durationMs, rotate the sensor through as many orientations as possible
        /// (or at least spin the robot in place a few times). The centre of the measured min/max range
        /// becomes HardIronOffset. Requires a continuous mode.
        /// </summary>
        public Vector3 CalibrateHardIron(int durationMs = 10000)
        {
            Vector3 first = GetRawMagneticField();
            Vector3 min = first;
            Vector3 max = first;

            Stopwatch timer = Stopwatch.StartNew();
            while (timer.ElapsedMilliseconds < durationMs)
            {
                Vector3 sample = GetRawMagneticField();
                if (!LastReadOverflowed)
                {
                    min = Vector3.Min(min, sample);
                    max = Vector3.Max(max, sample);
                }
                Thread.Sleep(15);
            }

            HardIronOffset = (min + max) / 2f;
            return HardIronOffset;
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
