namespace Avans.StatisticalRobot
{
    public sealed class LCDSlide
    {
        private readonly AbstractLCD lcd;
        private readonly int line;
        private readonly IEnumerable<string> lines;

        private Direction direction = Direction.L2R;
        private int frameDelayMs = 300;
        private int pauseBetweenLinesMs = 800;

        internal LCDSlide(AbstractLCD lcd, int line, IEnumerable<string> lines)
        {
            this.lcd = lcd;
            this.line = line;
            this.lines = lines;
        }

        /// <summary>
        /// Set the slide direction from Left to Right.
        /// </summary>
        /// <returns>The current Slide instance.</returns>
        public LCDSlide LeftToRight()
        {
            this.direction = Direction.L2R;
            return this;
        }

        /// <summary>
        /// Set the slide direction from Right to Left.
        /// </summary>
        /// <returns>The current Slide instance.</returns>
        public LCDSlide RightToLeft()
        {
            this.direction = Direction.R2L;
            return this;
        }

        /// <summary>
        /// Set the slide direction.
        /// </summary>
        /// <param name="direction">The direction to set for the slide.</param>
        /// <returns>The current Slide instance.</returns>
        public LCDSlide SetDirection(Direction direction)
        {
            this.direction = direction;
            return this;
        }

        /// <summary>
        /// Set the frame delay in milliseconds.
        /// </summary>
        /// <param name="milliseconds">The delay in milliseconds between frames.</param>
        /// <returns>The current Slide instance.</returns>
        public LCDSlide SetFrameDelay(int milliseconds)
        {
            this.frameDelayMs = milliseconds;
            return this;
        }

        /// <summary>
        /// Set the pause duration between lines in milliseconds.
        /// </summary>
        /// <param name="milliseconds">The pause duration in milliseconds between lines.</param>
        /// <returns>The current Slide instance.</returns>
        public LCDSlide SetPauseBetweenLines(int milliseconds)
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
                string padded = new string(' ', lcd.CharsPerLine) + line + new string(' ', lcd.CharsPerLine);

                if (direction == Direction.L2R)
                {
                    for (int i = 0; i <= padded.Length - lcd.CharsPerLine; i++)
                    {
                        lcd.SetText(this.line, 0, padded.Substring(i, lcd.CharsPerLine));
                        Thread.Sleep(frameDelayMs ?? this.frameDelayMs);
                    }
                }
                else
                {
                    for (int i = padded.Length - lcd.CharsPerLine; i >= 0; i--)
                    {
                        lcd.SetText(this.line, 0, padded.Substring(i, lcd.CharsPerLine));
                        Thread.Sleep(frameDelayMs ?? this.frameDelayMs);

                    }
                }

                Thread.Sleep(pauseBetweenLinesMs ?? this.pauseBetweenLinesMs);
            }
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