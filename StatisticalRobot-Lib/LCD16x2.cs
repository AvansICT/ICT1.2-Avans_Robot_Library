using System.Device.I2c;

namespace Avans.StatisticalRobot
{
    /// <summary>Writes text to a two-line, 16-character I2C LCD.</summary>
    public class LCD16x2
    {
        private I2cDevice Device { get; }

        /// <summary>Creates an I2C connection to the LCD at the specified address.</summary>
        /// <param name="address">The LCD I2C address, for example 0x3E.</param>
        public LCD16x2(byte address)
        {
            this.Device = Robot.CreateI2cDevice(address);
        }

        private void TextCommand(byte cmd)
        {
            Device.WriteByteRegister(0x80, cmd);
        }

        // Set display text \n for second line (or auto wrap)

        /// <summary>Clears the LCD and writes text across at most two rows of 16 characters.</summary>
        /// <param name="text">The text to display. Use \n for line breaks; lines also wrap after 16 characters. Text beyond the second row is ignored.</param>
        public void SetText(string text)
        {
            TextCommand(0x01); // clear display
            Thread.Sleep(50);
            TextCommand(0x08 | 0x04); // display on, no cursor
            TextCommand(0x28); // 2 lines
            Thread.Sleep(50);
            int count = 0;
            int row = 0;

            foreach (char c in text)
            {
                if (c == '\n' || count == 16)
                {
                    count = 0;
                    row++;
                    if (row == 2) break;
                    TextCommand(0xc0);
                    if (c == '\n') continue;
                }
                count++;
                Device.WriteByteRegister(0x40, (byte)c);
            }
        }

        //    // Update the display without erasing the display
        /// <summary>Returns the cursor home and overwrites text without clearing the LCD first. Input is padded to at least 32 characters.</summary>
        /// <param name="text">The text to display. Use \n for line breaks; lines also wrap after 16 characters. Text beyond the second row is ignored.</param>
        public void SetTextNoRefresh(string text)
        {
            TextCommand(0x02); // return home
            Thread.Sleep(50);
            TextCommand(0x08 | 0x04); // display on, no cursor
            TextCommand(0x28); // 2 lines
            Thread.Sleep(50);
            int count = 0;
            int row = 0;

            text = text.PadRight(32);

            foreach (char c in text)
            {
                if (c == '\n' || count == 16)
                {
                    count = 0;
                    row++;
                    if (row == 2) break;
                    TextCommand(0xc0);
                    if (c == '\n') continue;
                }
                count++;
                Device.WriteByteRegister(0x40, (byte)c);
            }
        }
    }
}
