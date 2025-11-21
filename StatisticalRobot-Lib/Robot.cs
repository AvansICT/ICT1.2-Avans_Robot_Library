using System.Device;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Pwm;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avans.StatisticalRobot;

public static class Robot
{

    private static I2cBus i2cBus = I2cBus.Create(1);
    private static I2cDevice romi32u4 = i2cBus.CreateDevice(0x14);
    //This is the address 0x04 for the grovePi
    private static I2cDevice grovePiAnalog = i2cBus.CreateDevice(0x04);

    private static Mutex mut = new Mutex();


    private static GpioController gpioController = new();
    private static PwmChannel? pwm;
    private static long stopwatchTicksPerUs = (long)(Stopwatch.Frequency * 0.000_001);
    static Robot()
    {
        byte grovePiAddress = 0x04;

        try
        {
            grovePiAnalog.WriteByte(0x01);

        }
        catch (Exception ex)
        {
            i2cBus.RemoveDevice(grovePiAddress);
            grovePiAnalog = null;
            // Console.WriteLine("GroveHat with address: " + grovePiAddress + " not found. ");
            grovePiAddress = 0x08;
            // Console.WriteLine("Checking I2C address for GroveHat: " + grovePiAddress);

        }

        if (grovePiAnalog == null)
        {
            grovePiAnalog = i2cBus.CreateDevice(grovePiAddress);
            try
            {
                grovePiAnalog.WriteByte(0x01);

            }
            catch (Exception ex)
            {

                grovePiAnalog = null;
                grovePiAddress = 0x00;
            }
        }

        switch (grovePiAddress)
        {
            case 0x04:
                // Nothing mentioned to the end user :)
                // Console.WriteLine("GroveHat version 1.1. installed.");
                break;
            case 0x08:
                Console.WriteLine("GroveHat version 1.0. installed.");
                break;
            default:
                Console.WriteLine("Is the GroveHat installed? Continuing without.");
                break;
        }
    }
    //private static bool TestDevice(int d)
    //{

    //}

    /// <summary>
    /// Reads data from Romi based on struct format
    /// </summary>
    /// <param name="address">Register adress</param>
    /// <param name="size">Size of data</param>
    /// <param name="format">struct format based on Python</param>
    /// <returns>A formatted list of objects</returns>
    private static object[] ReadUnpack(int address, int size, string format)
    {
        mut.WaitOne();
        romi32u4.WriteByte((byte)address);
        mut.ReleaseMutex();
        // Lees de gegevens in de buffer
        byte[] readBuffer = new byte[size];

        mut.WaitOne();
        romi32u4.Read(readBuffer);
        mut.ReleaseMutex();

        return StructConverter.Unpack(format, readBuffer);
    }

    /// <summary>
    /// Writes data to Romi with the correct format based on the type of data
    /// </summary>
    /// <param name="address">Register adress</param>
    /// <param name="data">Data list</param>
    private static void WritePack(int address, params object[] data)
    {
        byte[] writeBuffer = StructConverter.Pack(data)
            .Prepend((byte)address)
            .ToArray();
        mut.WaitOne();
        romi32u4.Write(writeBuffer);
        mut.ReleaseMutex();
    }

    public static ComponentInformation GetQueryComponentInformation()
    {

        return i2cBus.QueryComponentInformation();
    }

    /// <summary>
    /// Writes bytes to specific address
    /// </summary>
    /// <param name="address">Register address</param>
    /// <param name="data">Byte list</param>
    private static void WriteRaw(int address, byte[] data)
    {
        byte[] writeBuffer = data.Prepend((byte)address)
            .ToArray();

        mut.WaitOne();
        romi32u4.Write(writeBuffer);
        mut.ReleaseMutex();
    }

    /// <summary>
    /// Writes the brightness for each individual led on the Romi
    /// </summary>
    /// <param name="red">Value between 0-255</param>
    /// <param name="yellow">Value between 0-255</param>
    /// <param name="green">Value between 0-255</param>
    public static void LEDs(byte red, byte yellow, byte green)
    {
        WriteRaw(0, [yellow, green, red]);
    }

    /// <summary>
    /// Plays notes on the buzzer located on the romi
    /// </summary>
    /// <param name="notes">Parses the input string to determine note duration, octave, and whether the note is dotted. It handles octave (o), length (l), and octave shift (> and <) commands.</param>
    public static void PlayNotes(string notes)
    {
        byte[] data = new byte[16];
        data[0] = 1;
        byte[] noteBytes = System.Text.Encoding.ASCII.GetBytes(notes);
        Buffer.BlockCopy(noteBytes, 0, data, 1, Math.Min(noteBytes.Length, 14));
        WriteRaw(24, data);
    }

    /// <summary>
    /// Sets speed per motor <= 400
    /// </summary>
    /// <param name="speedLeft">Speed left motor</param>
    /// <param name="speedRight">Speed right motor</param>
    public static void Motors(short speedLeft, short speedRight)
    {
        WritePack(6, speedLeft, speedRight);
    }

