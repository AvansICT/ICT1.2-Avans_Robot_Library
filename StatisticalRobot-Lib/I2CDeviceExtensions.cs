
using System.Device.I2c;

/// <summary>
/// Makes it possible to specify what data needs to be read or written to what register
/// </summary>
public static class I2cDeviceExtensions
{
    /// <summary>Writes a register address to select it for a subsequent operation.</summary>
    /// <param name="device">The target I2C device.</param>
    /// <param name="register">The register address to select.</param>
    public static void WriteRegister(this I2cDevice device, byte register) {
        device.Write([ register ]);
        Thread.Sleep(1);
    }

    /// <summary>Writes a register address followed by a sequence of bytes.</summary>
    /// <param name="device">The target I2C device.</param>
    /// <param name="register">The register address to write to.</param>
    /// <param name="data">The bytes to write after the register address.</param>
    public static void WriteRegister(this I2cDevice device, byte register, ReadOnlySpan<byte> data) 
    {
        byte[] writeBuffer = new byte[data.Length + 1];

        writeBuffer[0] = register;
        data.CopyTo(writeBuffer.AsSpan().Slice(1));

        device.Write(writeBuffer);
        Thread.Sleep(1);
    }
    /// <summary>Writes a single byte to a register.</summary>
    /// <param name="device">The target I2C device.</param>
    /// <param name="register">The register address to write to.</param>
    /// <param name="data">The byte to write.</param>
    public static void WriteByteRegister(this I2cDevice device, byte register, byte data) 
    {
        byte[] writeBuffer = [register, data];  
        device.Write(writeBuffer);
        Thread.Sleep(1);
    }
    /// <summary>Selects a register and reads bytes into the supplied buffer.</summary>
    /// <param name="device">The target I2C device.</param>
    /// <param name="register">The register address to read from.</param>
    /// <param name="readBuffer">The buffer that receives the register data.</param>
    public static void ReadRegister(this I2cDevice device, byte register, Span<byte> readBuffer) 
    {
        // Trigger the register for reading
        device.WriteRegister(register);

        // Actually read the register
        device.Read(readBuffer);
    }
    /// <summary>Reads one byte from a register.</summary>
    /// <param name="device">The target I2C device.</param>
    /// <param name="register">The register address to read from.</param>
    /// <returns>The byte read from the register.</returns>
    public static byte ReadByteRegister(this I2cDevice device, byte register) 
    {
        // Trigger the register for reading
        device.WriteRegister(register);

        // Actually read the register
        return device.ReadByte();
    }

}
