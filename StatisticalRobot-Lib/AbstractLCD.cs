using System.Device.I2c;

namespace Avans.StatisticalRobot
{
    public abstract class AbstractLCD
    {
        public int Lines { get; init; }
        public int CharsPerLine { get; init; }

        protected I2cDevice Device { get; init; }

        protected AbstractLCD(byte address, int lines, int charsPerLine)
        {
            this.Lines = lines;
            this.CharsPerLine = charsPerLine;

            this.Device = Robot.CreateI2cDevice(address);
        }

        /// <summary>
        /// Create a slide show with one text line.
        /// </summary>
        /// <param name="text">Text to display in the slide show</param>
        /// <param name="line">Line on which the slide show will be displayed</param>
        /// <returns>A slide instance, where you can modify the slide show settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public LCDSlide SlideText(string text, int line)
        {
            CheckBounds(line);
            return new LCDSlide(this, line, [text]);
        }

        /// <summary>
        /// Set the cursor position
        /// </summary>
        /// <param name="line">Line of the cursor</param>
        /// <param name="characterPosition">Character position of the cursor</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public abstract void SetCursor(int line, int characterPosition);

        /// <summary>
        /// Set a text on the display at a specific position.
        /// Note that this does not clear the display first, so existing text may remain.
        /// </summary>
        /// <param name="line">Line of the cursor</param>
        /// <param name="characterPosition">Character position of the cursor</param>
        /// <param name="text">Text to display.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public abstract void SetText(int line, int characterPosition, string text);

        /// <summary>
        /// Set a text on the display.
        /// Use '\n' to go to the next line.
        /// 
        /// Note: This method clears the display before setting the text.
        /// </summary>
        /// <param name="text">Text to display</param>
        public abstract void SetText(string text);

        /// <summary>
        /// Set a text on the display without clearing it first.
        /// Use '\n' to go to the next line.
        /// 
        /// Note: This method does NOT clear the display before setting the text.
        /// </summary>
        /// <param name="text">Text to display</param>
        public abstract void SetTextNoRefresh(string text);

        /// <summary>
        /// Clear the display.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Create a slide show with multiple text lines.
        /// </summary>
        /// <param name="lines">Lines of text to display in the slide show</param>
        /// <param name="line">Line on which the slide show will be displayed</param>
        /// <returns>A slide instance, where you can modify the slide show settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public LCDSlide SlideShow(IEnumerable<string> lines, int line)
        {
            CheckBounds(line);
            return new LCDSlide(this, line, lines);
        }

        /// <summary>
        /// Create a slide show with multiple text lines.
        /// </summary>
        /// <param name="line">Line on which the slide show will be displayed</param>
        /// <param name="lines">Lines of text to display in the slide show</param>
        /// <returns>A slide instance, where you can modify the slide show settings.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public LCDSlide SlideShow(int line, params string[] lines)
        {
            CheckBounds(line);
            return new LCDSlide(this, line, lines);
        }

        protected void CheckBounds(int? line, int? characterPosition = null)
        {
            if (line.HasValue && (line < 0 || line > Lines - 1))
                throw new ArgumentOutOfRangeException(nameof(line), line, "Line must be between 0 and " + (Lines - 1));
            if (characterPosition.HasValue && (characterPosition < 0 || characterPosition > CharsPerLine - 1))
                throw new ArgumentOutOfRangeException(nameof(characterPosition), characterPosition, "Character position must be between 0 and " + (CharsPerLine - 1));
        }
    }
}