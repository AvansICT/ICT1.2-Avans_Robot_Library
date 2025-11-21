using System.Device.I2c;

namespace Avans.StatisticalRobot
{
    public class LCD16x2
    {
        // Commands
        private const byte CMD_CLEAR_DISPLAY = 0x01;
        private const byte CMD_RETURN_HOME = 0x02;
        private const byte CMD_CURSOR_OFF = 0x04;
        private const byte CMD_DISPLAY_ON = 0x08;
        private const byte CMD_2LINE = 0x28;
        private const byte CMD_WRITE_DATA = 0x40;
        private const byte CMD_SET_DDRAM_ADDR = 0x80;

        private const int WIDTH = 16;

        protected I2cDevice Device { get; }

        /// <summary>
        /// Dit is een I2C device
        /// 3.3V/5V
        /// </summary>
        /// <param name="address">Address for example: 0x3E</param>
        public LCD16x2(byte address)
        {
            this.Device = Robot.CreateI2cDevice(address);

            // Initialize the display with default settings
            TextCommand(CMD_CLEAR_DISPLAY);
            Thread.Sleep(1);

            TextCommand(CMD_DISPLAY_ON | CMD_CURSOR_OFF);
            TextCommand(CMD_2LINE);
        }

        /// <summary>
        /// Set the cursor position
        /// </summary>
        /// <param name="row">Row of the cursor (0 or 1)</param>
        /// <param name="column">Column of the cursor (0 to 15)</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetCursor(int row, int column)
        {
            if (row < 0 || row > 1)
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be 0 or 1");
            if (column < 0 || column > 15)
                throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be between 0 and 15");

            TextCommand((byte)((0x40 * row) + (column % 0x10) + 0x80));
        }

        /// <summary>
        /// Set a text on the display at a specific position.
        /// Note that this does not clear the display first, so existing text may remain.
        /// </summary>
        /// <param name="row">Row of the cursor (0 or 1)</param>
        /// <param name="column">Column of the cursor (0 to 15)</param>
        /// <param name="text">Text to display.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetText(int row, int column, string text)
        {
            if (row < 0 || row > 1)
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be 0 or 1");
            if (column < 0 || column > 15)
                throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be between 0 and 15");

            // Trim text if it exceeds the width of the display
            if (text.Length > WIDTH - column)
            {
                text = text[..(WIDTH - column)];
            }

            SetCursor(row, column);
            foreach (char c in text)
            {
                WriteChar(c);
            }
        }

        /// <summary>
        /// Set a text on the display.
        /// Use '\n' to go to the next line.
        /// 
        /// Note: This method clears the display before setting the text.
        /// </summary>
        /// <param name="text">Text to display</param>
        public void SetText(string text)
        {
            TextCommand(CMD_CLEAR_DISPLAY);
            Thread.Sleep(50);

            int count = 0, row = 0;
            foreach (char c in text)
            {
                if (c == '\n' || count == WIDTH)
                {
                    count = 0;
                    row++;
                    if (row == 2) break;

                    SetCursor(row, 0);

                    if (c == '\n') continue;
                }

                count++;
                WriteChar(c);
            }
        }

        /// <summary>
        /// Set a text on the display without clearing it first.
        /// Use '\n' to go to the next line.
        /// 
        /// Note: This method does NOT clear the display before setting the text.
        /// </summary>
        /// <param name="text">Text to display</param>
        public void SetTextNoRefresh(string text)
        {
            TextCommand(CMD_RETURN_HOME);
            Thread.Sleep(50);

            text = text.PadRight(32);

            int count = 0, row = 0;
            foreach (char c in text)
            {
                if (c == '\n' || count == WIDTH)
                {
                    count = 0;
                    row++;
                    if (row == 2) break;

                    SetCursor(row, 0);

                    if (c == '\n') continue;
                }

                count++;
                WriteChar(c);
            }
        }

        /// <summary>
        /// Clear the display.
        /// </summary>
        public void Clear()
        {
            TextCommand(CMD_CLEAR_DISPLAY);
            Thread.Sleep(50);
        }

        /// <summary>
        /// Create a slide show with one text line.
        /// </summary>
        /// <param name="text">Text to display in the slide show</param>
        /// <param name="row">Row on which the slide show will be displayed (0 or 1)</param>
        /// <returns>A slide instance, where you can modify the slide show settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Slide SlideText(string text, int row)
        {
            if (row < 0 || row > 1)
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be 0 or 1");

            return new Slide(this, row, [text]);
        }

