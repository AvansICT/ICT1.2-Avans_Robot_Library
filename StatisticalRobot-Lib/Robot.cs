using System.Device;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Pwm;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Avans.StatisticalRobot;

/// <summary>
/// Provides shared access to the Romi motors, sensors, GPIO, and timing functions.
/// </summary>
/// <remarks>Requires the Raspberry Pi hardware and access to I2C and GPIO.</remarks>
public static class Robot {

    private static I2cBus i2cBus = I2cBus.Create(1);
    private static I2cDevice romi32u4 = i2cBus.CreateDevice(0x14);
    //This is the address 0x04 for the grovePi
    private static I2cDevice? grovePiAnalog = i2cBus.CreateDevice(0x04);

    private static GpioController gpioController = new();
    private static PwmChannel? pwm;
    private static long stopwatchTicksPerUs = (long)(Stopwatch.Frequency * 0.000_001);
  static Robot()
    {
        byte grovePiAddress = 0x04;
        I2cDevice analogDevice = grovePiAnalog
            ?? throw new InvalidOperationException("The Grove analog device was not created.");

        try
        {
            analogDevice.WriteByte(0x01);

        }
        catch (Exception)
        {
            i2cBus.RemoveDevice(grovePiAddress);
            grovePiAnalog = null;
            // Console.WriteLine("GroveHat with address: " + grovePiAddress + " not found. ");
            grovePiAddress = 0x08;
            // Console.WriteLine("Checking I2C address for GroveHat: " + grovePiAddress);

        }

        if (grovePiAnalog == null)
        {
            analogDevice = i2cBus.CreateDevice(grovePiAddress);
            grovePiAnalog = analogDevice;
            try
            {
                analogDevice.WriteByte(0x01);

            }
            catch (Exception)
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

    /// <summary>Reads bytes from a Romi register and decodes them using the supplied struct format.</summary>
    /// <param name="address">The Romi register address.</param>
    /// <param name="size">The number of bytes to read.</param>
    /// <param name="format">The format string interpreted by StructConverter.</param>
    /// <returns>The decoded register values.</returns>
    private static object[] ReadUnpack(int address, int size, string format)
    {
        
        romi32u4.WriteByte((byte)address);

        // Lees de gegevens in de buffer
        byte[] readBuffer = new byte[size];
        romi32u4.Read(readBuffer);

        return StructConverter.Unpack(format, readBuffer);
    }

    /// <summary>Encodes values and writes them to the specified Romi register.</summary>
    /// <param name="address">The Romi register address.</param>
    /// <param name="data">The values to encode and write.</param>
    private static void WritePack(int address, params object[] data)
    {
        byte[] writeBuffer = StructConverter.Pack(data)
            .Prepend((byte)address)
            .ToArray();
        romi32u4.Write(writeBuffer);
    }

    /// <summary>Queries information about the Raspberry Pi I2C bus and its devices.</summary>
    /// <returns>The component information reported by the I2C bus.</returns>
    public static ComponentInformation GetQueryComponentInformation()
    {

        return i2cBus.QueryComponentInformation();
    }

    /// <summary>Writes raw bytes to the specified Romi register.</summary>
    /// <param name="address">The Romi register address.</param>
    /// <param name="data">The bytes to write after the register address.</param>
    private static void WriteRaw(int address, byte[] data) 
    {
        byte[] writeBuffer = data.Prepend((byte)address)
            .ToArray();

        romi32u4.Write(writeBuffer);

    }

    /// <summary>Writes the red, yellow, and green LED values to the Romi.</summary>
    /// <param name="red">The red LED value, from 0 to 255.</param>
    /// <param name="yellow">The yellow LED value, from 0 to 255.</param>
    /// <param name="green">The green LED value, from 0 to 255.</param>
    public static void LEDs(byte red, byte yellow, byte green)
    {
        WriteRaw(0, [yellow, green, red]);
    }

    /// <summary>Sends up to 14 ASCII characters of a note sequence to the Romi buzzer.</summary>
    /// <param name="notes">The note sequence interpreted by the Romi firmware. Only the first 14 ASCII characters are sent.</param>
    public static void PlayNotes(string notes)
    {
        byte[] data = new byte[16];
        data[0] = 1;
        byte[] noteBytes = System.Text.Encoding.ASCII.GetBytes(notes);
        Buffer.BlockCopy(noteBytes, 0, data, 1, Math.Min(noteBytes.Length, 14));
        WriteRaw(24, data);
    }

    /// <summary>Sends signed speed commands to the left and right motors without validating their range.</summary>
    /// <param name="speedLeft">The signed speed command for the left motor.</param>
    /// <param name="speedRight">The signed speed command for the right motor.</param>
    public static void Motors(short speedLeft, short speedRight)
    {
        WritePack(6,speedLeft,speedRight);
    }

    /// <summary>Reads the three button states reported by the Romi.</summary>
    /// <returns>The three decoded button flags, in register order.</returns>
    public static bool[] ReadButtons()
    {
        return ReadUnpack(3,3,"???")
            .OfType<bool>()
            .ToArray();
    }

    /// <summary>Reads the battery voltage reported by the Romi.</summary>
    /// <returns>The battery voltage in millivolts.</returns>
    public static ushort ReadBatteryMillivolts()
    {
        return ReadUnpack(10,2,"H")
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

    /// <summary>Reads the two signed wheel encoder counts reported by the Romi.</summary>
    /// <returns>The two signed encoder counts, in register order.</returns>
    public static short[] ReadEncoders()
    {
        return ReadUnpack(39,4,"hh")
            .OfType<short>()
            .ToArray();
    }

    /// <summary>Creates a device on the shared I2C bus.</summary>
    /// <param name="address">The device's I2C address.</param>
    /// <returns>The I2C device for the specified address.</returns>
    public static I2cDevice CreateI2cDevice(byte address) 
    {
        return i2cBus.CreateDevice(address);
    } 

    /// <summary>Opens a digital pin in the specified mode.</summary>
    /// <param name="pinNumber">The GPIO pin number used by the Grove port.</param>
    /// <param name="state">The input or output mode for the pin.</param>
    public static void SetDigitalPinMode(int pinNumber,PinMode state)
    {
        gpioController.OpenPin(pinNumber,state);
    }

    /// <summary>Writes a value to an open digital output pin.</summary>
    /// <param name="pinNumber">The GPIO pin number used by the Grove port.</param>
    /// <param name="value">The high or low value to write.</param>
    public static void WriteDigitalPin(int pinNumber, PinValue value)
    {
        gpioController.Write(pinNumber,value);
    }

    /// <summary>Reads an open digital input pin.</summary>
    /// <param name="pinNumber">The GPIO pin number used by the Grove port.</param>
    /// <returns>The pin's high or low value.</returns>
    public static PinValue ReadDigitalPin(int pinNumber)
    {
        return gpioController.Read(pinNumber);
    }

    /// <summary>
    /// Creates the PWM channel on chip 0, channel 0.
    /// </summary>
    /// <param name="frequency">The PWM frequency in hertz.</param>
    /// <param name="dutyCyclePercentage">The duty cycle as a fraction between 0.0 and 1.0.</param>
    /// <exception cref="ArgumentOutOfRangeException">The duty cycle is outside the allowed range.</exception>
    public static void SetPwmPin(int frequency, double dutyCyclePercentage )
    {
        if(dutyCyclePercentage < 0 || dutyCyclePercentage > 1) 
        {
            throw new ArgumentOutOfRangeException("Duty Cycle needs to be between 0.0 and 1.0");
        }
        pwm = PwmChannel.Create(0,0,frequency,dutyCyclePercentage);
    }

    private static PwmChannel GetPwmChannel() =>
        pwm ?? throw new InvalidOperationException("Call SetPwmPin before using PWM.");

    /// <summary>Changes the configured PWM channel's frequency.</summary>
    /// <param name="frequency">The PWM frequency in hertz.</param>
    /// <exception cref="InvalidOperationException">The PWM channel has not been configured with <see cref="SetPwmPin"/>.</exception>
    public static void ChangePwmFrequency(int frequency)
    {
        GetPwmChannel().Frequency = frequency;
    }

    /// <summary>Changes the configured PWM channel's duty cycle.</summary>
    /// <param name="dutyCycle">The duty cycle as a fraction between 0.0 and 1.0.</param>
    /// <exception cref="InvalidOperationException">The PWM channel has not been configured with <see cref="SetPwmPin"/>.</exception>
    public static void ChangePwmDutyCycle(double dutyCycle)
    {
        GetPwmChannel().DutyCycle = dutyCycle;
    }

    /// <summary>Starts output from the configured PWM channel.</summary>
    /// <exception cref="InvalidOperationException">The PWM channel has not been configured with <see cref="SetPwmPin"/>.</exception>
    public static void StartPwm() => GetPwmChannel().Start();

    /// <summary>Stops output from the configured PWM channel.</summary>
    /// <exception cref="InvalidOperationException">The PWM channel has not been configured with <see cref="SetPwmPin"/>.</exception>
    public static void StopPwm() => GetPwmChannel().Stop();

    /// <summary>Reads an analog channel on the Grove board.</summary>
    /// <param name="analogPin">The analog channel index, such as 0 for A0.</param>
    /// <returns>The raw analog reading, or 0 if the board is unavailable or the read fails.</returns>
    /// <remarks>Read failures are also written to the console.</remarks>
    public static int AnalogRead(byte analogPin)
    {
        try
        {
            I2cDevice analogDevice = grovePiAnalog
                ?? throw new InvalidOperationException("The Grove analog board is unavailable.");
            analogDevice.WriteRegister((byte)(0x30 + analogPin));
            byte[] readBuffer = new byte[2];
            analogDevice.ReadRegister((byte)(0x30+analogPin),readBuffer);
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

    /// <summary>Waits for the requested pin state, then measures how long the pin remains HIGH within the overall timeout.</summary>
    /// <param name="pin">The digital input pin to read.</param>
    /// <param name="waitFor">The pin state to wait for before timing; the timed loop measures HIGH.</param>
    /// <param name="timeoutms">The overall timeout for waiting and timing, in milliseconds.</param>
    /// <returns>The rounded HIGH duration in microseconds; -2 if the pin already has the requested state, -1 if waiting times out, or 0 if timing the pulse times out.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int PulseIn(int pin, PinValue waitFor, int timeoutms)
    {
        if(gpioController.Read(pin) == waitFor) 
        {
            return -2;
        }

        Stopwatch timeoutTimer = Stopwatch.StartNew();

        PinValue notWaitFor = !waitFor;
        while(gpioController.Read(pin) == notWaitFor && timeoutTimer.ElapsedMilliseconds < timeoutms)
        {
            // Wait for pulse
        }

        if(timeoutTimer.ElapsedMilliseconds >= timeoutms) {
            return -1;
        }

        Stopwatch pulseTimer = Stopwatch.StartNew();
        while(gpioController.Read(pin) == PinValue.High && timeoutTimer.ElapsedMilliseconds < timeoutms) 
        {
            // Wait
        }
        pulseTimer.Stop();

        if(timeoutTimer.ElapsedMilliseconds >= timeoutms) {
            return 0;
        }

        return (int)Math.Round(pulseTimer.Elapsed.TotalMicroseconds);
    }

    /// <summary>Busy-waits for approximately the specified number of microseconds.</summary>
    /// <param name="usec">The delay in microseconds.</param>
    /// <remarks>Timing depends on the operating system and processor scheduling.</remarks>
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

    /// <summary>Suspends the current thread for the specified delay.</summary>
    /// <param name="ms">The delay in milliseconds, or -1 to wait indefinitely.</param>
    public static void Wait(int ms)
    {
        Thread.Sleep(ms);
    }
}
