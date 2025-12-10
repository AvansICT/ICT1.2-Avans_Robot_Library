using System.Device.I2c;

namespace Avans.StatisticalRobot;

public enum AK09918_mode_type {
    AK09918_POWER_DOWN = 0x00,
    AK09918_NORMAL = 0x01,
    AK09918_CONTINUOUS_10HZ = 0x02,
    AK09918_CONTINUOUS_20HZ = 0x04,
    AK09918_CONTINUOUS_50HZ = 0x06,
    AK09918_CONTINUOUS_100HZ = 0x08,
    AK09918_SELF_TEST = 0x10, // ignored by switchMode() and initialize(), call selfTest() to use this mode
}

public enum AK09918_err_type
{
    AK09918_ERR_OK = 0,                 // ok
    AK09918_ERR_DOR = 1,                // data skipped
    AK09918_ERR_NOT_RDY = 2,            // not ready
    AK09918_ERR_TIMEOUT = 3,            // read/write timeout
    AK09918_ERR_SELFTEST_FAILED = 4,    // self test failed
    AK09918_ERR_OVERFLOW = 5,           // sensor overflow, means |x|+|y|+|z| >= 4912uT
    AK09918_ERR_WRITE_FAILED = 6,       // fail to write
    AK09918_ERR_READ_FAILED = 7,        // fail to read
}

public class AK09918
{
    // I2C address
    const byte AK09918_I2C_ADDR = 0x0C; // I2C address (Can't be changed)

    // Register addresses
    const byte AK09918_WIA1  = 0x00; // Company ID
    const byte AK09918_WIA2  = 0x01; // Device ID
    const byte AK09918_RSV1  = 0x02; // Reserved 1
    const byte AK09918_RSV2  = 0x03; // Reserved 2
    const byte AK09918_ST1   = 0x10; // DataStatus 1
    const byte AK09918_HXL   = 0x11; // X-axis data (Low)
    const byte AK09918_HXH   = 0x12; // X-axis data (High)
    const byte AK09918_HYL   = 0x13; // Y-axis data (Low)
    const byte AK09918_HYH   = 0x14; // Y-axis data (High)
    const byte AK09918_HZL   = 0x15; // Z-axis data (Low)
    const byte AK09918_HZH   = 0x16; // Z-axis data (High)
    const byte AK09918_TMPS  = 0x17; // Dummy
    const byte AK09918_ST2   = 0x18; // DataStatus 2
    const byte AK09918_CNTL1 = 0x30; // Dummy
    const byte AK09918_CNTL2 = 0x31; // Control settings
    const byte AK09918_CNTL3 = 0x32; // Control settings

    // Bit masks
     const byte AK09918_SRST_BIT = 0x01; // Soft Reset
     const byte AK09918_HOFL_BIT = 0x08; // Sensor Over Flow
     const byte AK09918_DOR_BIT  = 0x02; // Data Over Run
     const byte AK09918_DRDY_BIT = 0x01; // Data Ready

    // Privates
    private I2cDevice _device;
    private AK09918_mode_type _mode;

    public AK09918()
    {
        _device = Robot.CreateI2cDevice(AK09918_I2C_ADDR);
    }

    // default to AK09918_CONTINUOUS_10HZ mode
    public AK09918_err_type Initialize(AK09918_mode_type mode = AK09918_mode_type.AK09918_NORMAL)
    {
        if(mode == AK09918_mode_type.AK09918_SELF_TEST)
        {
            mode = AK09918_mode_type.AK09918_POWER_DOWN;
        }
        _mode = mode;

        if(mode == AK09918_mode_type.AK09918_NORMAL)
        {
            return AK09918_err_type.AK09918_ERR_OK;
        }
        else
        {
            return SwitchMode(_mode);
        }
    }

    // Get magnet data in uT
    public AK09918_err_type GetData(ref short x, ref short y, ref short z)
    {
        AK09918_err_type err = GetRawData(ref x, ref y, ref z);
        x = (short)(x * 15 / 100);
        y = (short)(y * 15 / 100);
        z = (short)(z * 15 / 100);

        return err;
    }

    // Get raw I2C magnet data
    public AK09918_err_type GetRawData(ref short x, ref short y, ref short z)
    {
        if(_mode == AK09918_mode_type.AK09918_NORMAL)
        {
            SwitchMode(AK09918_mode_type.AK09918_NORMAL);
            bool is_end = false;
            int count = 0;
            while(!is_end)
            {
                if(_GetRawMode() == 0x00)
                {
                    is_end = true;
                }
                if(count >= 15)
                {
                    return  AK09918_err_type.AK09918_ERR_TIMEOUT;
                }
                count++;
                Robot.Wait(1);
            }
        }

        Span<byte> buffer = stackalloc byte[8];
        _device.ReadRegister(AK09918_HXL, buffer);
        x = (short)(buffer[1] << 8 | buffer[0]);
        y = (short)(buffer[3] << 8 | buffer[2]);
        z = (short)(buffer[5] << 8 | buffer[4]);

        // set register to not disable measurements

        if ((buffer[7] & AK09918_HOFL_BIT) != 0)
        {
            return AK09918_err_type.AK09918_ERR_OVERFLOW;
        }
        return AK09918_err_type.AK09918_ERR_OK;
    }

    public AK09918_mode_type GetMode()
    {
        return _mode;
    }

    public AK09918_err_type SwitchMode(AK09918_mode_type mode)
    {
        if(mode == AK09918_mode_type.AK09918_SELF_TEST)
        {
            return AK09918_err_type.AK09918_ERR_WRITE_FAILED;
        }
        _mode = mode;
        _device.WriteByteRegister(AK09918_CNTL2, (byte)mode);
        return AK09918_err_type.AK09918_ERR_OK;
    }

    public void Reset()
    {
        _device.WriteByteRegister(AK09918_CNTL3, AK09918_SRST_BIT);
    }

    public string StrError(AK09918_err_type error)
    {
        string result;
        switch(error)
        {
            case AK09918_err_type.AK09918_ERR_OK:
                result = "AK09918_ERR_OK: OK";
                break;

            case AK09918_err_type.AK09918_ERR_DOR:
                result = "AK09918_ERR_DOR: Data skipped";
                break;

            case AK09918_err_type.AK09918_ERR_NOT_RDY:
                result = "AK09918_ERR_NOT_RDY: Not ready";
                break;

            case AK09918_err_type.AK09918_ERR_TIMEOUT:
                result = "AK09918_ERR_TIMEOUT: Timeout";
                break;

            case AK09918_err_type.AK09918_ERR_SELFTEST_FAILED:
                result = "AK09918_ERR_SELFTEST_FAILED: Self test failed";
                break;

            case AK09918_err_type.AK09918_ERR_OVERFLOW:
                result = "AK09918_ERR_OVERFLOW: Sensor overflow";
                break;

            case AK09918_err_type.AK09918_ERR_WRITE_FAILED:
                result = "AK09918_ERR_WRITE_FAILED: Fail to write";
                break;

            case AK09918_err_type.AK09918_ERR_READ_FAILED:
                result = "AK09918_ERR_READ_FAILED: Fail to read";
                break;

            default:
                result = "Unknown Error";
                break;
        }

        return result;
    }
    
    public short GetDeviceID()
    {
        Span<byte> buffer = stackalloc byte[2];
        _device.ReadRegister(AK09918_WIA1, buffer);
        return (short)(buffer[0] << 8 | buffer[1]);
    }

    private byte _GetRawMode()
    {
        return _device.ReadByteRegister(AK09918_CNTL2);    
    }
}