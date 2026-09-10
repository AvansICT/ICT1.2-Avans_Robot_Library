namespace Avans.StatisticalRobot
{
    public class PeriodTimer(int msPeriod)
    {
        private DateTime _lastTickTime = DateTime.Now;

        private TimeSpan _period = TimeSpan.FromMilliseconds(msPeriod);

        /// <summary>
        /// checks if a certain amount of time has elapsed and if it has elapsed resets the timer
        /// </summary>
        /// <returns>returns true if the period has passed since the last reset and false if not</returns>
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
