namespace Avans.StatisticalRobot
{
    /// <summary>Tracks a repeating interval that is checked by the application.</summary>
    /// <param name="msPeriod">The interval between ticks, in milliseconds.</param>
    public class PeriodTimer(int msPeriod)
    {
        private DateTime _lastTickTime = DateTime.Now;

        private TimeSpan _period = TimeSpan.FromMilliseconds(msPeriod);

        /// <summary>Checks whether the configured period has elapsed and resets the timer when it has.</summary>
        /// <returns>True if the period has elapsed since construction or the last successful check; otherwise, false.</returns>
        public bool Check()
        {
            var result = false;

            if (DateTime.Now - _lastTickTime > _period)
            {
                result = true;
                _lastTickTime = DateTime.Now;
            }
            return result;
        }

    }
}
