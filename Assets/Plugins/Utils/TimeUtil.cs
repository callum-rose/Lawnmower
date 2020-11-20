using System;

namespace Utils
{
    public static class TimeUtil
    {
        public static long Millis { get { return GetCurrentDate().Ticks / TimeSpan.TicksPerMillisecond; } }
        public static long Seconds { get { return GetCurrentDate().Ticks / TimeSpan.TicksPerSecond; } }

        public static DateTime CurrentDate { get { return GetCurrentDate(); } }

        public static string GetNiceDateFormat(DateTime dateTime)
        {
            return string.Format("{0:dd_MM_yy}", dateTime);
        }

        public static string GetNiceTimeFormat(DateTime dateTime)
        {
            return string.Format("{0:HH:mm:ss}", dateTime);
        }

#if UNITY_EDITOR

        // for testing
        static DateTime fakeInitialDate = new DateTime(2018, 12, 20, 17, 0, 0, 0);
        static DateTime firstCalledDate = new DateTime(2000, 1, 1);

#endif

        /// <summary>
        /// Gets the time in milliseconds since the paramater millis.
        /// </summary>
        public static long GetMillisSince(long millis)
        {
            return Millis - millis;
        }

        /// <summary>
        /// Gets the time in seconds since the paramater seconds.
        /// </summary>
        public static long GetSecondsSince(long seconds)
        {
            return Seconds - seconds;
        }

        static DateTime GetCurrentDate()
        {
#if UNITY_EDITOR
            if (firstCalledDate.Year == 2000)
                firstCalledDate = DateTime.Now;

            // for testing to ensure same start time
            return fakeInitialDate + (DateTime.Now - firstCalledDate);
#else
        return DateTime.Now;
#endif
        }
    }
}