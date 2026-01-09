/* obsolete - The old code Profiler module used this module to calculate the elapsed times before i switched it to use Stopwatch which is portable
	/// and does not rely on win32 API calls

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Keystone.Timers
{
	/// <summary>
	/// Time management
	/// </summary>
	/// <remarks>The old Profiler module uses this module to calculate the elapsed times before i switched it to use Stopwatch which is portable
	/// and does not rely on win32 API calls</remarks>
	public class Time
    {
	    // The performance counter API has the best precision
	    [DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
		
		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(out long lpFrequency);
	
	    public static long Counter
	    {
	        get
	        {
	            long R;
	            QueryPerformanceCounter(out R);
	            return R;
	    	}
	    }
	
	    private static long mFrequency = 0L;
	    public static long Frequency
	    {
	    	get 
	    	{
	            // Caches the frequency since it doesn't change
            	if (mFrequency == 0L)
	            	QueryPerformanceFrequency(out mFrequency);
	            return mFrequency;
	    	}
	    }
	    
	    public static double ElapsedSeconds (long startCounter)
	    {
	    	return (Counter - startCounter) *  (1D / (double)Time.Frequency);
	    	
	    }
    }
}
*/