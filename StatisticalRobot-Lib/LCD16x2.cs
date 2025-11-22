using System.Device.I2c;

namespace Avans.StatisticalRobot
{
    public class LCD16x2
    {
        private I2cDevice Device { get; }

        /// <summary>
        /// Dit is een I2C device
        /// 3.3V/5V
        /// </summary>
        /// <param name="address">Address for example: 0x3E</param>
        public LCD16x2(byte address)
        {
            this.Device = Robot.CreateI2cDevice(address);
        }

        private void TextCommand(byte cmd)
        {
            Device.WriteByteRegister(0x80, cmd);
        }

        // Set display text \n for second line (or auto wrap)

        /// <summary>
        /// clears the display and then sets the text of the display
        /// </summary>
        /// <param name="text">the text you want to show on the display (use \n for linebreaks)</param>
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
        /// <summary>
        /// sets the text of the display by overwriting the previous text without clearing it first
        /// </summary>
        /// <param name="text">the text you want to show on the display (use \n for linebreaks)</param>
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
