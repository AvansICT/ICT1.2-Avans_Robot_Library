namespace Avans.StatisticalRobot
{
    public class LCD16x2 : AbstractLCD
    {
        // Commands
        private const byte CMD_CLEAR_DISPLAY = 0x01;
        private const byte CMD_RETURN_HOME = 0x02;
        private const byte CMD_CURSOR_OFF = 0x04;
        private const byte CMD_DISPLAY_ON = 0x08;
        private const byte CMD_2LINE = 0x28;
        private const byte CMD_WRITE_DATA = 0x40;
        private const byte CMD_SET_DDRAM_ADDR = 0x80;

        /// <summary>
        /// Dit is een I2C device
        /// 3.3V/5V
        /// </summary>
        /// <param name="address">Address for example: 0x3E</param>
        public LCD16x2(byte address) : base(address, 2, 16)
        {
            // Initialize the display with default settings
            TextCommand(CMD_CLEAR_DISPLAY);
            Thread.Sleep(1);

            TextCommand(CMD_DISPLAY_ON | CMD_CURSOR_OFF);
            TextCommand(CMD_2LINE);
        }

        public override void SetCursor(int line, int characterPosition)
        {
            CheckBounds(line, characterPosition);

            TextCommand((byte)((0x40 * line) + (characterPosition % 0x10) + 0x80));
        }

        public override void SetText(int line, int characterPosition, string text)
        {
            CheckBounds(line, characterPosition);

            // Trim text if it exceeds the width of the display
            if (text.Length > CharsPerLine - characterPosition)
            {
                text = text[..(CharsPerLine - characterPosition)];
            }

            SetCursor(line, characterPosition);
            foreach (char c in text)
            {
                WriteChar(c);
            }
        }

        public override void SetText(string text)
        {
            TextCommand(CMD_CLEAR_DISPLAY);
            Thread.Sleep(50);

            int count = 0, line = 0;
            foreach (char c in text)
            {
                if (c == '\n' || count == CharsPerLine)
                {
                    count = 0;
                    line++;
                    if (line == 2) break;

                    SetCursor(line, 0);

                    if (c == '\n') continue;
                }

                count++;
                WriteChar(c);
            }
        }

        public override void SetTextNoRefresh(string text)
        {
            TextCommand(CMD_RETURN_HOME);
            Thread.Sleep(50);

            text = text.PadRight(32);

            int count = 0, line = 0;
            foreach (char c in text)
            {
                if (c == '\n' || count == CharsPerLine)
                {
                    count = 0;
                    line++;
                    if (line == 2) break;

                    SetCursor(line, 0);

                    if (c == '\n') continue;
                }

                count++;
                WriteChar(c);
            }
        }

        public override void Clear()
        {
            TextCommand(CMD_CLEAR_DISPLAY);
            Thread.Sleep(50);
        }

        private void WriteChar(char c)
        {
            TextCommand((byte)c, CMD_WRITE_DATA);
        }

        private void TextCommand(byte cmd, byte action = CMD_SET_DDRAM_ADDR)
        {
            Device.WriteByteRegister(action, cmd);
        }
    }
}
