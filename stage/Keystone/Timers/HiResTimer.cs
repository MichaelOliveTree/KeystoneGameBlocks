using System;
using System.Runtime.InteropServices;

namespace Keystone.Timers
{
    /// <summary>
    /// Declarations for querying the Win32 Performance Counter
    /// </summary>
    internal class UnmanagedMethods
    {
        internal struct LARGE_INTEGER
        {
            public UInt32 lowpart;
            public UInt32 highpart;
        }

        [DllImport("kernel32.dll")]
        internal static extern UInt32 QueryPerformanceCounter(ref LARGE_INTEGER
                                                                  lpPerformanceCount);

        [DllImport("kernel32.dll")]
        internal static extern UInt32 QueryPerformanceFrequency(ref LARGE_INTEGER
                                                                    lpFrequency);
    }

    /// <summary>
    /// A Win32 timer using the system performance counter (the highest resolution timer
    /// possible on the system).  Typically the high performance counter has a resolution of 1 
    /// microsecond or less.
    /// </summary>
    public class HiResTimer
    {
        private double period = 0;
        private double startTime = 0;
        private double timerFrequency = 0;
        private bool hasHiResCounter = false;

        /// <summary>
        /// Starts the timing using the high resolution performance counter
        /// (if installed) on the machine.
        /// </summary>
        public void Start()
        {
            UnmanagedMethods.LARGE_INTEGER res = new
                UnmanagedMethods.LARGE_INTEGER();
            UnmanagedMethods.QueryPerformanceCounter(ref res);
            startTime = (res.highpart >> 32) + res.lowpart;
        }

        /// <summary>
        /// Stops timing using the high resolution performance counter.  Note
        /// if Start() has not been called prior to this call, the result
        /// is undefined.
        /// </summary>
        public void Stop()
        {
            UnmanagedMethods.LARGE_INTEGER res = new
                UnmanagedMethods.LARGE_INTEGER();
            UnmanagedMethods.QueryPerformanceCounter(ref res);
            double endTime = (res.highpart >> 32) + res.lowpart;
            period = endTime - startTime;
        }

        /// <summary>
        /// Returns the time measured between calling Start() and Stop() methods, 
        /// in seconds.
        /// </summary>
        public double ElapsedTime
        {
            get { return period/timerFrequency; }
        }

        /// <summary>
        /// Returns whether a high resolution counter is available on the system.
        /// If no hardware is available, this class will not be usable.  I have
        /// not found a system which does not provide this function.
        /// </summary>
        public bool HasHiResCounter
        {
            get { return hasHiResCounter; }
        }

        /// <summary>
        /// Returns the number of ticks per second of the high resolution
        /// counter.  Typically this will be 1 million or above.
        /// </summary>
        public double Frequency
        {
            get { return timerFrequency; }
        }

        /// <summary>
        ///  Constructs a new instance of the HiResTimer object, and checks
        ///  the availability and resolution of the system high resolution
        ///  performance counter.
        /// </summary>
        public HiResTimer()
        {
            // If the installed hardware supports a high-resolution performance counter, 
            // the return value is nonzero.
            // If the function fails, the return value is zero. To get extended error 
            // information, call GetLastError. For example, if the installed hardware 
            // does not support a high-resolution performance counter, the function fails. 
            UnmanagedMethods.LARGE_INTEGER res = new
                UnmanagedMethods.LARGE_INTEGER();
            UInt32 r = UnmanagedMethods.QueryPerformanceFrequency(ref res);
            if (r != 0)
            {
                hasHiResCounter = true;
                timerFrequency = (res.highpart >> 32) + res.lowpart;
            }
        }
    }
}