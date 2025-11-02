using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Keystone.Timers
{
    /// <summary>
    /// Handles all the time-based stuff.
    /// </summary>
    public class TimeManager
    {
        #region Externs

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll")]
        private static extern short QueryPerformanceCounter(ref long X);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32.dll")]
        private static extern short QueryPerformanceFrequency(ref long X);

        #endregion

        private static TimeManager _this;

        private double _frequency;
        private double _elapsedTime;
        private long _startTime, _endTime;

        /// <summary>
        /// Constructor
        /// </summary>
        private TimeManager()
        {
            long frequency = 0L;
            QueryPerformanceFrequency(ref frequency);
            _frequency = (double) frequency;

            QueryPerformanceCounter(ref _endTime);
        }

        /// <summary>
        /// Does all precision-timer calculations
        /// </summary>
        public void UpdateTime()
        {
            _startTime = _endTime;
            QueryPerformanceCounter(ref _endTime);
            _elapsedTime = ((double) (_endTime - _startTime)/_frequency*1000.0f);
        }

        /// <summary>
        /// Gets the elapsed time
        /// </summary>
        public double ElapsedTime
        {
            get { return _elapsedTime; }
        }

        /// <summary>
        /// Singleton getter
        /// </summary>
        public static TimeManager Instance
        {
            get
            {
                if (_this == null)
                {
                    throw new NotSupportedException("This module has not been initialized.");
                }
                return _this;
            }
        }

        /// <summary>
        /// Singleton initializer
        /// </summary>
        public static void Initialize()
        {
            _this = new TimeManager();
        }
    }
}