    /// <summary>
    /// Reads buttons on the Romi
    /// </summary>
    /// <returns>List with bools if button is pressed or not</returns>
    public static bool[] ReadButtons()
    {
        return ReadUnpack(3, 3, "???")
            .OfType<bool>()
            .ToArray();
    }

    /// <summary>
    /// Reads current millivolts of the batterys
    /// </summary>
    /// <returns>The value in millivolts</returns>
    public static ushort ReadBatteryMillivolts()
    {
        return ReadUnpack(10, 2, "H")
            .OfType<ushort>()
            .FirstOrDefault((ushort)0);
    }

    // based on the design of the romi on 1-7-2024 this is not needed
    // read analog port Romi
    //
    // public static ushort[] ReadAnalog()
    // {
    //     return ReadUnpack(12,12,"HHHHHH")
    //         .OfType<ushort>()
    //         .ToArray();
    // }

    /// <summary>
    /// Reads the encoders on the weels
    /// </summary>
    /// <returns>The position of the weels</returns>
    public static short[] ReadEncoders()
    {
        return ReadUnpack(39, 4, "hh")
            .OfType<short>()
            .ToArray();
    }

    public static I2cDevice CreateI2cDevice(byte address)
    {
        return i2cBus.CreateDevice(address);
    }

    public static void SetDigitalPinMode(int pinNumber, PinMode state)
    {
        gpioController.OpenPin(pinNumber, state);
    }

    public static void WriteDigitalPin(int pinNumber, PinValue value)
    {
        gpioController.Write(pinNumber, value);
    }

    public static PinValue ReadDigitalPin(int pinNumber)
    {
        return gpioController.Read(pinNumber);
    }

    /// <summary>
    /// Setup for the pwm
    /// </summary>
    /// <param name="frequency"></param>
    /// <param name="dutyCyclePercentage"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SetPwmPin(int frequency, double dutyCyclePercentage)
    {
        if (dutyCyclePercentage < 0 || dutyCyclePercentage > 1)
        {
            throw new ArgumentOutOfRangeException("Duty Cycle needs to be between 0.0 and 1.0");
        }
        pwm = PwmChannel.Create(0, 0, frequency, dutyCyclePercentage);
    }

    public static void ChangePwmFrequency(int frequency)
    {
        pwm.Frequency = frequency;
    }

    public static void ChangePwmDutyCycle(double dutyCycle)
    {
        pwm.DutyCycle = dutyCycle;
    }

    public static void StartPwm() => pwm.Start();
    public static void StopPwm() => pwm.Stop();

    public static int AnalogRead(byte analogPin)
    {
        try
        {
            grovePiAnalog.WriteRegister((byte)(0x30 + analogPin));
            byte[] readBuffer = new byte[2];
            grovePiAnalog.ReadRegister((byte)(0x30 + analogPin), readBuffer);
            int value = readBuffer[1] << 8 | readBuffer[0];
            return value;
        }
        catch (Exception ex)
        {
            // Handle any exceptions or errors
            Console.WriteLine("Error: " + ex.Message);
            return 0;
        }
    }

    /// <summary>
    /// Reads input pulses
    /// </summary>
    /// <param name="pin">The pin to read from</param>
    /// <param name="waitFor">Pin value to wait for</param>
    /// <param name="timeoutms">Timeout in milliseconds</param>
    /// <returns>-2 if pinstate invalid, -1 if timeout, 0 if timeout during pulse read</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int PulseIn(int pin, PinValue waitFor, int timeoutms)
    {
        if (gpioController.Read(pin) == waitFor)
        {
            return -2;
        }

        Stopwatch timeoutTimer = Stopwatch.StartNew();

        PinValue notWaitFor = !waitFor;
        while (gpioController.Read(pin) == notWaitFor && timeoutTimer.ElapsedMilliseconds < timeoutms)
        {
            // Wait for pulse
        }

        if (timeoutTimer.ElapsedMilliseconds >= timeoutms)
        {
            return -1;
        }

        Stopwatch pulseTimer = Stopwatch.StartNew();
        while (gpioController.Read(pin) == PinValue.High && timeoutTimer.ElapsedMilliseconds < timeoutms)
        {
            // Wait
        }
        pulseTimer.Stop();

        if (timeoutTimer.ElapsedMilliseconds >= timeoutms)
        {
            return 0;
        }

        return (int)Math.Round(pulseTimer.Elapsed.TotalMicroseconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void WaitUs(int usec)
    {
        long durationTicks = Robot.stopwatchTicksPerUs * usec;

        long startTicks = Stopwatch.GetTimestamp();
        long nowTicks;
        do
        {
            nowTicks = Stopwatch.GetTimestamp();
        } while (nowTicks - startTicks < durationTicks);
    }

    public static void Wait(int ms)
    {
        Thread.Sleep(ms);
    }
}