using System;
using System.Diagnostics;
using System.Numerics;

namespace Avans.StatisticalRobot
{
    /// <summary>
    /// Grove - IMU 9DOF (ICM-20600 + AK09918). Connect to a Grove I2C port.
    /// Combines accelerometer, gyroscope and compass, and adds:
    ///  - roll / pitch from the accelerometer
    ///  - tilt-compensated compass heading
    ///  - Yaw: rotation angle integrated from the gyroscope (call Update() regularly)
    /// </summary>
    public class GroveIMU9DOF : IUpdatable, IDisposable
    {
        public ICM20600 AccelGyro { get; }
        public AK09918 Magnetometer { get; }

        /// <summary>Magnetic declination at your location in degrees (east positive). Added to the compass heading.</summary>
        public double Declination { get; set; } = 0;

        /// <summary>
        /// Rotation around the Z axis in degrees since the last ResetYaw(), integrated from the gyroscope.
        /// Not wrapped: two full turns counterclockwise gives 720. Only valid if Update() is called often (every ~10 ms).
        /// </summary>
        public double Yaw { get; private set; }

        private readonly Stopwatch _stopwatch = new Stopwatch();
        private long _lastTicks;

        public GroveIMU9DOF(byte icm20600Address = ICM20600.DefaultAddress,
                            AccelRange accelRange = AccelRange.G4,
                            GyroRange gyroRange = GyroRange.Dps500)
        {
            AccelGyro = new ICM20600(icm20600Address, accelRange, gyroRange);
            Magnetometer = new AK09918(MagnetometerMode.Continuous100Hz);
        }

        /// <summary>Acceleration in g.</summary>
        public Vector3 GetAcceleration() => AccelGyro.GetAcceleration();

        /// <summary>Angular velocity in °/s.</summary>
        public Vector3 GetAngularVelocity() => AccelGyro.GetAngularVelocity();

        /// <summary>Magnetic field in µT (hard-iron corrected).</summary>
        public Vector3 GetMagneticField() => Magnetometer.GetMagneticField();

        /// <summary>Sensor temperature in °C.</summary>
        public float GetTemperature() => AccelGyro.GetTemperature();

        /// <summary>Roll and pitch in degrees, from the direction of gravity. Only accurate when not accelerating.</summary>
        public (double Roll, double Pitch) GetRollPitch()
        {
            Vector3 a = GetAcceleration();
            double roll = Math.Atan2(a.Y, a.Z);
            double pitch = Math.Atan2(-a.X, Math.Sqrt(a.Y * a.Y + a.Z * a.Z));
            return (ToDegrees(roll), ToDegrees(pitch));
        }

        /// <summary>
        /// Tilt-compensated compass heading in degrees (0–360), including Declination.
        /// Run CalibrateMagnetometer() first; motors, batteries and steel nearby distort the result.
        /// </summary>
        public double GetCompassHeading()
        {
            Vector3 a = GetAcceleration();
            Vector3 m = GetMagneticField();

            double roll = Math.Atan2(a.Y, a.Z);
            double pitch = Math.Atan2(-a.X, Math.Sqrt(a.Y * a.Y + a.Z * a.Z));

            double xh = m.X * Math.Cos(pitch)
                      + m.Y * Math.Sin(roll) * Math.Sin(pitch)
                      + m.Z * Math.Cos(roll) * Math.Sin(pitch);
            double yh = m.Y * Math.Cos(roll) - m.Z * Math.Sin(roll);

            double heading = ToDegrees(Math.Atan2(-yh, xh)) + Declination;
            heading %= 360.0;
            if (heading < 0) heading += 360.0;
            return heading;
        }

        /// <summary>Integrates the gyroscope Z axis into Yaw. Call this frequently from your main loop.</summary>
        public void Update()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
                _lastTicks = _stopwatch.ElapsedTicks;
                return;
            }

            long now = _stopwatch.ElapsedTicks;
            double dt = (now - _lastTicks) / (double)Stopwatch.Frequency;
            _lastTicks = now;

            Yaw += AccelGyro.GetAngularVelocity().Z * dt;
        }

        public void ResetYaw()
        {
            Yaw = 0;
        }

        /// <summary>Keep the robot completely still while this runs (~2 s).</summary>
        public Vector3 CalibrateGyro(int samples = 200) => AccelGyro.CalibrateGyro(samples);

        /// <summary>Rotate the robot (e.g. spin in place) while this runs.</summary>
        public Vector3 CalibrateMagnetometer(int durationMs = 10000) => Magnetometer.CalibrateHardIron(durationMs);

        private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;

        public void Dispose()
        {
            AccelGyro.Dispose();
            Magnetometer.Dispose();
        }
    }
}
