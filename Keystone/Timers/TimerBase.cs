using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Collections.Generic;

// TODO: is winmm.dll portable?  it's probably not

namespace Keystone.Timers
{
    public sealed class TimerBase : IDisposable
    {
        // timeBeginPeriod increases the fidelity of the windows timer from 20ms to 1ms.
        [SuppressUnmanagedCodeSecurity(), DllImport("winmm.dll")]
        private static extern int timeBeginPeriod(int uPeriod);

        [SuppressUnmanagedCodeSecurity(), DllImport("winmm.dll")]
        private static extern int timeEndPeriod(int uPeriod);

        //Private _disposed As Boolean = False
        private static Timer mInstance = null;
        private static readonly object mLock = new object();

        private Timer()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>timeBeginPeriod increases the fidelity of the windows timer from 20ms to 1ms.</remarks>
        public static Timer Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                    {
                        mInstance = new Timer();
                        timeBeginPeriod(1);
                    }
                    return mInstance;
                }
            }
        }

        public static bool PeriodExpired(int startPeriod, int periodLength)
        {
            return Math.Abs(Environment.TickCount - startPeriod) > periodLength;
        }

        public static int ElapsedMilliseconds(int startPeriod, out int tickCount)
        {
        	tickCount = Environment.TickCount;
            return Math.Abs(tickCount - startPeriod);
        }

   
        public int Time()
        {
            return Environment.TickCount;
        }
        
        #region IDisposable Members
        private static bool _disposed = false;

        public void Dispose()
        {
            timeEndPeriod(1);
            _disposed = true;
        }

        public void finalize()
        {
            if (!_disposed) Dispose();
        }
        #endregion
    }
}