        /// <summary>
        /// Create a slide show with multiple text lines.
        /// </summary>
        /// <param name="lines">Lines of text to display in the slide show</param>
        /// <param name="row">Row on which the slide show will be displayed (0 or 1)</param>
        /// <returns>A slide instance, where you can modify the slide show settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Slide SlideShow(IEnumerable<string> lines, int row)
        {
            if (row < 0 || row > 1)
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be 0 or 1");

            return new Slide(this, row, lines);
        }

        /// <summary>
        /// Create a slide show with multiple text lines.
        /// </summary>
        /// <param name="row">Row on which the slide show will be displayed (0 or 1)</param>
        /// <param name="lines">Lines of text to display in the slide show</param>
        /// <returns>A slide instance, where you can modify the slide show settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Slide SlideShow(int row, params string[] lines)
        {
            if (row < 0 || row > 1)
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be 0 or 1");

            return new Slide(this, row, lines);
        }

        private void WriteChar(char c)
        {
            TextCommand((byte)c, CMD_WRITE_DATA);
        }

        private void TextCommand(byte cmd, byte action = CMD_SET_DDRAM_ADDR)
        {
            Device.WriteByteRegister(action, cmd);
        }

        public sealed class Slide
        {
            private readonly LCD16x2 lcd;
            private readonly int row;
            private readonly IEnumerable<string> lines;

            private Direction direction = Direction.L2R;
            private int frameDelayMs = 300;
            private int pauseBetweenLinesMs = 800;

            internal Slide(LCD16x2 lcd, int row, IEnumerable<string> lines)
            {
                this.lcd = lcd;
                this.row = row;
                this.lines = lines;
            }

            /// <summary>
            /// Set the slide direction from Left to Right.
            /// </summary>
            /// <returns>The current Slide instance.</returns>
            public Slide LeftToRight()
            {
                this.direction = Direction.L2R;
                return this;
            }

            /// <summary>
            /// Set the slide direction from Right to Left.
            /// </summary>
            /// <returns>The current Slide instance.</returns>
            public Slide RightToLeft()
            {
                this.direction = Direction.R2L;
                return this;
            }

            /// <summary>
            /// Set the slide direction.
            /// </summary>
            /// <param name="direction">The direction to set for the slide.</param>
            /// <returns>The current Slide instance.</returns>
            public Slide SetDirection(Direction direction)
            {
                this.direction = direction;
                return this;
            }

            /// <summary>
            /// Set the frame delay in milliseconds.
            /// </summary>
            /// <param name="milliseconds">The delay in milliseconds between frames.</param>
            /// <returns>The current Slide instance.</returns>
            public Slide SetFrameDelay(int milliseconds)
            {
                this.frameDelayMs = milliseconds;
                return this;
            }

            /// <summary>
            /// Set the pause duration between lines in milliseconds.
            /// </summary>
            /// <param name="milliseconds">The pause duration in milliseconds between lines.</param>
            /// <returns>The current Slide instance.</returns>
            public Slide SetPauseBetweenLines(int milliseconds)
            {
                this.pauseBetweenLinesMs = milliseconds;
                return this;
            }

            /// <summary>
            /// Start the slide show.
            /// </summary>
            /// <param name="frameDelayMs">(Optionally) The delay in milliseconds between frames.</param>
            /// <param name="pauseBetweenLinesMs">(Optionally) The pause duration in milliseconds between lines.</param>
            public void Start(int? frameDelayMs = null, int? pauseBetweenLinesMs = null)
            {
                foreach (string line in lines)
                {
                    // Pad with spaces so it appears to slide in and out
                    string padded = new string(' ', WIDTH) + line + new string(' ', WIDTH);

                    if (direction == Direction.L2R)
                    {
                        for (int i = 0; i <= padded.Length - WIDTH; i++)
                        {
                            ShowFrame(padded.Substring(i, WIDTH), row);
                            Thread.Sleep(frameDelayMs ?? this.frameDelayMs);
                        }
                    }
                    else
                    {
                        for (int i = padded.Length - WIDTH; i >= 0; i--)
                        {
                            ShowFrame(padded.Substring(i, WIDTH), row);
                            Thread.Sleep(frameDelayMs ?? this.frameDelayMs);

                        }
                    }

                    Thread.Sleep(pauseBetweenLinesMs ?? this.pauseBetweenLinesMs);
                }
            }

            private void ShowFrame(string frame, int row)
            {
                lcd.SetCursor(row, 0);

                foreach (char c in frame)
                    lcd.WriteChar(c);
            }

            public enum Direction
            {
                /// <summary>
                /// Left to Right
                /// </summary>
                L2R,

                /// <summary>
                /// Right to Left
                /// </summary>
                R2L
            }
        }
    }
}
